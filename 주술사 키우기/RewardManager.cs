using System.Collections.Generic;
using Keiwando.BigInteger;
using UnityEngine;

public class RewardManager : Singleton<RewardManager>
{
    private Dictionary<Enums.RewardType, RewardBaseSO> dataDic;
    private Dictionary<Enums.RewardType, GameObject> iconDic;

    private UI_Rewards ui_rewards;

    private void Start()
    {
        dataDic = new Dictionary<Enums.RewardType, RewardBaseSO>();
        iconDic = new Dictionary<Enums.RewardType, GameObject>();

        ui_rewards = UIManager.Instance.GetUIElement<UI_Rewards>();
    }

    public void GiveReward(List<Reward> rewards, string title)
    {
        foreach (Reward reward in rewards)
        {
            Enums.RewardType type = reward.rewardType;
            BigInteger amount = reward.amount;

            RewardBaseSO data = GetRewardBaseData(type);
            data.GiveReward(amount, title);
        }

        ui_rewards.ShowUI();
    }

    public GameObject GetIcon(Enums.RewardType type)
    {
        if (!iconDic.ContainsKey(type))
            iconDic[type] = Resources.Load<GameObject>($"Sprites/RewardIcons/{type}");

        return iconDic[type];
    }

    public RewardBaseSO GetRewardBaseData(Enums.RewardType rewardType)
    {
        if (!dataDic.TryGetValue(rewardType, out RewardBaseSO data) || !data)
        {
            data = Resources.Load<RewardBaseSO>($"ScriptableObjects/RewardDataSO/{rewardType}RewardData");
            dataDic[rewardType] = data;
        }

        return data;
    }
}
