using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Bot Davranış Ayarları")]
    public float minAttackInterval = 3f;  
    public float maxAttackInterval = 6f;  

    [Header("Olasılıklar")]
    [Range(0, 100)] public int comboChance = 30; 
    [Range(0, 100)] public int skillChance = 20; 

    [Header("Görsel Referanslar")]
    public GameObject projectilePrefab; 
    public Transform firePoint;         
    public Transform targetPoint;       

    private bool isFighting = false;
    
    // Verileri BattleManager'dan çekeceğiz
    private float damageFromProfile;
    private float speedMultiplier;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // --- MULTIPLAYER KONTROLÜ ---
        // Eğer Ranked modundaysak (yani Online oynuyorsak), bu bot scriptini yok et.
        if (GameManager.Instance.currentMode == GameMode.Ranked)
        {
            Debug.Log("Ranked Modu: Bot devre dışı bırakılıyor.");
            Destroy(gameObject); // Kendini yok et
            return;
        }
        // ----------------------------
        
        if (projectilePrefab == null || firePoint == null || targetPoint == null)
        {
            Debug.LogWarning("EnemyManager: Referanslar eksik!");
            return;
        }

        StartCoroutine(StartBattleDelayed());
    }

    IEnumerator StartBattleDelayed()
    {
        // BattleManager'ın hazırlanmasını bekle
        yield return new WaitForSeconds(0.5f);
        
        // Verileri Yükle
        if (BattleManager.Instance != null && BattleManager.Instance.enemyProfile != null)
        {
            CharacterData data = BattleManager.Instance.enemyProfile;
            damageFromProfile = data.baseAttackDamage;
            speedMultiplier = data.attackSpeedMultiplier;
            Debug.Log($"Bot Hazır: {data.characterName} (Hasar: {damageFromProfile}, Hız: {speedMultiplier}x)");
        }
        else
        {
            damageFromProfile = 15f; // Varsayılan
            speedMultiplier = 1f;
        }

        StartBattle();
    }

    public void StartBattle()
    {
        if (!isFighting)
        {
            isFighting = true;
            StartCoroutine(EnemyLogicLoop());
        }
    }

    public void StopBattle()
    {
        isFighting = false;
        StopAllCoroutines();
    }

    IEnumerator EnemyLogicLoop()
    {
        yield return new WaitForSeconds(2f); // Başlangıç nezaketi

        while (isFighting)
        {
            // Bekleme süresini karaktere göre ayarla (Hızlı karakter az bekler)
            float baseWait = Random.Range(minAttackInterval, maxAttackInterval);
            float actualWait = baseWait / speedMultiplier; 
            yield return new WaitForSeconds(actualWait);

            // %20 ihtimalle Skill, yoksa Normal Saldırı
            int roll = Random.Range(0, 100);

            if (roll < skillChance)
            {
                // Skill Kullan (Mermi yok, direkt yetenek çalışır)
                if (BattleManager.Instance != null)
                    BattleManager.Instance.EnemyUseSkill();
            }
            else
            {
                // Normal Saldırı (Mermi atar)
                SpawnProjectile(damageFromProfile);

                // Kombo Şansı
                int comboRoll = Random.Range(0, 100);
                if (comboRoll < comboChance)
                {
                    yield return new WaitForSeconds(0.5f);
                    SpawnProjectile(damageFromProfile);
                }
            }
        }
    }

    void SpawnProjectile(float damage)
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // UI hiyerarşisinde doğru yere koy
            proj.transform.SetParent(firePoint.parent, true);
            proj.transform.localScale = Vector3.one;

            ProjectileController pc = proj.GetComponent<ProjectileController>();
            if (pc != null)
            {
                // isEnemyAttack = true, Hedef = targetPoint
                pc.Initialize(damage, true, targetPoint.position);
            }
        }
    }
}