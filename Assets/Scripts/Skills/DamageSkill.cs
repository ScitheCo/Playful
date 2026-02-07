using UnityEngine;

[CreateAssetMenu(menuName = "Playful/Skills/Damage Skill")]
public class DamageSkill : SkillData
{
    public float damageAmount = 200f;
    public GameObject effectPrefab; // Görsel efekt (Opsiyonel)

    public override void Trigger(BattleManager bm, EnemyManager em, bool isPlayer)
    {
        // Yeteneği kullanan Player ise -> Rakip hasar alır (EnemyAttack = false)
        // Yeteneği kullanan Rakip ise -> Player hasar alır (EnemyAttack = true)
        bool targetIsPlayer = !isPlayer; 
        
        bm.TakeDamage(targetIsPlayer, damageAmount);
        Debug.Log($"{skillName} kullanıldı! Hasar: {damageAmount}");
        
        // İleride buraya efekt oynatma kodu da eklenebilir
    }
}