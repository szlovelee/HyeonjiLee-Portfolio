using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어 데이터의 로드 및 저장을 담당하는 매니저.
/// Dirty Flag 패턴을 사용하여 변경된 타입만 선택적으로 저장합니다.
/// 데이터는 최초 접근 시 Lazy Load되며, Application.persistentDataPath/PlayerData/ 에 저장됩니다.
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    private readonly Dictionary<Type, PlayerData> _dataDic = new();
    private readonly HashSet<Type> _dirtyFlag = new();

    private const float SAVE_INTERVAL = 30f;
    private static string _directoryPath;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        _directoryPath = Path.Combine(Application.persistentDataPath, "PlayerData");

        if (!Directory.Exists(_directoryPath))
            Directory.CreateDirectory(_directoryPath);

        LoadInitialData();
        StartCoroutine(SaveTimer());
    }

    private void LoadInitialData()
    {
        // 게임 시작 시 반드시 필요한 데이터가 있다면 여기서 미리 로드합니다.
    }

    /// <summary>
    /// 해당 타입의 플레이어 데이터를 반환합니다. 캐시에 없으면 파일에서 로드합니다.
    /// </summary>
    public T GetData<T>() where T : PlayerData, new()
    {
        if (_dataDic.TryGetValue(typeof(T), out var data))
            return (T)data;

        return Load<T>();
    }

    private T Load<T>() where T : PlayerData, new()
    {
        var type = typeof(T);
        string path = GetFilePath(type);
        T loadedData;

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            loadedData = JsonUtility.FromJson<T>(json);
        }
        else
        {
            loadedData = new T();
        }

        loadedData.Initialize();
        loadedData.OnDirty += () => _dirtyFlag.Add(type);

        _dataDic[type] = loadedData;
        return loadedData;
    }

    private IEnumerator SaveTimer()
    {
        while (true)
        {
            yield return CoroutineTime.GetWaitForSeconds(SAVE_INTERVAL);
            Save();
        }
    }

    /// <summary>
    /// Dirty 상태인 데이터만 저장합니다.
    /// </summary>
    public void SaveImmediate() => Save();

    /// <summary>
    /// 로드된 모든 데이터를 저장합니다.
    /// </summary>
    public void SaveAll()
    {
        foreach (var data in _dataDic)
            WriteFile(data.Key);
    }

    private void Save()
    {
        foreach (var type in _dirtyFlag)
            WriteFile(type);

        _dirtyFlag.Clear();
    }

    private void WriteFile(Type type)
    {
        if (!_dataDic.TryGetValue(type, out var data)) return;

        data.GetReadyForSave();
        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(GetFilePath(type), json);
            Debug.Log($"[PlayerDataManager] Saved: {type.Name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDataManager] Save failed ({type.Name}): {ex.Message}");
        }
    }

    private string GetFilePath(Type type) => Path.Combine(_directoryPath, $"{type.Name}.json");

    private void OnApplicationQuit() => SaveImmediate();
}
