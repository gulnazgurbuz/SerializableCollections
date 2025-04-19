using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TK, TV> : Dictionary<TK, TV>, ISerializationCallbackReceiver {
    [SerializeField] private List<TK> _keys = new();
    [SerializeField] private List<TV> _values = new();
    
    public SerializableDictionary() {
    }

    public SerializableDictionary(IDictionary<TK, TV> dictionary) : base(dictionary) {
    }

    public SerializableDictionary(IDictionary<TK, TV> dictionary, IEqualityComparer<TK> comparer) : base(dictionary,comparer) {
    }

    public SerializableDictionary(IEnumerable<KeyValuePair<TK, TV>> collection) : base(collection) {
    }

    public SerializableDictionary(IEnumerable<KeyValuePair<TK, TV>> collection, IEqualityComparer<TK> comparer) : base(
        collection, comparer) {
    }

    public SerializableDictionary(IEqualityComparer<TK> comparer) : base(comparer) {
    }

    public SerializableDictionary(int capacity) : base(capacity) {
    }

    public SerializableDictionary(int capacity, IEqualityComparer<TK> comparer) : base(capacity, comparer) {
    }

    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public void OnBeforeSerialize() {
        _keys.Clear();
        _values.Clear();
        foreach (var pair in this) {
            _keys.Add(pair.Key);
            _values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize() {
        Clear();
        if (_keys.Count != _values.Count) {
            throw new SerializationException(
                $"there are {_keys.Count} keys and {_values.Count} values after deserialization. Make sure that both key and value types are serializable.");
        }
        for (int i = 0; i < _keys.Count; i++) {
            if (!ContainsKey(_keys[i])) {
                Add(_keys[i], _values[i]);
            }
        }
    }
}