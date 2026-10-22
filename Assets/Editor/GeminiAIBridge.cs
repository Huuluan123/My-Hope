using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

public class SimpleGeminiBridge : EditorWindow
{
    private string apiKey = "";
    private string prompt = "";
    private string logMessage = "Sẵn sàng...";
    private Vector2 scrollPos;

    [MenuItem("Tools/Simple Gemini Bridge")]
    public static void OpenWindow()
    {
        GetWindow<SimpleGeminiBridge>("Gemini Bridge");
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString("Gemini_API_Key", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini Unity Mini Bridge", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.PasswordField("Gemini API Key:", apiKey);
        if (GUI.changed)
        {
            EditorPrefs.SetString("Gemini_API_Key", apiKey);
        }

        EditorGUILayout.Space(6);
        GUILayout.Label("Yêu cầu (Prompt):");
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(60));

        if (GUILayout.Button("Gửi yêu cầu", GUILayout.Height(30)))
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                EditorUtility.DisplayDialog("Thiếu API Key", "Vui lòng nhập Gemini API Key từ Google AI Studio.", "OK");
                return;
            }
            _ = SendToGemini(prompt);
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Nhật ký thực thi:");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(140));
        EditorGUILayout.TextArea(logMessage, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private async Task SendToGemini(string userPrompt)
    {
        logMessage = "Đang gửi yêu cầu đến Gemini...";
        Repaint();

        // Sử dụng model gemini-3.6-flash để phản hồi nhanh và tiết kiệm quota
        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={apiKey}";
        string payload = BuildRequestJson(userPrompt);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                logMessage = $"[Lỗi API]: {request.error}\n{request.downloadHandler.text}";
                Repaint();
                return;
            }

            ProcessGeminiResponse(request.downloadHandler.text);
        }
    }

    private string BuildRequestJson(string userText)
    {
        string escapedPrompt = "\"" + userText
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "") + "\"";
        return $@"{{
          ""contents"": [{{
            ""role"": ""user"",
            ""parts"": [{{ ""text"": {escapedPrompt} }}]
          }}],
          ""tools"": [{{
            ""functionDeclarations"": [
              {{
                ""name"": ""patch_script"",
                ""description"": ""Thay thế một đoạn code cũ bằng đoạn code mới trong file C#."",
                ""parameters"": {{
                  ""type"": ""OBJECT"",
                  ""properties"": {{
                    ""relative_path"": {{ ""type"": ""STRING"", ""description"": ""Đường dẫn từ thư mục gốc, vd: Assets/Scripts/Player.cs"" }},
                    ""target_snippet"": {{ ""type"": ""STRING"", ""description"": ""Đoạn code chính xác cần thay thế"" }},
                    ""replacement_snippet"": {{ ""type"": ""STRING"", ""description"": ""Đoạn code mới thay thế vào"" }}
                  }},
                  ""required"": [""relative_path"", ""target_snippet"", ""replacement_snippet""]
                }}
              }},
              {{
                ""name"": ""create_ui_toolkit_asset"",
                ""description"": ""Tạo file giao diện UXML và stylesheet USS trong Assets/UI."",
                ""parameters"": {{
                  ""type"": ""OBJECT"",
                  ""properties"": {{
                    ""file_name"": {{ ""type"": ""STRING"", ""description"": ""Tên file không gồm đuôi mở rộng, vd: MainMenu"" }},
                    ""uxml_code"": {{ ""type"": ""STRING"", ""description"": ""Nội dung file UXML"" }},
                    ""uss_code"": {{ ""type"": ""STRING"", ""description"": ""Nội dung file USS"" }}
                  }},
                  ""required"": [""file_name"", ""uxml_code"", ""uss_code""]
                }}
              }},
              {{
                ""name"": ""spawn_primitive_object"",
                ""description"": ""Tạo GameObject hình khối cơ bản trong Hierarchy (Cube, Sphere, Capsule, Cylinder, Plane)."",
                ""parameters"": {{
                  ""type"": ""OBJECT"",
                  ""properties"": {{
                    ""object_name"": {{ ""type"": ""STRING"", ""description"": ""Tên hiển thị của object"" }},
                    ""primitive_type"": {{ ""type"": ""STRING"", ""description"": ""Cube, Sphere, Capsule, Cylinder, Plane"" }}
                  }},
                  ""required"": [""object_name"", ""primitive_type""]
                }}
              }}
            ]
          }}]
        }}";
    }

    private void ProcessGeminiResponse(string rawJson)
    {
        try
        {
            if (rawJson.Contains("\"name\": \"patch_script\""))
            {
                ExecutePatchScript(rawJson);
            }
            else if (rawJson.Contains("\"name\": \"create_ui_toolkit_asset\""))
            {
                ExecuteCreateUIToolkit(rawJson);
            }
            else if (rawJson.Contains("\"name\": \"spawn_primitive_object\""))
            {
                ExecuteSpawnPrimitive(rawJson);
            }
            else
            {
                logMessage = "Gemini phản hồi văn bản (không gọi action):\n" + ExtractTextResponse(rawJson);
            }
        }
        catch (Exception ex)
        {
            logMessage = "[Lỗi xử lý]: " + ex.Message;
        }
        Repaint();
    }

    private void ExecutePatchScript(string json)
    {
        string path = ExtractJsonField(json, "relative_path");
        string target = ExtractJsonField(json, "target_snippet");
        string replacement = ExtractJsonField(json, "replacement_snippet");

        string fullPath = Path.Combine(Application.dataPath, "..", path);
        if (!File.Exists(fullPath))
        {
            logMessage = $"[Thất bại]: Không tìm thấy file {path}";
            return;
        }

        string content = File.ReadAllText(fullPath);
        if (!content.Contains(target))
        {
            logMessage = "[Thất bại]: Đoạn code mục tiêu không khớp chính xác trong file.";
            return;
        }

        content = content.Replace(target, replacement);
        File.WriteAllText(fullPath, content);
        AssetDatabase.Refresh();

        logMessage = $"[Thành công]: Đã sửa code trong {path} (Giữ nguyên .meta GUID) và refresh Unity!";
    }

    private void ExecuteCreateUIToolkit(string json)
    {
        string fileName = ExtractJsonField(json, "file_name");
        string uxml = ExtractJsonField(json, "uxml_code");
        string uss = ExtractJsonField(json, "uss_code");

        string folderPath = Path.Combine(Application.dataPath, "UI");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string uxmlPath = Path.Combine(folderPath, $"{fileName}.uxml");
        string ussPath = Path.Combine(folderPath, $"{fileName}.uss");

        File.WriteAllText(uxmlPath, uxml);
        File.WriteAllText(ussPath, uss);
        AssetDatabase.Refresh();

        logMessage = $"[Thành công]: Đã tạo {fileName}.uxml và {fileName}.uss trong Assets/UI/!";
    }

    private void ExecuteSpawnPrimitive(string json)
    {
        string objName = ExtractJsonField(json, "object_name");
        string primTypeStr = ExtractJsonField(json, "primitive_type");

        if (!Enum.TryParse(primTypeStr, true, out PrimitiveType primType))
        {
            primType = PrimitiveType.Cube;
        }

        GameObject go = GameObject.CreatePrimitive(primType);
        go.name = string.IsNullOrEmpty(objName) ? "Gemini_Object" : objName;

        // Đăng ký hoàn tác Ctrl + Z
        Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
        Selection.activeGameObject = go;

        logMessage = $"[Thành công]: Đã tạo {go.name} ({primType}) trong Hierarchy! (Có thể bấm Ctrl + Z để hủy).";
    }

    // Helper trích xuất text JSON cơ bản
    private string ExtractJsonField(string json, string key)
    {
        string pattern = $"\"{key}\": \"";
        int start = json.IndexOf(pattern, StringComparison.Ordinal);
        if (start == -1) return "";
        start += pattern.Length;

        StringBuilder sb = new StringBuilder();
        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '\\' && i + 1 < json.Length)
            {
                char next = json[i + 1];
                if (next == 'n') sb.Append('\n');
                else if (next == 'r') sb.Append('\r');
                else if (next == 't') sb.Append('\t');
                else if (next == '"') sb.Append('"');
                else if (next == '\\') sb.Append('\\');
                else sb.Append(next);
                i++;
            }
            else if (json[i] == '"')
            {
                break;
            }
            else
            {
                sb.Append(json[i]);
            }
        }
        return sb.ToString();
    }

    private string ExtractTextResponse(string json)
    {
        string text = ExtractJsonField(json, "text");
        return string.IsNullOrEmpty(text) ? json : text;
    }
}