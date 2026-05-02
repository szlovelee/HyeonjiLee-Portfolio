using System;

namespace GameData
{
    /// <summary>
    /// 모든 정적 게임 데이터의 기반 클래스.
    /// id를 공통 식별자로 가지며, DefaultDataManager를 통해 로드 및 조회됩니다.
    /// </summary>
    [Serializable]
    public abstract class DefaultData
    {
        public int id;
    }

    [Serializable]
    public class AgedBaseData : DefaultData
    {
        public Enums.Flavor flavor;
        public int count;
    }

    /// <summary>
    /// 맵 컴포넌트 데이터의 공통 기반 클래스.
    /// </summary>
    [Serializable]
    public abstract class MapComponentData : DefaultData
    {
        public string name;
        public string title;
        public int cost;
    }

    [Serializable]
    public class MapBackgroundData : MapComponentData
    {
        public Enums.TileArea tileArea;
        public HorizontalPointTile horizontalPoint;
        public VerticalPointTile verticalPoint;
    }

    [Serializable]
    public class HorizontalPointTile
    {
        public int rowIndex;
        public string tileName;
    }

    [Serializable]
    public class VerticalPointTile
    {
        public int colIndex;
        public string tileName;
    }

    [Serializable]
    public class MapProbData : MapComponentData
    {
        public Enums.ProbType type;
    }

    [Serializable]
    public class UpgradeData : DefaultData
    {
        public int upgradeLevel;
        public Enums.StoreUpgradeType upgradeType;
        public float effectValue;
        public int cost;
    }

    [Serializable]
    public class LabDefaultUpgradeData : DefaultData
    {
        public int upgradeLevel;
        public Enums.LabUpgradeType upgradeType;
        public int effectValue;
        public int cost;
    }

    [Serializable]
    public class WorkerDefaultData : DefaultData
    {
        public string name;
        public int maxStats1;
        public int maxStats2;
        public int maxStats3;
    }

    [Serializable]
    public class WorkerUpgradeDefaultData : DefaultData
    {
        public int stat;
        public int gold;
        public int ticket;
    }
}
