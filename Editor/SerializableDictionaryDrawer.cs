using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor {
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer {
        private const int EntriesPerPage = 50;
        private readonly Dictionary<string, int> _pageLookup = new();
        private readonly Dictionary<string, object> _tempKeyLookup = new();
        private readonly Dictionary<string, object> _tempValueLookup = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var keys = property.FindPropertyRelative("_keys");
            var values = property.FindPropertyRelative("_values");
            var uniqueKey = GetUniqueKey(property);

            if (!_pageLookup.ContainsKey(uniqueKey)) _pageLookup[uniqueKey] = 0;
            if (!_tempKeyLookup.ContainsKey(uniqueKey)) _tempKeyLookup[uniqueKey] = GetDefault(keys);
            if (!_tempValueLookup.ContainsKey(uniqueKey)) _tempValueLookup[uniqueKey] = GetDefault(values);

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var totalEntries = keys.arraySize;
            var totalPages = Mathf.CeilToInt(totalEntries / (float)EntriesPerPage);
            var currentPage = Mathf.Clamp(_pageLookup[uniqueKey], 0, Mathf.Max(0, totalPages - 1));

            var start = currentPage * EntriesPerPage;
            var end = Mathf.Min(start + EntriesPerPage, totalEntries);

            // Pagination Controls
            if (totalPages > 1) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.enabled = currentPage > 0;
                if (GUILayout.Button("←", GUILayout.Width(30))) {
                    _pageLookup[uniqueKey] = Mathf.Max(0, currentPage - 1);
                }

                GUI.enabled = true;
                GUILayout.Label($"Page {currentPage + 1} / {Mathf.Max(1, totalPages)}");

                GUI.enabled = currentPage < totalPages - 1;
                if (GUILayout.Button("→", GUILayout.Width(30))) {
                    _pageLookup[uniqueKey] = Mathf.Min(totalPages - 1, currentPage + 1);
                }

                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Entries
            for (int i = start; i < end; i++) {
                var keyProp = keys.GetArrayElementAtIndex(i);
                var valueProp = values.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(keyProp, GUIContent.none);
                EditorGUILayout.PropertyField(valueProp, GUIContent.none);

                if (GUILayout.Button("X", GUILayout.Width(20))) {
                    keys.DeleteArrayElementAtIndex(i);
                    values.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // New Entry
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("New Entry", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                EditorGUILayout.LabelField("Key", GUILayout.Width(60));
                _tempKeyLookup[uniqueKey] = DrawInputFieldWithoutLabel(_tempKeyLookup[uniqueKey], 100);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                EditorGUILayout.LabelField("Value", GUILayout.Width(60));
                _tempValueLookup[uniqueKey] = DrawInputFieldWithoutLabel(_tempValueLookup[uniqueKey], 160);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Add Entry")) {
                var newKey = _tempKeyLookup[uniqueKey];
                var newValue = _tempValueLookup[uniqueKey];

                if (IsDuplicateKey(keys, newKey)) {
                    Debug.LogWarning($"Key already exists: {newKey}");
                }
                else {
                    AddToSerializedArray(keys, newKey);
                    AddToSerializedArray(values, newValue);
                    _tempKeyLookup[uniqueKey] = GetDefault(keys);
                    _tempValueLookup[uniqueKey] = GetDefault(values);
                    _pageLookup[uniqueKey] = Mathf.CeilToInt(keys.arraySize / (float)EntriesPerPage) - 1;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Clear Dictionary")) {
                keys.ClearArray();
                values.ClearArray();
            }

            EditorGUI.indentLevel--;
        }

        private string GetUniqueKey(SerializedProperty property) {
            return $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";
        }

        private object DrawInputFieldWithoutLabel(object value, float width) {
            if (value is int i) return EditorGUILayout.IntField(GUIContent.none, i, GUILayout.Width(width));
            if (value is float f) return EditorGUILayout.FloatField(GUIContent.none, f, GUILayout.Width(width));
            if (value is bool b) return EditorGUILayout.Toggle(GUIContent.none, b, GUILayout.Width(width));
            if (value is string s) return EditorGUILayout.TextField(GUIContent.none, s, GUILayout.Width(width));
            if (value is Object o)
                return EditorGUILayout.ObjectField(GUIContent.none, o, typeof(Object), true, GUILayout.Width(width));


            string str = value?.ToString() ?? "";
            return EditorGUILayout.TextField(GUIContent.none, str, GUILayout.Width(width));
        }


        private object GetDefault(SerializedProperty arrayProp) {
            var parent = arrayProp.serializedObject.FindProperty(arrayProp.propertyPath.Split('.')[0]);
            var expectedType = GetExpectedPropertyType(parent, arrayProp.name);

            return expectedType switch {
                SerializedPropertyType.Integer => 0,
                SerializedPropertyType.Float => 0f,
                SerializedPropertyType.Boolean => false,
                SerializedPropertyType.String => "",
                SerializedPropertyType.ObjectReference => null,
                _ => ""
            };
        }


        private bool IsDuplicateKey(SerializedProperty keys, object key) {
            for (var i = 0; i < keys.arraySize; i++) {
                var k = keys.GetArrayElementAtIndex(i);
                if (k.propertyType == SerializedPropertyType.Integer && key is int ik && ik == k.intValue)
                    return true;
                if (k.propertyType == SerializedPropertyType.String && key is string sk && sk == k.stringValue)
                    return true;
            }

            return false;
        }

        private void AddToSerializedArray(SerializedProperty array, object value) {
            array.arraySize++;
            var element = array.GetArrayElementAtIndex(array.arraySize - 1);

            switch (element.propertyType) {
                case SerializedPropertyType.Integer:
                    if (value is int iVal)
                        element.intValue = iVal;
                    break;
                case SerializedPropertyType.Float:
                    if (value is float fVal)
                        element.floatValue = fVal;
                    break;
                case SerializedPropertyType.Boolean:
                    if (value is bool bVal)
                        element.boolValue = bVal;
                    break;
                case SerializedPropertyType.String:
                    if (value is string sVal)
                        element.stringValue = sVal;
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (value is Object oVal)
                        element.objectReferenceValue = oVal;
                    break;
                default:
                    Debug.LogWarning($"Unsupported property type: {element.propertyType}");
                    break;
            }
        }

        private SerializedPropertyType GetExpectedPropertyType(SerializedProperty property, string relativeName) {
            var field = property.serializedObject.targetObject.GetType()
                .GetField(property.name,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

            if (field == null || !field.FieldType.IsGenericType) return SerializedPropertyType.String;

            var genericArgs = field.FieldType.GetGenericArguments();
            var index = relativeName == "_keys" ? 0 : 1;
            var targetType = genericArgs[index];

            return TypeToSerializedPropertyType(targetType);
        }

        private SerializedPropertyType TypeToSerializedPropertyType(System.Type type) {
            if (type == typeof(int)) return SerializedPropertyType.Integer;
            if (type == typeof(float)) return SerializedPropertyType.Float;
            if (type == typeof(bool)) return SerializedPropertyType.Boolean;
            if (type == typeof(string)) return SerializedPropertyType.String;
            if (typeof(Object).IsAssignableFrom(type)) return SerializedPropertyType.ObjectReference;
            return SerializedPropertyType.Generic;
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}