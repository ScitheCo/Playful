using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Oyuncu Verileri")]
    public float maxHealth = 1000f;
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    
    // Defans Sistemi
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;

    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana = 0f;

    [Header("Oyun Ayarları")]
    public float roundTime = 120f;
    private bool isGameActive = true;

    // --- İSTATİSTİK VERİLERİ ---
    [HideInInspector] public float totalDamageDealt = 0;
    [HideInInspector] public float matchDurationCounter = 0;
    [HideInInspector] public int maxCombo = 0; // Kombo rekoru burada tutulur

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentPlayerHealth = maxHealth;
        currentEnemyHealth = maxHealth;
        currentMana = 0;
        
        // İstatistikleri Sıfırla
        totalDamageDealt = 0;
        matchDurationCounter = 0;
        maxCombo = 0;

        UpdateAllUI();
    }

    void Update()
    {
        if (!isGameActive) return;

        matchDurationCounter += Time.deltaTime;

        // Zamanlayıcı
        if (roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            var ui = UIManager.Instance?.GetPanel<BattleUI>();
            if (ui) ui.UpdateTimer(roundTime);
        }
        else
        {
            EndGame();
        }

        // Kalkan Süresi
        if (isPlayerShieldActive)
        {
            shieldDuration -= Time.deltaTime;
            if (shieldDuration <= 0) DeactivateShield();
        }
    }

    // === HASAR MANTIĞI ===
    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isGameActive) return;

        if (isPlayerTakingDamage)
        {
            if (isPlayerShieldActive) damageAmount *= 0.3f; // Kalkan varsa hasar azalır
            currentPlayerHealth -= damageAmount;
            
            // Ekranı Titret
            var ui = UIManager.Instance?.GetPanel<BattleUI>();
            if (ui) ui.ShakeScreen(0.2f, 15f);
        }
        else
        {
            currentEnemyHealth -= damageAmount;
            totalDamageDealt += damageAmount; // Hasar istatistiği
        }

        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
        currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);

        UpdateAllUI();
        CheckWinCondition();
    }

    // === KOMBO GÜNCELLEME (BoardManager Çağırır) ===
    public void UpdateComboStats(int currentChain)
    {
        if (currentChain > maxCombo)
        {
            maxCombo = currentChain;
            Debug.Log($"Yeni Kombo Rekoru: x{maxCombo}");
        }
    }

    // === KALKAN VE MANA ===
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

    public void AddMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        UIManager.Instance?.GetPanel<BattleUI>()?.UpdateMana(currentMana, maxMana);
    }

    public void UseSkill()
    {
        if (currentMana >= maxMana)
        {
            TakeDamage(false, 200f);
            currentMana = 0;
            UpdateAllUI();
        }
    }

    // === YARDIMCILAR ===
    void UpdateAllUI()
    {
        var ui = UIManager.Instance?.GetPanel<BattleUI>();
        if (ui != null)
        {
            ui.UpdateBattleBars(currentPlayerHealth, maxHealth, currentEnemyHealth, maxHealth);
            ui.UpdateMana(currentMana, maxMana);
        }
    }

    void CheckWinCondition()
    {
        if (currentEnemyHealth <= 0 || currentPlayerHealth <= 0)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        isGameActive = false;
        if (EnemyManager.Instance != null) EnemyManager.Instance.StopBattle();

        bool isVictory = currentPlayerHealth > currentEnemyHealth;
        
        // EndGameManager'a tüm verileri (Combo dahil) gönder
        if (EndGameManager.Instance != null)
        {
            EndGameManager.Instance.ProcessGameResult(isVictory, totalDamageDealt, maxCombo, matchDurationCounter, currentPlayerHealth);
        }
        else
        {
            Debug.LogError("EndGameManager bulunamadı!");
        }
    }
}