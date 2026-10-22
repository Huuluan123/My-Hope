#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

internal static class WorldStateKeyDropdownUtility
{
    private static List<string> cachedStateKeys;
    private static bool isCacheValid;

    public static string[] GetStateKeys(
        string currentValue,
        bool includeNone)
    {
        EnsureStateKeyCache();

        List<string> options =
            new List<string>();

        if (includeNone)
        {
            options.Add("<None>");
        }

        options.AddRange(cachedStateKeys);

        bool hasMatchingKey = false;

        foreach (string key in cachedStateKeys)
        {
            if (NormalizeKey(key) == NormalizeKey(currentValue))
            {
                hasMatchingKey = true;
                break;
            }
        }

        // Chỉ thêm Missing nếu thật sự không có key tương ứng.
        if (!string.IsNullOrEmpty(currentValue)
            && !hasMatchingKey)
        {
            options.Add(currentValue + " (Missing)");
        }

        if (!includeNone && options.Count == 0)
        {
            options.Add("<No state keys found>");
        }

        return options.ToArray();
    }

    private static void AddKeys(
        List<string> keys,
        List<StateEntry> states)
    {
        if (states == null)
        {
            return;
        }

        foreach (StateEntry state in states)
        {
            if (state == null
                || string.IsNullOrEmpty(state.stateName))
            {
                continue;
            }

            if (!keys.Contains(state.stateName))
            {
                keys.Add(state.stateName);
            }
        }
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Trim().Normalize(
            System.Text.NormalizationForm.FormC);
    }

    private static int FindMatchingKeyIndex(
        string[] options,
        string currentValue)
    {
        string normalizedCurrentValue =
            NormalizeKey(currentValue);

        if (string.IsNullOrEmpty(normalizedCurrentValue))
        {
            return -1;
        }

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];

            if (option.StartsWith("<")
                || option.EndsWith(" (Missing)"))
            {
                continue;
            }

            if (NormalizeKey(option) == normalizedCurrentValue)
            {
                return i;
            }
        }

        return -1;
    }

    public static void DrawStateKeyPopup(
        SerializedProperty property,
        string label,
        bool includeNone)
    {
        string currentValue = property.stringValue;
        string[] options = GetStateKeys(currentValue, includeNone);

        int currentIndex = FindMatchingKeyIndex(
            options,
            currentValue);

        if (currentIndex < 0
            && !string.IsNullOrEmpty(currentValue))
        {
            currentIndex = Array.IndexOf(
                options,
                currentValue + " (Missing)");
        }

        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int selectedIndex = EditorGUILayout.Popup(
            label,
            currentIndex,
            options);

        if (options.Length == 0)
        {
            property.stringValue = string.Empty;
            return;
        }

        string selectedValue = options[selectedIndex];

        if (includeNone && selectedValue == "<None>")
        {
            property.stringValue = string.Empty;
        }
        else if (selectedValue.EndsWith(" (Missing)"))
        {
            // Giữ lại key cũ để không làm mất dữ liệu.
            property.stringValue = currentValue;
        }
        else if (!selectedValue.StartsWith("<"))
        {
            // Ghi lại đúng key chuẩn lấy từ WorldStateManager.
            // Việc này cũng tự sửa khoảng trắng hoặc Unicode sai lệch.
            property.stringValue = selectedValue;
        }
    }

    private static void EnsureStateKeyCache()
    {
        if (!isCacheValid || cachedStateKeys == null)
        {
            RefreshStateKeyCache();
        }
    }

    public static void InvalidateStateKeyCache()
    {
        isCacheValid = false;
    }

    public static void RefreshStateKeyCache()
    {
        List<string> keys = new List<string>();

        // Chỉ tìm prefab GAME_MANAGERS thay vì quét toàn bộ prefab.
        string[] prefabGuids =
            AssetDatabase.FindAssets("GAME_MANAGERS t:Prefab");

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath =
                AssetDatabase.GUIDToAssetPath(prefabGuid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                continue;
            }

            WorldStateManager prefabManager =
                prefab.GetComponentInChildren<WorldStateManager>(true);

            if (prefabManager == null)
            {
                continue;
            }

            AddKeys(keys, prefabManager.defaultStates);
            AddKeys(keys, prefabManager.visibleStates);
        }

        // Đọc thêm manager trực tiếp trong Scene nếu có.
        WorldStateManager[] sceneManagers =
            Resources.FindObjectsOfTypeAll<WorldStateManager>();

        foreach (WorldStateManager sceneManager in sceneManagers)
        {
            if (sceneManager == null
                || !sceneManager.gameObject.scene.IsValid())
            {
                continue;
            }

            AddKeys(keys, sceneManager.defaultStates);
            AddKeys(keys, sceneManager.visibleStates);
        }

        cachedStateKeys = keys;
        isCacheValid = true;
    }
}

