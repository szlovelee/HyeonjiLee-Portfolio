using System.Collections.Generic;
using Keiwando.BigInteger;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/RewardDatas/EquipmentReward")]
public class EquipmentRewardSO : RewardBaseSO
{
    [SerializeField] private Enums.Grade grade;
    [SerializeField] private int index;

    private readonly System.Random random = new System.Random();
    private readonly HashSet<EquipmentData> summonedItems = new HashSet<EquipmentData>();
    private readonly Dictionary<EquipmentData, int> summonedCounts = new Dictionary<EquipmentData, int>();

    private EquipmentData equipment;
    private int amount;

    public override void GiveReward(BigInteger amount, string title)
    {
        this.amount = BigInteger.ToInt32(amount);
        SummonEquipment(this.amount, title);
    }

    private void SummonEquipment(int quantity, string title)
    {
        summonedItems.Clear();
        summonedCounts.Clear();

        for (int i = 0; i < quantity; i++)
        {
            Enums.EquipType equipType = Enums.equipTypes[random.Next(0, Enums.equipTypes.Length)];
            Enums.EquipmentType[] equipmentTypeData = EquipmentManager.Instance.GetBaseData(equipType).EquipmentTypes;
            Enums.EquipmentType type = equipmentTypeData[random.Next(0, equipmentTypeData.Length)];

            string name = $"{type}_{grade}_{index}";
            equipment = EquipmentManager.Instance.GetData(name);
            amount = quantity;

            AddSlot(title);

            summonedItems.Add(equipment);
            if (summonedCounts.ContainsKey(equipment)) summonedCounts[equipment]++;
            else summonedCounts[equipment] = 1;
        }

        foreach (EquipmentData item in summonedItems)
            EquipmentManager.Instance.UpdateEquipmentCount(item, summonedCounts[item]);
    }

    public override void AddSlot(string title)
    {
        EquipmentRewardSlot slot = UIManager.Instance.GetUIElement<UI_Rewards>().GetSlot<EquipmentRewardSlot>(title);
        slot.SetUI(equipment, amount);
    }

    public override RewardSlot GetRewardSlot(Reward reward)
    {
        EmptyEquipmentRewardSlot slotPrefab = Resources.Load<EmptyEquipmentRewardSlot>("Prefabs/RewardSlots/EmptyEquipmentRewardSlot");
        EmptyEquipmentRewardSlot slot = Instantiate(slotPrefab);
        slot.SetUI(grade, index, BigInteger.ToInt32(reward.amount));
        return slot;
    }
}
