using UnityEngine;

// Abstract: Tek başına kullanılamaz, türetilmesi gerekir.
public abstract class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public int manaCost = 100;
    [TextArea] public string description;

    // Her yetenek bu fonksiyonu kendi bildiği gibi dolduracak
    public abstract void Trigger(BattleManager battleManager, EnemyManager enemyManager, bool isPlayer);
}