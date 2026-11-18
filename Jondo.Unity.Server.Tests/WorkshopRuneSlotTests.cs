using Jondo.Unity.Server.Handlers;
using Xunit;

namespace Jondo.Unity.Server.Tests;

/// <summary>
/// The magus table's rune slot holds a stack, as on the official servers: the client drags
/// «kcr { 1: 300 }» and the slot used to keep a single rune of it.
/// </summary>
public sealed class WorkshopRuneSlotTests
{
    [Theory]
    [InlineData(0, 300, 500, 300)]   // a 300 drag from a stack of 500
    [InlineData(0, 300, 120, 120)]   // never more than the bag holds
    [InlineData(10, 5, 500, 15)]     // more of the same stack
    [InlineData(10, -4, 500, 6)]     // some taken back out
    [InlineData(10, -50, 500, 0)]    // all of them out
    [InlineData(0, 1, 1, 1)]         // the old single rune still works
    [InlineData(5, 1, 0, 0)]         // a stack gone from the bag empties the slot
    public void Slot_takes_as_many_as_dragged_up_to_the_stack(int had, int delta, int stack, int expected)
        => Assert.Equal(expected, WorkshopHandler.SlotQuantity(had, delta, stack));

    [Fact]
    public void Slot_quantity_survives_extreme_drags()
    {
        Assert.Equal(500, WorkshopHandler.SlotQuantity(int.MaxValue, int.MaxValue, 500));
        Assert.Equal(0, WorkshopHandler.SlotQuantity(0, int.MinValue, 500));
    }

    [Theory]
    [InlineData(300, false, 499, 299)]  // one rune spent, the rest stays laid
    [InlineData(300, true, 500, 300)]   // the Almanax spared it: nothing left the slot
    [InlineData(1, false, 10, 0)]       // the last one laid: the slot empties
    [InlineData(300, false, 40, 40)]    // the bag holds fewer than were laid
    [InlineData(5, false, 0, 0)]        // the stack is gone
    public void Each_fusion_spends_one_and_keeps_the_rest(int had, bool spared, int stackLeft, int expected)
        => Assert.Equal(expected, WorkshopHandler.SlotAfterFusion(had, spared, stackLeft));
}
