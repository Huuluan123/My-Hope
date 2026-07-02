using UnityEditor;
using UnityEngine;
using System.IO;

public class StateKeyRefactorTool : EditorWindow
{
    private string oldKey = "";
    private string newKey = "";

    // Creates a new menu item on the top toolbar in Unity
    [MenuItem("Tools/Management/Refactor State Key Globally")]
    public static void ShowWindow()
    {
        GetWindow<StateKeyRefactorTool>("State Key Refactor");
    }

    private void OnGUI()
    {
        GUILayout.Label("State Key Refactor Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        oldKey = EditorGUILayout.TextField("Old Key Name:", oldKey);
        newKey = EditorGUILayout.TextField("New Key Name:", newKey);

        GUILayout.Space(20);

        if (GUILayout.Button("Refactor Keys", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(oldKey) || string.IsNullOrEmpty(newKey))
            {
                EditorUtility.DisplayDialog("Error", "Please enter both Old and New Key names!", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm", $"Are you sure you want to change all Keys from '{oldKey}' to '{newKey}' across the entire project?", "Refactor", "Cancel"))
            {
                RefactorKeysGlobally(oldKey, newKey);
            }
        }
    }

    private void RefactorKeysGlobally(string oldK, string newK)
    {
        int count = 0;

        // 1. FIND AND MODIFY ALL PREFABS IN THE PROJECT
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool isModified = false;

            // Scan StateDependentActivator
            var activators = prefab.GetComponentsInChildren<StateDependentActivator>(true);
            foreach (var a in activators)
            {
                if (a.stateKey == oldK)
                {
                    Undo.RecordObject(a, "Refactor State Key");
                    a.stateKey = newK;
                    isModified = true;
                    count++;
                }
            }

            // Scan StateObjectDeactivator
            var deactivators = prefab.GetComponentsInChildren<StateObjectDeactivator>(true);
            foreach (var d in deactivators)
            {
                if (d.saveKeyOnDeactivate == oldK)
                {
                    Undo.RecordObject(d, "Refactor State Key");
                    d.saveKeyOnDeactivate = newK;
                    isModified = true;
                    count++;
                }
            }

            // Save Prefab if modified
            if (isModified) EditorUtility.SetDirty(prefab);
        }

        // 2. FIND AND MODIFY OBJECTS IN THE CURRENT SCENE
        var sceneActivators = FindObjectsByType<StateDependentActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var a in sceneActivators)
        {
            if (a.stateKey == oldK)
            {
                Undo.RecordObject(a, "Refactor State Key");
                a.stateKey = newK;
                EditorUtility.SetDirty(a);
                count++;
            }
        }

        var sceneDeactivators = FindObjectsByType<StateObjectDeactivator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in sceneDeactivators)
        {
            if (d.saveKeyOnDeactivate == oldK)
            {
                Undo.RecordObject(d, "Refactor State Key");
                d.saveKeyOnDeactivate = newK;
                EditorUtility.SetDirty(d);
                count++;
            }
        }

        // 3. MODIFY WORLD STATE MANAGER (Both Default and Visible States)
        var wsm = FindFirstObjectByType<WorldStateManager>();
        if (wsm != null)
        {
            Undo.RecordObject(wsm, "Refactor State Key");
            bool wsmModified = false;

            foreach (var state in wsm.defaultStates)
            {
                if (state.stateName == oldK) { state.stateName = newK; wsmModified = true; count++; }
            }
            foreach (var state in wsm.visibleStates)
            {
                if (state.stateName == oldK) { state.stateName = newK; wsmModified = true; }
            }

            if (wsmModified) EditorUtility.SetDirty(wsm);
        }

        // 4. MODIFY DIRECTLY IN JSON FILE IF EXISTS
        string jsonPath = Path.Combine(Application.dataPath, "MasterSave.json");
        if (File.Exists(jsonPath))
        {
            string jsonContent = File.ReadAllText(jsonPath);
            // Specifically replace the stateName field in JSON
            string oldJsonTarget = $"\"stateName\": \"{oldK}\"";
            string newJsonTarget = $"\"stateName\": \"{newK}\"";

            if (jsonContent.Contains(oldJsonTarget))
            {
                jsonContent = jsonContent.Replace(oldJsonTarget, newJsonTarget);
                File.WriteAllText(jsonPath, jsonContent);
                Debug.Log("[Refactor] MasterSave.json file updated successfully!");
            }
        }

        // 5. Save all changes to disk
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", $"Successfully found and renamed {count} instances from '{oldK}' to '{newK}'!", "Awesome");
    }
}