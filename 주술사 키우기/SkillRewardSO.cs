using System.Collections.Generic;
using Keiwando.BigInteger;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/RewardDatas/SkillReward")]
public class SkillRewardSO : RewardBaseSO
{
    [SerializeField] private Enums.Grade grade;

    private readonly System.Random random = new System.Random();
    private readonly HashSet<Skill> summonedItems = new HashSet<Skill>();
    private readonly Dictionary<Skill, int> summonedCounts = new Dictionary<Skill, int>();

    private Skill skill;
    private int amount;

    public override void GiveReward(BigInteger amount, string title)
    {
        this.amount = BigInteger.ToInt32(amount);
        SummonSkills(this.amount, title);
    }

    private void SummonSkills(int quantity, string title)
    {
        summonedItems.Clear();
        summonedCounts.Clear();

        for (int i = 0; i < quantity; i++)
        {
            Enums.SkillType skillType = Enums.skillTypes[random.Next(0, Enums.skillTypes.Length)];
            int index = random.Next(0, SkillManager.Instance.GetMaxIndexOfTheGrade(skillType, grade) + 1);

            string skillName = $"{skillType}_{grade}_{index}";
            skill = SkillManager.Instance.GetData(skillName);
            amount = quantity;

            AddSlot(title);

            summonedItems.Add(skill);
            if (summonedCounts.ContainsKey(skill)) summonedCounts[skill]++;
            else summonedCounts[skill] = 1;
        }

        foreach (Skill item in summonedItems)
            SkillManager.Instance.UpdateSkillCount(item, summonedCounts[item]);
    }

    public override void AddSlot(string title)
    {
        SkillRewardSlot slot = UIManager.Instance.GetUIElement<UI_Rewards>().GetSlot<SkillRewardSlot>(title);
        slot.SetUI(skill, amount);
    }

    public override RewardSlot GetRewardSlot(Reward reward)
    {
        EmptySkillRewardSlot slotPrefab = Resources.Load<EmptySkillRewardSlot>("Prefabs/RewardSlots/EmptySkillRewardSlot");
        EmptySkillRewardSlot slot = Instantiate(slotPrefab);
        slot.SetUI(grade, BigInteger.ToInt32(reward.amount));
        return slot;
    }
}
