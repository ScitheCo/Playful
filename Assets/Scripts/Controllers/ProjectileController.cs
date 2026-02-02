using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private float damageAmount;
    private bool isEnemyAttack; // True ise Player'a vurur, False ise Rakibe
    private Vector3 targetPosition;
    
    [Header("Ayarlar")]
    public float speed = 1500f; // Canvas üzerinde hareket edeceği için yüksek hız lazım
    public float hitThreshold = 10f; // Hedefe ne kadar yaklaşınca çarpsın?

    // Mermiyi başlatan fonksiyon
    public void Initialize(float damage, bool isEnemy, Vector3 target)
    {
        damageAmount = damage;
        isEnemyAttack = isEnemy;
        targetPosition = target;
        
        // Merminin yönünü hedefe çevir (Görsel olarak güzel durur)
        Vector3 dir = targetPosition - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Update()
    {
        // Hedefe doğru uç
        // (UI üzerinde hareket ettiğimiz için RectTransform veya transform kullanabiliriz. 
        // Basit transform.position dünya koordinatlarında UI için de çalışır.)
        
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        // Hedefe vardık mı?
        if (Vector3.Distance(transform.position, targetPosition) < hitThreshold)
        {
            OnHit();
        }
    }

    void OnHit()
    {
        // 1. Hasarı Ver
        if (BattleManager.Instance != null)
        {
            // isEnemyAttack TRUE ise -> Player hasar alır (TakeDamage'ın ilk parametresi true olur)
            // isEnemyAttack FALSE ise -> Rakip hasar alır
            BattleManager.Instance.TakeDamage(isEnemyAttack, damageAmount);
        }

        // 2. Efekt Çıkar (İleride buraya patlama efekti ekleyeceğiz)
        // Instantiate(explosionEffect, transform.position, ...);

        // 3. Kendini Yok Et
        Destroy(gameObject);
    }
}