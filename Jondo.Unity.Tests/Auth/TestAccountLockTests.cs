using Jondo.Unity.Server;
using Jondo.Unity.Server.Managers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Jondo.Unity.Tests.Auth
{
    /// <summary>FFH: a server friends sign in to locks the test account whose password is published.</summary>
    public class TestAccountLockTests
    {
        private static SqliteConnection Accounts(string password, int role = 5)
        {
            var db = new SqliteConnection("Data Source=:memory:");
            db.Open();
            using var make = db.CreateCommand();
            make.CommandText = "CREATE TABLE Accounts (Id INTEGER PRIMARY KEY, Login TEXT, Password TEXT, " +
                               "Nickname TEXT, GameToken TEXT, Role INTEGER, LauncherToken TEXT);" +
                               "INSERT INTO Accounts VALUES (188940901, 'keka', $p, 'Keka', 'tok', $r, 'tok');" +
                               "INSERT INTO Accounts VALUES (7, 'rafa', $p, 'Rafa', NULL, 5, NULL);";
            make.Parameters.AddWithValue("$p", Claves.Cifrar(password));
            make.Parameters.AddWithValue("$r", role);
            make.ExecuteNonQuery();
            return db;
        }

        private static (string Password, long Role) Row(SqliteConnection db, string login)
        {
            using var read = db.CreateCommand();
            read.CommandText = "SELECT Password, Role FROM Accounts WHERE Login = $l;";
            read.Parameters.AddWithValue("$l", login);
            using var row = read.ExecuteReader();
            Assert.True(row.Read());
            return (row.GetString(0), row.GetInt64(1));
        }

        [Fact]
        public void The_published_password_stops_working_and_keka_is_no_longer_administrator()
        {
            using var db = Accounts(DatabaseManager.TestAccountPassword);
            Assert.True(DatabaseManager.LockTestAccount(db));
            var keka = Row(db, "keka");
            Assert.False(Claves.Comprueba(DatabaseManager.TestAccountPassword, keka.Password, out _));
            Assert.Equal(1, keka.Role);
            // Nobody else is touched, even with the same password.
            var rafa = Row(db, "rafa");
            Assert.True(Claves.Comprueba(DatabaseManager.TestAccountPassword, rafa.Password, out _));
            Assert.Equal(5, rafa.Role);
            // Once is enough.
            Assert.False(DatabaseManager.LockTestAccount(db));
        }

        [Fact]
        public void A_keka_with_a_password_of_its_own_is_left_alone()
        {
            using var db = Accounts("another-secret");
            Assert.False(DatabaseManager.LockTestAccount(db));
            Assert.Equal(5, Row(db, "keka").Role);
        }
    }
}
