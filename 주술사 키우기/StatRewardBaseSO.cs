using Keiwando.BigInteger;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/RewardDatas/StatReward")]
public class StatRewardBaseSO : RewardBaseSO
{
    [SerializeField] private Enums.DataType dataType;
    [SerializeField] private bool isBigInteger;

    private int currentAmount;
    private BigInteger currentAmountBigInteger;

    public override void GiveReward(BigInteger amount, string title)
    {
        currentAmount = BigInteger.ToInt32(amount);
        currentAmountBigInteger = amount;

        if (isBigInteger) PlayerDataManager.Instance.UpdateIntData(amount, dataType);
        else PlayerDataManager.Instance.UpdateFloatData(currentAmount, dataType);

        AddSlot(title);
    }

    public override void AddSlot(string title)
    {
        StatRewardSlot slot = UIManager.Instance.GetUIElement<UI_Rewards>().GetSlot<StatRewardSlot>(title);
        string amountText = isBigInteger ? currentAmountBigInteger.ChangeMoney() : $"{currentAmount}";
        slot.SetUI(dataType, amountText);
    }

    public override RewardSlot GetRewardSlot(Reward reward)
    {
        StatRewardSlot slotPrefab = Resources.Load<StatRewardSlot>("Prefabs/RewardSlots/StatRewardSlot");
        StatRewardSlot slot = Instantiate(slotPrefab);
        slot.SetUI(dataType, reward.amount.ChangeMoney());
        return slot;
    }
}
