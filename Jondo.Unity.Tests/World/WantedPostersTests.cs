using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jondo.Unity.Launcher;
using Jondo.Unity.Server;
using Jondo.Unity.Server.Managers;
using Xunit;

namespace Jondo.Unity.Tests.World
{
    /// <summary>
    /// Alone: the placement test loads the maps and changes the world's groups, which the other
    /// world tests read and load concurrently.
    /// </summary>
    [CollectionDefinition("WantedPosters", DisableParallelization = true)]
    public class WantedPostersCollection { }

    /// <summary>The wanted posters: the criminals read off the client's data, and where they stand.</summary>
    [Collection("WantedPosters")]
    public class WantedPostersTests
    {
        private static void LoadTheRealOnes()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Paths.WantedJson));
            WantedPosters.LoadFrom(doc.RootElement);
        }

        private const string Two = @"{
          ""jailers"": [ { ""npc"": 9, ""ask"": 1, ""first"": 2, ""offer"": 3, ""second"": 4, ""keep"": 5,
                           ""listen"": 6, ""another"": 7, ""leave"": 8, ""shrug"": 10 } ],
          ""wanted"": [
            { ""quest"": 100, ""monster"": 900500, ""subareas"": [90001], ""catch"": 11, ""deliver"": 12, ""map"": 77,
              ""jailers"": [ { ""npc"": 9, ""deliver"": 20, ""pay"": 21, ""found"": 22, ""seen"": 23, ""unseen"": 24 } ] },
            { ""quest"": 101, ""monster"": 900501, ""subareas"": [90002], ""catch"": 13, ""deliver"": 14, ""map"": 0,
              ""jailers"": [ { ""npc"": 9, ""deliver"": 30, ""pay"": 31, ""found"": 32, ""seen"": 33, ""unseen"": 34 } ] }
          ] }";

        private static void LoadTwo()
        {
            using var doc = JsonDocument.Parse(Two);
            WantedPosters.LoadFrom(doc.RootElement);
        }

        /// <summary>«On recherche Gein»: his quest, his zone, and the Krosmoglob's replies, as the client has them.</summary>
        [Fact]
        public void Gein_is_wanted_by_the_Krosmoglob()
        {
            if (!File.Exists(Paths.WantedJson)) return;
            LoadTheRealOnes();

            var gein = WantedPosters.ByMonster(3761)!;
            Assert.Equal(1563, gein.Quest);
            Assert.Equal(new[] { 829 }, gein.Subareas);
            Assert.Equal((8930, 8931, 130023937L), (gein.Catch, gein.Deliver, gein.Map));
            Assert.Equal(15585, gein.Follower);
            var report = gein.To(2358)!;
            Assert.Equal((21723, 78913, 57773, 57774, 57775),
                         (report.Deliver, report.Pay, report.Found, report.Seen, report.Unseen));

            var krosmoglob = WantedPosters.JailerOf(2358)!;
            Assert.Equal((78879, 57756, 78878, 57757, 79096),
                         (krosmoglob.Ask, krosmoglob.First, krosmoglob.Offer, krosmoglob.Second, krosmoglob.Keep));
            Assert.Same(gein, WantedPosters.ByQuest(1563));
        }

        /// <summary>Every criminal the data wants has somebody to take him.</summary>
        [Fact]
        public void Every_criminal_has_a_jailer()
        {
            if (!File.Exists(Paths.WantedJson)) return;
            LoadTheRealOnes();

            Assert.True(WantedPosters.All.Count >= 80);
            Assert.All(WantedPosters.All, c => Assert.Contains(c.Jailers, j => j.Deliver != 0));
            Assert.All(WantedPosters.All, c => Assert.NotEmpty(c.Subareas));
        }

        /// <summary>Gein, out of every group the database put him in, back in one of the Hauts Ténébreux.</summary>
        [Fact]
        public void Each_criminal_comes_back_in_one_group_of_his_zone()
        {
            if (!File.Exists(Paths.WorldDb) || !File.Exists(Paths.WantedJson)) return;
            LoadTheRealOnes();
            // Other suites load definitions without spawning the world. Exercise that state too.
            MobSpawnManager.EnsureMonsterData();
            Assert.NotNull(MobSpawnManager.GetMonsterData(31));
            // Placement needs actual world groups; loaded monster definitions do not imply that.
            MobSpawnManager.InitializeAndSpawnAll();

            Assert.True(MobSpawnManager.PlaceCriminalsNow() > 0);
            var at = WantedPosters.WhereIs(3761);
            Assert.NotNull(at);
            Assert.Equal(829, MapManager.GetMapInfo(at!.Value.Map)!.SubAreaId);
            var group = MobSpawnManager.GetMobsForMap(at.Value.Map).Single(g => g.MobId == at.Value.Group);
            Assert.Contains(group.Members, m => m.Monster.Id == 3761);
            Assert.DoesNotContain(WantedPosters.Due(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), c => c.Monster == 3761);
        }

        /// <summary>One group of his zone at a time; away a while once beaten.</summary>
        [Fact]
        public void A_criminal_stands_in_one_place_and_goes_away_when_taken()
        {
            LoadTwo();
            const long now = 1_000_000_000;

            Assert.Equal(new[] { 900500, 900501 }, WantedPosters.Due(now).Select(c => c.Monster));

            WantedPosters.Placed(900500, 77, -5);
            Assert.True(WantedPosters.IsAbout(900500));
            Assert.Equal(new[] { 900501 }, WantedPosters.Due(now).Select(c => c.Monster));

            // Beaten by nobody who wanted him: back after the grace.
            WantedPosters.Released(77, -5, now);
            Assert.False(WantedPosters.IsAbout(900500));
            Assert.DoesNotContain(900500, WantedPosters.Due(now).Select(c => c.Monster));
            Assert.Contains(900500, WantedPosters.Due(now + (long)WantedPosters.Grace.TotalMilliseconds).Select(c => c.Monster));

            // Taken out of the group he stood in, even once it is gone: hours. Out of another -- a
            // legendary hunt's own --: nothing.
            Assert.Null(WantedPosters.Captured(900500, 77, -6, now, new Random(3)));
            var away = WantedPosters.Captured(900500, 77, -5, now, new Random(3))!.Value;
            Assert.InRange(away, WantedPosters.Absence - WantedPosters.AbsenceSpread,
                           WantedPosters.Absence + WantedPosters.AbsenceSpread);
            Assert.DoesNotContain(900500, WantedPosters.Due(now + (long)away.TotalMilliseconds - 1).Select(c => c.Monster));
            Assert.Contains(900500, WantedPosters.Due(now + (long)away.TotalMilliseconds).Select(c => c.Monster));
        }

        /// <summary>FFH: beaten by anybody, he stays away the capture's hours; and only some come back.</summary>
        [Fact]
        public void Ffh_settings_keep_a_beaten_criminal_away_and_let_only_some_come_back()
        {
            var previous = ServerSettings.Current;
            try
            {
                LoadTwo();
                const long now = 1_000_000_000;
                ServerSettings.UseForTests(new() { WantedBeatenStaysAway = true, WantedChancePercent = 25, WantedRetryMinutes = 60 });

                // Beaten by nobody who wanted him: hours, not the two minutes' grace.
                WantedPosters.Placed(900500, 77, -5);
                WantedPosters.Released(77, -5, now, beaten: true);
                long early = now + (long)(WantedPosters.Absence - WantedPosters.AbsenceSpread).TotalMilliseconds - 1;
                long late = now + (long)(WantedPosters.Absence + WantedPosters.AbsenceSpread).TotalMilliseconds;
                Assert.DoesNotContain(900500, WantedPosters.Due(early).Select(c => c.Monster));
                Assert.Contains(900500, WantedPosters.Due(late).Select(c => c.Monster));

                // A group that leaves without a fight -- a map emptied -- still lets him back after the grace.
                WantedPosters.Placed(900501, 88, -9);
                WantedPosters.Released(88, -9, now);
                Assert.Contains(900501, WantedPosters.Due(now + (long)WantedPosters.Grace.TotalMilliseconds).Select(c => c.Monster));

                // A quarter come back; the rest wait an hour for their next chance.
                var dice = new Random(7);
                int back = Enumerable.Range(0, 4000).Count(_ => WantedPosters.ComesBack(900501, now, dice));
                Assert.InRange(back, 850, 1150);
                Assert.False(Enumerable.Range(0, 50).All(_ => WantedPosters.ComesBack(900501, now, dice)));
                Assert.DoesNotContain(900501, WantedPosters.Due(now + 3_600_000 - 1).Select(c => c.Monster));
                Assert.Contains(900501, WantedPosters.Due(now + 3_600_000).Select(c => c.Monster));

                // The game's own settings: every one comes back, the grace is two minutes.
                ServerSettings.UseForTests(new());
                Assert.All(Enumerable.Range(0, 100), _ => Assert.True(WantedPosters.ComesBack(900500, now, dice)));
                WantedPosters.Placed(900500, 77, -5);
                WantedPosters.Released(77, -5, late, beaten: true);
                Assert.Contains(900500, WantedPosters.Due(late + (long)WantedPosters.Grace.TotalMilliseconds).Select(c => c.Monster));
            }
            finally { ServerSettings.UseForTests(previous); }
        }

        /// <summary>FFH: a criminal moves on after his stay, waits his next chance, and his absences outlive a restart.</summary>
        [Fact]
        public void Ffh_a_criminal_moves_on_after_his_stay_and_his_absence_outlives_a_restart()
        {
            var previous = ServerSettings.Current;
            string file = Path.Combine(Path.GetTempPath(), $"wanted_away_{Guid.NewGuid():N}.json");
            try
            {
                LoadTwo();
                const long now = 1_000_000_000, hour = 3_600_000;
                ServerSettings.UseForTests(new() { WantedStayMinutes = 60, WantedRetryMinutes = 60, WantedBeatenStaysAway = true });

                WantedPosters.Placed(900500, 77, -5, now);
                Assert.Empty(WantedPosters.Overstayed(now + hour - 1));
                Assert.Equal(new[] { (900500, 77L, -5L) }, WantedPosters.Overstayed(now + hour));
                Assert.Equal(new[] { (900500, 77L, -5L) }, WantedPosters.Standing());

                WantedPosters.Left(900500, now + hour);
                Assert.False(WantedPosters.IsAbout(900500));
                Assert.DoesNotContain(900500, WantedPosters.Due(now + 2 * hour - 1).Select(c => c.Monster));
                Assert.Contains(900500, WantedPosters.Due(now + 2 * hour).Select(c => c.Monster));

                // 0, the game's: he stays until beaten.
                ServerSettings.UseForTests(new());
                WantedPosters.Placed(900501, 88, -9, now);
                Assert.Empty(WantedPosters.Overstayed(now + 100 * hour));

                // Beaten, kept in the file, and still away after a «restart».
                ServerSettings.UseForTests(new() { WantedBeatenStaysAway = true });
                long real = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                WantedPosters.KeepAbsencesIn(file, real);
                WantedPosters.Released(88, -9, real, beaten: true);
                Assert.True(File.Exists(file));

                LoadTwo();
                Assert.Contains(900501, WantedPosters.Due(real).Select(c => c.Monster));
                WantedPosters.KeepAbsencesIn(file, real);
                Assert.DoesNotContain(900501, WantedPosters.Due(real + 1).Select(c => c.Monster));
                Assert.Contains(900501, WantedPosters.Due(real + (long)(WantedPosters.Absence + WantedPosters.AbsenceSpread).TotalMilliseconds).Select(c => c.Monster));
            }
            finally
            {
                ServerSettings.UseForTests(previous);
                LoadTwo();
                File.Delete(file);
            }
        }

        /// <summary>FFH, on the real world: one criminal to an area, and the one who moves on gives his place back.</summary>
        [Fact]
        public void Ffh_one_criminal_to_an_area_and_the_one_who_moves_on_leaves_his_group_as_it_was()
        {
            if (!File.Exists(Paths.WorldDb) || !File.Exists(Paths.WantedJson)) return;
            var previous = ServerSettings.Current;
            try
            {
                Interactives.Initialize();
                MobSpawnManager.EnsureMonsterData();
                MobSpawnManager.InitializeAndSpawnAll();
                LoadTheRealOnes();
                ServerSettings.UseForTests(new()
                {
                    WantedChancePercent = 100, WantedOnePerArea = true, WantedStayMinutes = 60, WantedRetryMinutes = 60,
                });
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                static int Area(int monster)
                {
                    int zone = WantedPosters.ByMonster(monster)!.Subareas[0];
                    int area = Interactives.AreaOfSubArea(zone);
                    return area >= 0 ? area : -1_000_000 - zone;
                }

                Assert.True(MobSpawnManager.RotateCriminalsNow(now) > 0);
                var first = WantedPosters.Standing();
                int areas = WantedPosters.All.Select(c => Area(c.Monster)).Distinct().Count();
                Assert.InRange(first.Count, 2, areas);
                Assert.Equal(first.Count, first.Select(s => Area(s.Monster)).Distinct().Count());

                // An hour on: every one of them moves on, out of his group, and does not come straight back.
                long later = now + 3_600_000;
                MobSpawnManager.RotateCriminalsNow(later);
                foreach (var (monster, map, group) in first)
                {
                    Assert.NotEqual((map, group), WantedPosters.WhereIs(monster) ?? (0, 0));
                    var stood = MobSpawnManager.GetMobsForMap(map).SingleOrDefault(g => g.MobId == group);
                    if (stood != null) Assert.DoesNotContain(stood.Members, m => m.Monster?.Id == monster);
                    Assert.DoesNotContain(monster, WantedPosters.Due(later + 1).Select(c => c.Monster));
                }
                var second = WantedPosters.Standing();
                Assert.Equal(second.Count, second.Select(s => Area(s.Monster)).Distinct().Count());
            }
            finally
            {
                ServerSettings.UseForTests(previous);
                LoadTheRealOnes();
            }
        }

        /// <summary>A map emptied or put back lets its criminal go.</summary>
        [Fact]
        public void A_map_cleared_lets_its_criminal_go()
        {
            LoadTwo();
            WantedPosters.Placed(900501, 88, -9);
            WantedPosters.ReleasedFrom(88, 0);
            Assert.False(WantedPosters.IsAbout(900501));
        }

        /// <summary>Groups drawn from a zone's roster come out without them.</summary>
        [Fact]
        public void Drawn_groups_carry_no_criminal()
        {
            LoadTwo();
            var ids = new List<int> { 10, 900500, 11, 900501 };
            Assert.Equal(2, WantedPosters.Strip(ids));
            Assert.Equal(new[] { 10, 11 }, ids);
        }

        [Fact]
        public void A_jailer_hands_over_only_his_own()
        {
            LoadTwo();
            Assert.Equal(new[] { 900500, 900501 }, WantedPosters.HandedTo(9).Select(c => c.Monster));
            Assert.Empty(WantedPosters.HandedTo(8));
            Assert.Null(WantedPosters.JailerOf(8));
        }
    }
}
