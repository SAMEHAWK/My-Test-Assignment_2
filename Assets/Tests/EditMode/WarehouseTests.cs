using NUnit.Framework;
using UnityEngine;
using Core;
using Building;
using System.Reflection;

// Warehouse 特有行为 EditMode 测试（acceptedTypes 过滤）
// EditMode tests for Warehouse-specific behavior (acceptedTypes filter)
public class WarehouseTests
{
    private Warehouse _warehouse;
    private GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestWarehouse");
        _warehouse = _go.AddComponent<Warehouse>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void CanAddResource_RejectsUnacceptedType_WhenFilterSet()
    {
        SetAcceptedTypes(_warehouse, ResourceType.N1);

        Assert.IsTrue(_warehouse.CanAddResource(ResourceType.N1, 1));
        Assert.IsFalse(_warehouse.CanAddResource(ResourceType.N2, 1));
    }

    [Test]
    public void AddResource_DoesNotAdd_WhenTypeRejected()
    {
        SetAcceptedTypes(_warehouse, ResourceType.N1);

        _warehouse.AddResource(ResourceType.N2, 1);
        Assert.AreEqual(0, _warehouse.GetTotalStored());
    }

    private static void SetAcceptedTypes(Warehouse warehouse, params ResourceType[] types)
    {
        var field = typeof(Warehouse).GetField("acceptedTypes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(warehouse, new System.Collections.Generic.List<ResourceType>(types));
    }
}
