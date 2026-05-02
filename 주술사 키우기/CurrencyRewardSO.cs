using Keiwando.BigInteger;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/RewardDatas/CurrencyReward")]
public class CurrencyRewardSO : RewardBaseSO
{
    [SerializeField] private Enums.CurrencyType type;

    private BigInteger amount;

    public override void GiveReward(BigInteger amount, string title)
    {
        this.amount = amount;
        CurrencyManager.Instance.TryUpdateCurrency(type, amount);
        AddSlot(title);
    }

    public override void AddSlot(string title)
    {
        CurrencyRewardSlot slot = UIManager.Instance.GetUIElement<UI_Rewards>().GetSlot<CurrencyRewardSlot>(title);
        slot.SetUI(type, amount);
    }

    public override RewardSlot GetRewardSlot(Reward reward)
    {
        CurrencyRewardSlot slotPrefab = Resources.Load<CurrencyRewardSlot>("Prefabs/RewardSlots/CurrencyRewardSlot");
        CurrencyRewardSlot slot = Instantiate(slotPrefab);
        slot.SetUI(type, reward.amount);
        return slot;
    }
}
