using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kütüphanesi

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Oyuncu Ayarları")]
    public float maxHealth = 1000f;
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    
    // DEFANS SİSTEMİ (YEŞİL TAŞ)
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;

    [Header("Mana & Skill")]
    public float maxMana = 100f;
    public float currentMana = 0f;
    
    [Header("UI Referansları")]
    public Slider battleSlider; // O meşhur Mavi/Kırmızı bar
    public Image manaBarFill;   // Skill butonu etrafındaki dolum barı
    public Button skillButton;  // Tıklanacak buton
    public TextMeshProUGUI timerText; // Süre yazısı
    
    [Header("Yeni UI Referansları (Görsele Göre)")]
    public TextMeshProUGUI playerHealthText; // Senin altındaki "50" yazısı
    public TextMeshProUGUI enemyHealthText;  // Rakibin altındaki "50" yazısı
    public GameObject playerShieldIcon;      // Senin yanındaki kalkan resmi
    public GameObject enemyShieldIcon;       // Rakibin yanındaki kalkan resmi (Opsiyonel)

    [Header("Oyun Ayarları")]
    public float roundTime = 120f; 
    private bool isGameActive = true;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentPlayerHealth = maxHealth;
        currentEnemyHealth = maxHealth;
        currentMana = 0;
        
        // Başlangıçta kalkan kapalı
        if(playerShieldIcon) playerShieldIcon.SetActive(false);
        if(enemyShieldIcon) enemyShieldIcon.SetActive(false);

        UpdateBattleBar();
        UpdateManaUI();
        skillButton.interactable = false;
    }

    void Update()
    {
        if (!isGameActive) return;

        // Süre Sayacı
        if (roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            EndGame();
        }

        // Kalkan Süresi Sayacı
        if (isPlayerShieldActive)
        {
            shieldDuration -= Time.deltaTime;
            if (shieldDuration <= 0)
            {
                DeactivateShield();
            }
        }
    }

    // === HASAR VE BAR SİSTEMİ ===
    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isGameActive) return;

        if (isPlayerTakingDamage)
        {
            // Eğer kalkanımız varsa hasarı %70 azalt!
            if (isPlayerShieldActive) damageAmount *= 0.3f;
            
            currentPlayerHealth -= damageAmount;
        }
        else
        {
            // Rakip hasar yiyor
            currentEnemyHealth -= damageAmount;
        }

        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
        currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);

        UpdateBattleBar();
        CheckWinCondition();
    }

    // === YEŞİL TAŞ: KALKAN AÇMA ===
    public void ActivatePlayerShield(float duration)
    {
        isPlayerShieldActive = true;
        shieldDuration = duration;
        
        if (playerShieldIcon != null) 
            playerShieldIcon.SetActive(true); // İkonu görünür yap
            
        Debug.Log("KALKAN AKTİF! Hasar azalacak.");
    }

    void DeactivateShield()
    {
        isPlayerShieldActive = false;
        if (playerShieldIcon != null) 
            playerShieldIcon.SetActive(false); // İkonu gizle
    }

    void UpdateBattleBar()
    {
        float totalHealth = currentPlayerHealth + currentEnemyHealth;
        
        if (totalHealth > 0)
        {
            // Barın doluluk oranı
            float ratio = currentPlayerHealth / totalHealth;
            battleSlider.value = ratio * 100f; 

            // Altındaki Sayıları Güncelle (Örn: 500 / 1000 yerine %50 gibi görünebilir veya direkt can)
            // Senin resimdeki gibi basit sayı gösterimi:
            if(playerHealthText) playerHealthText.text = Mathf.FloorToInt(currentPlayerHealth).ToString();
            if(enemyHealthText) enemyHealthText.text = Mathf.FloorToInt(currentEnemyHealth).ToString();
        }
    }

    // === MANA SİSTEMİ ===
    public void AddMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        UpdateManaUI();
    }

    void UpdateManaUI()
    {
        if (manaBarFill != null)
            manaBarFill.fillAmount = currentMana / maxMana;

        if (currentMana >= maxMana)
            skillButton.interactable = true;
    }

    public void UseSkill()
    {
        if (currentMana >= maxMana)
        {
            Debug.Log("Yetenek Kullanıldı!");
            // Örnek Yetenek: Tahtayı Karıştır
            // BoardManager.Instance.ShuffleBoard(); <-- Bunu sonra açacağız
            
            currentMana = 0;
            skillButton.interactable = false;
            UpdateManaUI();
        }
    }

    // === YARDIMCILAR ===
    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(roundTime / 60F);
        int seconds = Mathf.FloorToInt(roundTime % 60F);
        if(timerText) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void CheckWinCondition()
    {
        if (currentEnemyHealth <= 0)
        {
            Debug.Log("KAZANDIN! (Nakavt)");
            isGameActive = false;
        }
        else if (currentPlayerHealth <= 0)
        {
            Debug.Log("KAYBETTİN! (Nakavt)");
            isGameActive = false;
        }
    }

    void EndGame()
    {
        isGameActive = false;
        if (currentPlayerHealth > currentEnemyHealth) Debug.Log("Süre Bitti: Kazandın!");
        else Debug.Log("Süre Bitti: Kaybettin!");
    }
}