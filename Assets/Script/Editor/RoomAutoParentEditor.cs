using UnityEngine;
using UnityEditor;

// Báo cho Unity biết script này dùng để vẽ lại giao diện cho script RoomAutoParent
[CustomEditor(typeof(RoomAutoParent))]
public class RoomAutoParentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ các biến mặc định (nếu bạn có khai báo thêm biến public nào đó)
        DrawDefaultInspector();

        // Ép kiểu để gọi các hàm bên trong script chính
        RoomAutoParent script = (RoomAutoParent)target;

        EditorGUILayout.Space(10); // Cách ra một chút cho đẹp
        EditorGUILayout.LabelField("Level Design Tools", EditorStyles.boldLabel);

        // Tạo nút Gom Đồ Vật (Chiều cao 30 pixel cho dễ bấm)
        if (GUILayout.Button("Auto Parent", GUILayout.Height(30)))
        {
            script.AutoParentElements();
        }

        EditorGUILayout.Space(5);

        // Tạo nút Thả Đồ Vật
        if (GUILayout.Button("Release Elements", GUILayout.Height(30)))
        {
            script.ReleaseElements();
        }
    }
}