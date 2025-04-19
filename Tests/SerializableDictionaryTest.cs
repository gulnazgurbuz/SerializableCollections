using NUnit.Framework;
using UnityEngine;


[TestFixture]
public class SerializedDictionaryTests {
    private const string DefKey = "KEY";
    private const string DefVal = "VAL";
    private SerializableDictionary<string, string> _collection;

    [SetUp]
    public void SetUp() {
        _collection = new SerializableDictionary<string, string>();
    }

    [Test]
    public void collection_should_add_a_value_properly() {
        Assert.IsTrue(_collection.TryAdd(DefKey, DefVal));
        Assert.IsTrue(_collection.TryGetValue(DefKey, out var value));
        Assert.AreEqual(DefVal, value);
    }

    [Test]
    public void collection_should_not_add_an_existing_key() {
        Assert.IsTrue(_collection.TryAdd(DefKey, DefVal));
        Assert.IsFalse(_collection.TryAdd(DefKey, string.Empty));
        Assert.IsTrue(_collection.TryGetValue(DefKey, out var value));
        Assert.AreEqual(DefVal, value);
    }

    [Test]
    public void collection_should_serialize_properly() {
        _collection.TryAdd(DefKey, DefVal);
        var json = JsonUtility.ToJson(_collection);
        var des = JsonUtility.FromJson<SerializableDictionary<string, string>>(json);
        Assert.IsTrue(des.TryGetValue(DefKey, out var value));
        Assert.AreEqual(DefVal, value);
    }
}