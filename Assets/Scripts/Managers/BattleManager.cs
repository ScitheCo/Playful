using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Oyuncu Ayarları")]
    public float maxHealth = 1000f;
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    
    // Defans Sistemi (Yeşil Taş)
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;

    [Header("Mana & Skill")]
    public float maxMana = 100f;
    public float currentMana = 0f;
    
    [Header("UI Referansları")]
    public Slider battleSlider; 
    public Image manaBarFill;   
    public Button skillButton;  
    public TextMeshProUGUI timerText; 
    
    [Header("Görsel Referanslar")]
    public TextMeshProUGUI playerHealthText; 
    public TextMeshProUGUI enemyHealthText;  
    public GameObject playerShieldIcon;      
    public GameObject enemyShieldIcon;       

    // --- YENİ: SARSINTI REFERANSI ---
    [Header("Efekt Referansları")]
    public ObjectShake cameraShaker; // Inspector'dan TopPanel'i (ObjectShake olan objeyi) buraya sürükle
    // -------------------------------

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
        
        if(playerShieldIcon) playerShieldIcon.SetActive(false);
        if(enemyShieldIcon) enemyShieldIcon.SetActive(false);

        UpdateBattleBar();
        UpdateManaUI();
        skillButton.interactable = false;
    }

    void Update()
    {
        if (!isGameActive) return;

        if (roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            EndGame();
        }

        if (isPlayerShieldActive)
        {
            shieldDuration -= Time.deltaTime;
            if (shieldDuration <= 0)
            {
                DeactivateShield();
            }
        }
    }

    // === HASAR VE EFEKT SİSTEMİ ===
    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isGameActive) return;

        if (isPlayerTakingDamage)
        {
            // Kalkan kontrolü
            if (isPlayerShieldActive) damageAmount *= 0.3f; // %70 Azalt
            
            currentPlayerHealth -= damageAmount;

            // --- YENİ: EKRAN TİTREŞİMİ ---
            // Sadece biz hasar aldığımızda ekran titresin
            if (cameraShaker != null)
            {
                // 0.2 saniye boyunca 15 şiddetinde titret
                cameraShaker.Shake(0.2f, 15f);
            }
        }
        else
        {
            currentEnemyHealth -= damageAmount;
        }

        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
        currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);

        UpdateBattleBar();
        CheckWinCondition();
    }

    public void ActivatePlayerShield(float duration)
    {
        isPlayerShieldActive = true;
        shieldDuration = duration;
        
        if (playerShieldIcon != null) playerShieldIcon.SetActive(true);
    }

    void DeactivateShield()
    {
        isPlayerShieldActive = false;
        if (playerShieldIcon != null) playerShieldIcon.SetActive(false);
    }

    void UpdateBattleBar()
    {
        float totalHealth = currentPlayerHealth + currentEnemyHealth;
        
        if (totalHealth > 0)
        {
            float ratio = currentPlayerHealth / totalHealth;
            // Slider Max Value 100 olmalı
            battleSlider.value = ratio * 100f; 

            if(playerHealthText) playerHealthText.text = Mathf.FloorToInt(currentPlayerHealth).ToString();
            if(enemyHealthText) enemyHealthText.text = Mathf.FloorToInt(currentEnemyHealth).ToString();
        }
    }

    public void AddMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        UpdateManaUI();
    }

    void UpdateManaUI()
    {
        if (manaBarFill != null) manaBarFill.fillAmount = currentMana / maxMana;
        if (currentMana >= maxMana) skillButton.interactable = true;
    }

    public void UseSkill()
    {
        if (currentMana >= maxMana)
        {
            // ÖRNEK SKILL: AĞIR DARBE
            TakeDamage(false, 200f); 
            
            currentMana = 0;
            skillButton.interactable = false;
            UpdateManaUI();
        }
    }

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