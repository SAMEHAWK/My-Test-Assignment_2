using NUnit.Framework;
using Core;

// ResourceInventory Domain 逻辑 EditMode 测试（Warehouse / Backpack 共用）
// EditMode tests for shared ResourceInventory domain logic
public class ResourceInventoryTests
{
    private ResourceInventory _inventory;

    [SetUp]
    public void SetUp()
    {
        _inventory = new ResourceInventory(10);
    }

    [Test]
    public void TryAdd_IncreasesCount()
    {
        Assert.IsTrue(_inventory.TryAdd(ResourceType.N1, 3));
        Assert.AreEqual(3, _inventory.GetResourceCount(ResourceType.N1));
    }

    [Test]
    public void CanAdd_ReturnsFalse_WhenFull()
    {
        for (int i = 0; i < 10; i++)
            _inventory.TryAdd(ResourceType.N1, 1);

        Assert.IsFalse(_inventory.CanAdd(ResourceType.N2, 1));
    }

    [Test]
    public void TryRemove_DecreasesCount()
    {
        _inventory.TryAdd(ResourceType.N1, 2);
        Assert.IsTrue(_inventory.TryRemove(ResourceType.N1, 1));
        Assert.AreEqual(1, _inventory.GetResourceCount(ResourceType.N1));
    }

    [Test]
    public void HasResources_ReturnsTrue_WhenEnough()
    {
        _inventory.TryAdd(ResourceType.N2, 2);
        Assert.IsTrue(_inventory.HasResources(ResourceType.N2, 2));
        Assert.IsFalse(_inventory.HasResources(ResourceType.N2, 3));
    }

    [Test]
    public void RemoveUpTo_ClearsType_WhenZero()
    {
        _inventory.TryAdd(ResourceType.N2, 1);
        Assert.AreEqual(1, _inventory.RemoveUpTo(ResourceType.N2, 1));
        Assert.AreEqual(0, _inventory.GetTotalStored());
    }

    [Test]
    public void CanAdd_WithFilter_RejectsUnacceptedType()
    {
        Assert.IsFalse(_inventory.CanAdd(ResourceType.N3, 1, type => type == ResourceType.N1));
        Assert.IsTrue(_inventory.CanAdd(ResourceType.N1, 1, type => type == ResourceType.N1));
    }
}
