using UnityEngine;

[CreateAssetMenu(menuName = "Playful/Skills/Heal Skill")]
public class HealSkill : SkillData
{
    public float healAmount = 150f;

    public override void Trigger(BattleManager bm, EnemyManager em, bool isPlayer)
    {
        bm.Heal(isPlayer, healAmount); // BattleManager'a Heal fonksiyonu ekleyeceğiz
        Debug.Log($"{skillName} kullanıldı! İyileşme: {healAmount}");
    }
}