[CustomEditor(typeof(Trigger_WorldStateChange))]
public class TriggerWorldStateChangeEditor : Editor
{
    private SerializedProperty stateKey;
    private SerializedProperty targetStateValue;

    private void OnEnable()
    {
        stateKey = serializedObject.FindProperty("stateKey");
        targetStateValue = serializedObject.FindProperty("targetStateValue");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(
            "Configure Event Flag",
            EditorStyles.boldLabel);

        WorldStateKeyDropdownUtility.DrawStateKeyPopup(
            stateKey,
            "State Key",
            includeNone: false);

        EditorGUILayout.PropertyField(
            targetStateValue,
            new GUIContent("Target State Value"));

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(StateDependentActivator))]
public class StateDependentActivatorEditor : Editor
{
    private SerializedProperty stateKey;
    private SerializedProperty activeWhenStateIsTrue;

    private void OnEnable()
    {
        stateKey = serializedObject.FindProperty("stateKey");
        activeWhenStateIsTrue =
            serializedObject.FindProperty("activeWhenStateIsTrue");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(
            "State Configuration",
            EditorStyles.boldLabel);

        WorldStateKeyDropdownUtility.DrawStateKeyPopup(
            stateKey,
            "State Key",
            includeNone: false);

        EditorGUILayout.PropertyField(
            activeWhenStateIsTrue,
            new GUIContent("Active When State Is True"));

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(StateObjectDeactivator))]
public class StateObjectDeactivatorEditor : Editor
{
    private SerializedProperty watchStateKey;
    private SerializedProperty targetStateValueOnDeactivate;
    private SerializedProperty saveKeyOnDeactivate;

    private void OnEnable()
    {
        watchStateKey =
            serializedObject.FindProperty("watchStateKey");

        targetStateValueOnDeactivate =
            serializedObject.FindProperty(
                "targetStateValueOnDeactivate");

        saveKeyOnDeactivate =
            serializedObject.FindProperty("saveKeyOnDeactivate");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(
            "State Condition",
            EditorStyles.boldLabel);

        WorldStateKeyDropdownUtility.DrawStateKeyPopup(
            watchStateKey,
            "Watch State Key",
            includeNone: false);

        EditorGUILayout.PropertyField(
            targetStateValueOnDeactivate,
            new GUIContent("Deactivate When Value Is"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "State Save (Optional)",
            EditorStyles.boldLabel);

        WorldStateKeyDropdownUtility.DrawStateKeyPopup(
            saveKeyOnDeactivate,
            "Save Key On Deactivate",
            includeNone: true);

        serializedObject.ApplyModifiedProperties();
    }
}

[InitializeOnLoad]
internal static class StateKeyDropdownEditorEvents
{
    static StateKeyDropdownEditorEvents()
    {
        EditorApplication.projectChanged +=
            WorldStateKeyDropdownUtility.InvalidateStateKeyCache;

        EditorApplication.hierarchyChanged +=
            WorldStateKeyDropdownUtility.InvalidateStateKeyCache;
    }
}

internal static class StateKeyDropdownRefreshMenu
{
    [MenuItem("Tools/World State/Refresh State Key Cache")]
    private static void RefreshStateKeyCache()
    {
        WorldStateKeyDropdownUtility.RefreshStateKeyCache();

        Debug.Log(
            "[WorldState] State key cache refreshed.");
    }
}
#endif
