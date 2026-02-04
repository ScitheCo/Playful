using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private float damageAmount;
    private bool isEnemyAttack; // True = Player'a vuruyor, False = Rakibe
    private Vector3 targetPosition;
    
    [Header("Ayarlar")]
    public float speed = 1500f; 
    public float hitThreshold = 10f; 

    // --- YENİ: VFX REFERANSI ---
    [Header("Görsel Efektler")]
    public GameObject hitEffectPrefab; // Çarpma anında çıkacak efekt
    // ---------------------------

    public void Initialize(float damage, bool isEnemy, Vector3 target)
    {
        damageAmount = damage;
        isEnemyAttack = isEnemy;
        targetPosition = target;
        
        // Yönü hedefe çevir
        Vector3 dir = targetPosition - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (Vector3.Distance(transform.position, targetPosition) < hitThreshold)
        {
            OnHit();
        }
    }

    void OnHit()
    {
        // 1. Hasarı İşle
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.TakeDamage(isEnemyAttack, damageAmount);
        }

        // 2. Çarpma Efekti Yarat
        if (hitEffectPrefab != null)
        {
            GameObject vfx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            
            // Efekti UI içinde tut
            vfx.transform.SetParent(transform.parent); 
            vfx.transform.localScale = Vector3.one;

            Destroy(vfx, 2f); 
        }

        // 3. Mermiyi Yok Et
        Destroy(gameObject);
    }
}