using System;
using System.Collections.Generic;
using UnityEngine;

public class SystemDataTest : MonoBehaviour, ISystemDataReader
{
    private List<TestMissionData> _missionData;
    private TestMissionData _firstData;
    
    private void Start()
    {
        LoadData();
        Print();
    }

    private void LoadData()
    {
        _missionData = SystemDataManager.Instance.GetAll<TestMissionData>(gameObject);
        _firstData = SystemDataManager.Instance.GetById<TestMissionData>(gameObject, 101);
    }

    private void Print()
    {
        Debug.Log($"FirstData : ({_firstData.title}) {_firstData.content} ");
        foreach (var data in _missionData)
        {
            Debug.Log($"{data.id} : {data.title}");
        }
    }
}
