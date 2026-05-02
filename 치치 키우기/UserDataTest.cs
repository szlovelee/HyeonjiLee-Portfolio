using System;
using System.Collections.Generic;
using UnityEngine;

public class UserDataTest : MonoBehaviour, ISystemDataReader, IUserDataUpdater
{
    private SystemDataManager _systemDataM;
    private UserDataManager _userDataM;

    private int _firstUncompleted;
    private TestMissionCompleteData _completeData;
    
    public void Start()
    {
        _systemDataM = SystemDataManager.Instance;
        _userDataM = UserDataManager.Instance;
        
        _firstUncompleted = -1;
        _completeData = _userDataM.GetData<TestMissionCompleteData>(gameObject);

        
        CheckCompleteStatus();
        SetNextAsCompleted();
    }

    private void CheckCompleteStatus()
    {
        List<TestMissionData> missions = _systemDataM.GetAll<TestMissionData>(gameObject);
        
        foreach (var mission in missions)
        {
            bool completed = _completeData.IsCompleted(gameObject, mission.id);
            Debug.Log($"Mission {mission.id} - Completed : {completed}");

            if (_firstUncompleted == -1 && !completed) _firstUncompleted = mission.id;
        }
    }

    private void SetNextAsCompleted()
    {
        if (_firstUncompleted == -1) return;
        
        Debug.Log($"Setting mission # {_firstUncompleted} as completed...");
        _completeData.SetAsCompleted(gameObject, _firstUncompleted);
        _userDataM.SaveImmediate();
    }
}
