using NUnit.Framework;
using UnityEngine;
using Core;
using Player;
using System.Reflection;

// PlayerBackpack 特有行为 EditMode 测试
// EditMode tests for PlayerBackpack-specific behavior
public class PlayerBackpackTests
{
    private PlayerBackpack _backpack;
    private GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestBackpack");
        _backpack = _go.AddComponent<PlayerBackpack>();
        // EditMode 不会自动调用 Awake，需手动触发初始化 / Awake is not auto-called in EditMode
        InvokeAwake(_backpack);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void OnBackpackChanged_FiresAfterAdd()
    {
        int callCount = 0;
        _backpack.OnBackpackChanged.AddListener(() => callCount++);

        _backpack.AddResource(ResourceType.N1, 1);

        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void DefaultCapacity_IsTwenty()
    {
        for (int i = 0; i < 20; i++)
            _backpack.AddResource(ResourceType.N1, 1);

        Assert.IsFalse(_backpack.CanAddResource(ResourceType.N1, 1));
        Assert.AreEqual(20, _backpack.GetTotalStored());
    }

    private static void InvokeAwake(object target)
    {
        var method = target.GetType().GetMethod("Awake",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(target, null);
    }
}
