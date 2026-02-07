using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Playful/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Kimlik")]
    public string characterName;
    public Sprite avatar; 
    public string className; 

    [Header("Ekonomi (YENİ)")]
    public bool isDefaultUnlocked = false; // Oyun başı açık mı? (Örn: Warrior true, diğerleri false)
    public int goldPrice = 1000; // Emekle alma fiyatı
    public int gemPrice = 100;   // Parayla alma fiyatı

    [Header("Temel İstatistikler")]
    public float maxHealth = 1000f;
    public float maxMana = 100f;

    [Header("Savaş Ayarları")]
    public float baseAttackDamage = 15f; 
    public float attackSpeedMultiplier = 1.0f;

    [Header("Yetenekler")]
    public SkillData activeSkill; 
    
    [Header("Pasif Çarpanlar")]
    public float redDamageMultiplier = 1.0f;
    public float blueManaMultiplier = 1.0f;
    public float greenHealMultiplier = 1.0f;
}