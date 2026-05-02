using System;
using System.Collections.Generic;
using UnityEngine;
using GameData;

/// <summary>
/// 맵 배경 타일 및 소품(Prob) 데이터를 관리하는 플레이어 데이터 클래스.
///
/// 저장 구조:
/// - *ForSave 필드는 직렬화 전용 임시 필드입니다. 런타임에서 직접 수정하지 마세요.
/// - 실제 데이터는 private 필드로 관리되며, GetReadyForSave() 호출 시 *ForSave 필드로 복사됩니다.
/// </summary>
[Serializable]
public class MapData : PlayerData
{
    #region Serialization Fields

    public List<MapBackgroundData> BackgroundDatasForSave;
    public List<MapBackgroundData> PossessedBackgroundDatasForSave;
    public List<MapProbData> ProbDatasForSave;
    public List<MapProbData> PossessedProbDatasForSave;
    public int LockedProbIndexForSave;

    #endregion

    private List<MapBackgroundData> _currentBackgrounds;
    private List<MapBackgroundData> _possessedBackgrounds;
    private List<MapProbData> _currentProbs;
    private List<MapProbData> _possessedProbs;
    private int _lockedProbIndex;

    public override void Initialize()
    {
        var defaultDataManager = DefaultDataManager.Instance;

        _currentBackgrounds = new List<MapBackgroundData>();
        _possessedBackgrounds = new List<MapBackgroundData>();

        if (BackgroundDatasForSave != null)
        {
            _currentBackgrounds.AddRange(BackgroundDatasForSave);
            _possessedBackgrounds.AddRange(PossessedBackgroundDatasForSave);
            BackgroundDatasForSave = null;
            PossessedBackgroundDatasForSave = null;
        }
        else
        {
            for (int i = 0; i < (int)Enums.TileArea.Max; i++)
            {
                var data = defaultDataManager.GetById<MapBackgroundData>((i + 1) * 100 + 1);
                _currentBackgrounds.Add(data);
                _possessedBackgrounds.Add(data);
            }
        }

        _currentProbs = new List<MapProbData>();
        _possessedProbs = new List<MapProbData>();

        if (ProbDatasForSave != null)
        {
            _currentProbs.AddRange(ProbDatasForSave);
            _possessedProbs.AddRange(PossessedProbDatasForSave);
            ProbDatasForSave = null;
            PossessedProbDatasForSave = null;
        }
        else
        {
            for (int i = 0; i < (int)Enums.ProbType.Max; i++)
            {
                var data = defaultDataManager.GetById<MapProbData>(i * 100 + 1);
                _currentProbs.Add(data);
                _possessedProbs.Add(data);
            }
        }

        _lockedProbIndex = LockedProbIndexForSave;

        MarkDirty();
    }

    public MapBackgroundData GetCurrentTileData(Enums.TileArea areaType)
        => _currentBackgrounds[(int)areaType];

    public bool IsTilePossessed(int tileID)
        => _possessedBackgrounds.Contains(DefaultDataManager.Instance.GetById<MapBackgroundData>(tileID));

    public void ObtainTile(int tileID)
    {
        _possessedBackgrounds.Add(DefaultDataManager.Instance.GetById<MapBackgroundData>(tileID));
        MarkDirty();
    }

    public void ChangeTile(Enums.TileArea areaType, MapBackgroundData bgData)
    {
        _currentBackgrounds[(int)areaType] = bgData;
        MarkDirty();
    }

    public MapProbData GetCurrentProbData(Enums.ProbType probType)
        => _currentProbs[(int)probType];

    public bool IsProbPossessed(int probID)
        => _possessedProbs.Contains(DefaultDataManager.Instance.GetById<MapProbData>(probID));

    public void ObtainProb(int probID)
    {
        _possessedProbs.Add(DefaultDataManager.Instance.GetById<MapProbData>(probID));
        MarkDirty();
    }

    public void ChangeProb(Enums.ProbType probType, MapProbData probData)
    {
        _currentProbs[(int)probType] = probData;
        MarkDirty();
    }

    public void UnlockProb(Enums.ProbType type)
    {
#if UNITY_EDITOR
        if ((int)type < Const.Map.LOCKED_PROB_ENUM_START)
            Debug.LogWarning($"[MapData] {type} is not a locked prob type.");
        if ((int)type != Const.Map.LOCKED_PROB_ENUM_START + _lockedProbIndex + 1)
            Debug.LogWarning($"[MapData] Unlock order error: {type} is not the current unlock target.");
#endif
        _lockedProbIndex++;
        MarkDirty();
    }

    public int GetUnlockedCount() => _lockedProbIndex;

    public override void GetReadyForSave()
    {
        BackgroundDatasForSave = _currentBackgrounds;
        PossessedBackgroundDatasForSave = _possessedBackgrounds;
        ProbDatasForSave = _currentProbs;
        PossessedProbDatasForSave = _possessedProbs;
        LockedProbIndexForSave = _lockedProbIndex;
    }
}
