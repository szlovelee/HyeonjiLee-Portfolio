using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameData;

/// <summary>
/// Resources/JSON 폴더의 JSON 파일을 런타임에 로드하여 캐싱하는 정적 데이터 매니저.
/// 파일명과 GameData 네임스페이스 내 클래스명이 일치해야 자동으로 로드됩니다.
/// </summary>
public class DefaultDataManager : Singleton<DefaultDataManager>
{
    private readonly Dictionary<Type, object> _dataDic = new();

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    private void Init()
    {
        var jsonFiles = Resources.LoadAll<TextAsset>("JSON");

        foreach (var file in jsonFiles)
        {
            string typeName = $"GameData.{file.name}, Assembly-CSharp";
            Type dataType = Type.GetType(typeName);

            if (dataType == null)
            {
                Debug.LogWarning($"[DefaultDataManager] Type not found: {typeName}");
                continue;
            }

            LoadDefaultData(dataType, file);
        }
    }

    private void LoadDefaultData(Type type, TextAsset jsonAsset)
    {
        Type wrapperType = typeof(DefaultDataWrapper<>).MakeGenericType(type);
        object wrapper = JsonUtility.FromJson(jsonAsset.text, wrapperType);

        var itemList = wrapperType.GetField("items").GetValue(wrapper) as IEnumerable;

        var dataById = (IDictionary)Activator.CreateInstance(
            typeof(Dictionary<,>).MakeGenericType(typeof(int), type));

        foreach (var item in itemList)
        {
            int id = (int)type.GetField("id").GetValue(item);
            dataById[id] = item;
        }

        _dataDic[type] = dataById;
    }

    /// <summary>
    /// id로 정적 데이터를 조회합니다.
    /// </summary>
    public T GetById<T>(int id) where T : DefaultData
    {
        if (_dataDic.TryGetValue(typeof(T), out object dictObj) &&
            dictObj is Dictionary<int, T> dict &&
            dict.TryGetValue(id, out T result))
        {
            return result;
        }

        Debug.LogWarning($"[DefaultDataManager] {typeof(T).Name} ID {id} not found.");
        return null;
    }

    /// <summary>
    /// 해당 타입의 모든 정적 데이터를 반환합니다.
    /// </summary>
    public List<T> GetAll<T>() where T : DefaultData
    {
        if (_dataDic.TryGetValue(typeof(T), out object dictObj) &&
            dictObj is Dictionary<int, T> dict)
        {
            return dict.Values.ToList();
        }

        return new List<T>();
    }
}

[Serializable]
public class DefaultDataWrapper<T>
{
    public List<T> items;
}
