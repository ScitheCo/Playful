using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Karakter Seçimi (Inspector'dan Ata)")]
    public CharacterData playerProfile; // Senin Karakterin
    public CharacterData enemyProfile;  // Rakip Karakter (Bot)

    [Header("Canlı Veriler (Read Only)")]
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    public float currentMana = 0;
    
    // Cache (Kısayol) Verileri
    private float playerMaxHP;
    private float enemyMaxHP;
    private float playerMaxMana;

    // Defans Sistemi
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;

    [Header("Oyun Ayarları")]
    public float roundTime = 120f;
    private bool isGameActive = true;

    // İstatistikler
    [HideInInspector] public float totalDamageDealt = 0;
    [HideInInspector] public float matchDurationCounter = 0;
    [HideInInspector] public int maxCombo = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Verileri Profillerden Yükle
        InitializeCharacters();

        // 2. UI'ı Güncelle
        UpdateAllUI();
        
        // 3. İstatistikleri Sıfırla
        totalDamageDealt = 0;
        matchDurationCounter = 0;
        maxCombo = 0;
    }

    void InitializeCharacters()
    {
        // Eğer profil atanmadıysa hata vermesin, varsayılan değer kullan
        if (playerProfile != null)
        {
            playerMaxHP = playerProfile.maxHealth;
            playerMaxMana = playerProfile.maxMana;
        }
        else
        {
            playerMaxHP = 1000; playerMaxMana = 100;
            Debug.LogWarning("Player Profile eksik! Varsayılan değerler kullanılıyor.");
        }

        if (enemyProfile != null) enemyMaxHP = enemyProfile.maxHealth;
        else enemyMaxHP = 1000;

        // Canları fulle
        currentPlayerHealth = playerMaxHP;
        currentEnemyHealth = enemyMaxHP;
        currentMana = 0;
    }

    void Update()
    {
        if (!isGameActive) return;

        matchDurationCounter += Time.deltaTime;

        if (roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UIManager.Instance?.GetPanel<BattleUI>()?.UpdateTimer(roundTime);
        }
        else
        {
            EndGame();
        }

        if (isPlayerShieldActive)
        {
            shieldDuration -= Time.deltaTime;
            if (shieldDuration <= 0) DeactivateShield();
        }
    }

    // === HASAR VE İYİLEŞME ===
    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isGameActive) return;

        if (isPlayerTakingDamage)
        {
            if (isPlayerShieldActive) damageAmount *= 0.3f;
            currentPlayerHealth -= damageAmount;
            UIManager.Instance?.GetPanel<BattleUI>()?.ShakeScreen(0.2f, 15f);
        }
        else
        {
            // Pasif Kontrolü: Player'ın Kırmızı Hasar Çarpanı var mı?
            if (playerProfile != null) damageAmount *= playerProfile.redDamageMultiplier;
            
            currentEnemyHealth -= damageAmount;
            totalDamageDealt += damageAmount;
        }

        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
        currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);

        UpdateAllUI();
        CheckWinCondition();
    }

    // YENİ: İyileşme Fonksiyonu (Heal Skill için)
    public void Heal(bool isPlayer, float amount)
    {
        if (!isGameActive) return;

        if (isPlayer)
        {
            // Pasif Kontrolü: Yeşil çarpanı
            if (playerProfile != null) amount *= playerProfile.greenHealMultiplier;
            
            currentPlayerHealth += amount;
            currentPlayerHealth = Mathf.Min(currentPlayerHealth, playerMaxHP);
        }
        else
        {
            currentEnemyHealth += amount;
            currentEnemyHealth = Mathf.Min(currentEnemyHealth, enemyMaxHP);
        }
        UpdateAllUI();
    }

    // === YETENEK KULLANIMI (GÜNCELLENDİ) ===
    public void AddMana(float amount)
    {
        // Pasif Kontrolü: Mavi çarpanı
        if (playerProfile != null) amount *= playerProfile.blueManaMultiplier;

        currentMana += amount;
        currentMana = Mathf.Min(currentMana, playerMaxMana);
        UIManager.Instance?.GetPanel<BattleUI>()?.UpdateMana(currentMana, playerMaxMana);
    }

    public void UseSkill()
    {
        // Mana Yeterli mi ve Skill Var mı?
        if (currentMana >= playerMaxMana && playerProfile != null && playerProfile.activeSkill != null)
        {
            // SKILL DATA ÜZERİNDEKİ TETİĞİ ÇEK
            // Parametreler: (BattleManager Referansı, EnemyManager Referansı, Kullanan Player mı?)
            playerProfile.activeSkill.Trigger(this, EnemyManager.Instance, true);

            currentMana = 0;
            UpdateAllUI();
        }
        else
        {
            Debug.Log("Mana yetersiz veya Skill atanmamış!");
        }
    }

    // === GEREKLİ YARDIMCILAR ===
    public void ActivatePlayerShield(float duration)
    {
        isPlayerShieldActive = true;
        shieldDuration = duration;
        UIManager.Instance?.GetPanel<BattleUI>()?.SetPlayerShield(true);
    }

    void DeactivateShield()
    {
        isPlayerShieldActive = false;
        UIManager.Instance?.GetPanel<BattleUI>()?.SetPlayerShield(false);
    }

    public void UpdateComboStats(int currentChain)
    {
        if (currentChain > maxCombo) maxCombo = currentChain;
    }

    void UpdateAllUI()
    {
        var ui = UIManager.Instance?.GetPanel<BattleUI>();
        if (ui != null)
        {
            ui.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
            ui.UpdateMana(currentMana, playerMaxMana);
        }
    }

    void CheckWinCondition()
    {
        if (currentEnemyHealth <= 0 || currentPlayerHealth <= 0) EndGame();
    }

    void EndGame()
    {
        isGameActive = false;
        if (EnemyManager.Instance != null) EnemyManager.Instance.StopBattle();

        bool isVictory = currentPlayerHealth > currentEnemyHealth;
        
        if (EndGameManager.Instance != null)
        {
            EndGameManager.Instance.ProcessGameResult(isVictory, totalDamageDealt, maxCombo, matchDurationCounter, currentPlayerHealth);
        }
    }
    
    // === BOT YETENEK KULLANIMI ===
    public void EnemyUseSkill()
    {
        if (enemyProfile != null && enemyProfile.activeSkill != null)
        {
            // isPlayer = false gönderiyoruz
            enemyProfile.activeSkill.Trigger(this, EnemyManager.Instance, false);
            Debug.Log($"<color=red>Düşman Yetenek Kullandı: {enemyProfile.activeSkill.skillName}</color>");
        }
        else
        {
            Debug.LogWarning("Düşman profili veya yeteneği yok!");
        }
    }
}