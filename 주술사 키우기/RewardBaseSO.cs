using Keiwando.BigInteger;
using UnityEngine;

public abstract class RewardBaseSO : ScriptableObject
{
    [SerializeField] private Enums.RewardType rewardType;

    public Enums.RewardType Type => rewardType;

    public abstract void GiveReward(BigInteger amount, string title);
    public abstract void AddSlot(string title);
    public abstract RewardSlot GetRewardSlot(Reward reward);
}
