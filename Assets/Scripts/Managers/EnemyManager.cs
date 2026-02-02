using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Bot Zorluk Ayarları")]
    public float minAttackInterval = 3f;  
    public float maxAttackInterval = 6f;  
    public float baseDamage = 15f;        
    public float skillDamage = 40f;       

    [Header("Davranış Ayarları")]
    [Range(0, 100)] public int comboChance = 30; 
    [Range(0, 100)] public int skillChance = 10; 

    [Header("Mermi Sistemi Referansları")]
    public GameObject projectilePrefab; // Mermi Görseli (Prefab)
    public Transform firePoint;         // Mermi nereden çıkacak? (Rakip Avatarı)
    public Transform targetPoint;       // Mermi nereye gidecek? (Player Avatarı veya Bar)

    private bool isFighting = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Güvenlik: Referanslar eksikse hata verip durdurmasın, uyarısın.
        if (projectilePrefab == null || firePoint == null || targetPoint == null)
        {
            Debug.LogWarning("EnemyManager: Mermi referansları eksik! Bot çalışmayacak.");
            return;
        }

        StartBattle();
    }

    public void StartBattle()
    {
        if (!isFighting)
        {
            isFighting = true;
            StartCoroutine(EnemyLogicLoop());
            Debug.Log("EnemyManager: Savaş Başladı.");
        }
    }

    public void StopBattle()
    {
        isFighting = false;
        StopAllCoroutines();
    }

    IEnumerator EnemyLogicLoop()
    {
        yield return new WaitForSeconds(2f);

        while (isFighting)
        {
            float waitTime = Random.Range(minAttackInterval, maxAttackInterval);
            yield return new WaitForSeconds(waitTime);

            int roll = Random.Range(0, 100);

            if (roll < skillChance)
            {
                // Skill Atışı
                SpawnProjectile(skillDamage);
                Debug.Log("<color=red>RAKİP SKILL ATTI!</color>");
            }
            else
            {
                // Normal Atış
                SpawnProjectile(baseDamage);

                // Kombo
                int comboRoll = Random.Range(0, 100);
                if (comboRoll < comboChance)
                {
                    yield return new WaitForSeconds(0.5f); // Yarım saniye sonra ikinci mermi
                    SpawnProjectile(baseDamage);
                    Debug.Log("<color=orange>Rakip Kombo Atışı!</color>");
                }
            }
        }
    }

    // === YENİ: MERMİ FIRLATMA ===
    void SpawnProjectile(float damage)
    {
        // 1. Mermiyi Yarat (Ateşlenme noktasında)
        // Canvas üzerinde çalıştığımız için parent olarak Canvas'ı veya Root'u vermek iyi olabilir 
        // ama şimdilik dünya koordinatında instantiate ediyoruz, UI üstünde görünecektir.
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        
        // Merminin UI'ın altında kalmaması için Canvas'ın bir çocuğu yapmak gerekebilir.
        // Şimdilik firePoint'in parent'ını (muhtemelen Canvas/TopPanel) parent yapalım:
        proj.transform.SetParent(firePoint.parent, true);
        proj.transform.localScale = Vector3.one; // Boyut bozulmasın diye

        // 2. Mermiyi Kur (Hasarı ve Hedefi ver)
        ProjectileController pc = proj.GetComponent<ProjectileController>();
        if (pc != null)
        {
            // true = EnemyAttack (Yani Player hasar alacak)
            pc.Initialize(damage, true, targetPoint.position);
        }
    }
}