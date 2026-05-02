using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Net;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// Google Sheets에 게시된 CSV 데이터를 JSON으로 변환하여 Resources/JSON 폴더에 저장하는 Editor 툴.
/// 시트 목록 CSV의 각 행은 "{파일명},{시트URL}" 형식이어야 합니다.
/// CSV 헤더에서 "parent.child" 형식으로 중첩 구조를 표현할 수 있습니다.
/// </summary>
public class CsvToJsonConverterEditor : EditorWindow
{
    private string _dataUrl = "YOUR_SHEET_LIST_CSV_URL";

    [MenuItem("Tools/CSV to JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CsvToJsonConverterEditor>("CSV → JSON");
    }

    private void OnGUI()
    {
        GUILayout.Label("Google Sheet CSV → JSON Converter", EditorStyles.boldLabel);
        _dataUrl = EditorGUILayout.TextField("Sheet List URL", _dataUrl);

        if (GUILayout.Button("Convert & Save"))
        {
            ConvertCsvToJson();
        }
    }

    private void ConvertCsvToJson()
    {
        using var client = new WebClient();
        string urlText = client.DownloadString(_dataUrl);

        var dataSheets = urlText.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        var subFieldCache = new Dictionary<string, object>();

        foreach (var sheet in dataSheets)
        {
            try
            {
                var line = sheet.Trim().Split(',');
                string fileName = line[0];
                string currentUrl = line[1];

                string csvText;
                using (var stream = client.OpenRead(currentUrl))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    csvText = reader.ReadToEnd();
                }

                var lines = csvText.Split('\n')
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                var headers = lines[0].Trim().Split(',');

                string typeName = $"GameData.{fileName}, Assembly-CSharp";
                Type dataType = Type.GetType(typeName);

                if (dataType == null)
                {
                    Debug.LogError($"[CsvToJson] Type not found: {typeName}");
                    return;
                }

                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dataType));

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Trim().Split(',');
                    object obj = Activator.CreateInstance(dataType);

                    subFieldCache.Clear();

                    for (int j = 0; j < headers.Length && j < values.Length; j++)
                    {
                        string header = headers[j].Trim();
                        string value = values[j].Trim();

                        if (string.IsNullOrWhiteSpace(value)) continue;

                        if (header.Contains('.'))
                        {
                            SetNestedField(obj, dataType, header, value, subFieldCache);
                        }
                        else
                        {
                            SetField(obj, dataType, header, value);
                        }
                    }

                    list.Add(obj);
                }

                Type wrapperType = typeof(Wrapper<>).MakeGenericType(dataType);
                object wrapper = Activator.CreateInstance(wrapperType);
                wrapperType.GetField("items").SetValue(wrapper, list);

                string json = JsonUtility.ToJson(wrapper, true);
                string savePath = Path.Combine(Application.dataPath, $"Resources/JSON/{fileName}.json");
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllText(savePath, json);
                AssetDatabase.Refresh();

                Debug.Log($"[CsvToJson] Saved: {savePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvToJson] Conversion failed: {ex.Message}");
            }
        }
    }

    private void SetNestedField(object obj, Type dataType, string header, string value, Dictionary<string, object> cache)
    {
        var parts = header.Split('.');
        string parentFieldName = parts[0].Trim();
        string childFieldName = parts[1].Trim();

        var parentField = dataType.GetField(parentFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (parentField == null) return;

        if (!cache.TryGetValue(parentFieldName, out object parentValue))
        {
            parentValue = parentField.GetValue(obj) ?? Activator.CreateInstance(parentField.FieldType);
            parentField.SetValue(obj, parentValue);
            cache[parentFieldName] = parentValue;
        }

        var childField = parentField.FieldType.GetField(childFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (childField == null) return;

        childField.SetValue(parentValue, ParseValue(childField.FieldType, value));
    }

    private void SetField(object obj, Type dataType, string header, string value)
    {
        var field = dataType.GetField(header, BindingFlags.Public | BindingFlags.Instance);
        if (field == null) return;

        field.SetValue(obj, ParseValue(field.FieldType, value));
    }

    private object ParseValue(Type fieldType, string value)
    {
        if (fieldType.IsEnum)
            return Enum.Parse(fieldType, value);

        if (fieldType == typeof(float))
            return (float)Math.Round(float.Parse(value, CultureInfo.InvariantCulture), 4);

        return Convert.ChangeType(value, fieldType);
    }

    [Serializable]
    public class Wrapper<T>
    {
        public List<T> items;
    }
}
