using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 저장된 PlayerData 파일을 전부 삭제하는 Editor 유틸리티.
/// </summary>
public class PlayerDataClearer : EditorWindow
{
    [MenuItem("Tools/Clear PlayerData")]
    public static void ClearData()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "PlayerData");

        if (!Directory.Exists(directoryPath))
        {
            Debug.Log("[PlayerDataClearer] No saved PlayerData found.");
            return;
        }

        foreach (var file in Directory.GetFiles(directoryPath))
            File.Delete(file);

        Debug.Log($"[PlayerDataClearer] PlayerData cleared.\n{directoryPath}");
    }
}
