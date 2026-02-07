using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance;

    [Header("Veri Ayarları")]
    public RankData rankData; 
    public int baseXPGain = 100;

    private void Awake()
    {
        Instance = this;
    }

    public void ProcessGameResult(bool isVictory, float damage, int combo, float time, float remainingHP)
    {
        EndGameUI ui = UIManager.Instance.GetPanel<EndGameUI>();
        if (ui == null) return;
        
        UIManager.Instance.ShowPanel<EndGameUI>();

        PlayerData data = GameManager.Instance.playerData;
        GameMode currentMode = GameManager.Instance.currentMode; // Modu al

        // --- 1. ELO HESAPLAMA (Sadece Ranked) ---
        int eloChange = 0;
        
        if (currentMode == GameMode.Ranked)
        {
            eloChange = isVictory ? 25 : -15;
            data.elo += eloChange;
            Debug.Log("Ranked Maç Bitti. Elo Güncellendi.");
        }
        else
        {
            Debug.Log($"{currentMode} Modu: Elo değişmedi.");
        }

        // --- 2. ALTIN VE XP (Her modda verilebilir ama oranları farklı olabilir) ---
        int goldEarned = 0;
        int xpEarned = 0;

        if (isVictory)
        {
            if (currentMode == GameMode.Ranked) { goldEarned = 100; xpEarned = 100; }
            else if (currentMode == GameMode.Casual) { goldEarned = 50; xpEarned = 50; }
            else if (currentMode == GameMode.Practice) { goldEarned = 10; xpEarned = 10; } // Bot farmını önlemek için az ver
        }
        else
        {
            goldEarned = 10; // Teselli ödülü
        }

        data.gold += goldEarned;
        data.currentXP += xpEarned;

        // Level Atlama Kontrolü (Aynen kalsın)
        if (data.currentXP >= data.requiredXP)
        {
            data.currentXP -= data.requiredXP;
            data.level++;
            data.requiredXP *= 1.2f;
        }

        // --- KAYDET ---
        GameManager.Instance.SaveGame();
        
        // --- İSTATİSTİK GÖNDERME (YENİ) ---
        // Sadece Ranked modunda Elo gönderilir
        if (GameManager.Instance.currentMode == GameMode.Ranked)
        {
            PlayFabManager.Instance.SendLeaderboardStats(data.elo, data.level);
        }
        // Level her modda gönderilebilir (İstersen) practice dışında casualda da level gitsin.
        else 
        {
            // PlayFabManager.Instance.SendLeaderboardStats(data.elo, data.level); // Opsiyonel
        }

        // --- UI GÜNCELLEME ---
        var currentRank = rankData.GetRankByElo(data.elo);
        var nextRank = rankData.GetNextRank(data.elo);

        // Görsel hesaplamalar...
        float range = nextRank.minElo - currentRank.minElo;
        float progress = (range > 0) ? (float)(data.elo - currentRank.minElo) / range : 1;

        ui.SetVictoryStatus(isVictory);
        ui.SetEloVisuals(data.elo, eloChange, currentRank.rankIcon, nextRank.rankIcon, progress);
        ui.SetLevelVisuals(data.level, data.currentXP / data.requiredXP);
        ui.SetStats(damage, combo, time, remainingHP);

        // Butonlar (Rematch mantığı modlara göre değişebilir ama şimdilik kalsın)
        ui.rematchButton.onClick.RemoveAllListeners();
        ui.rematchButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        ui.mainMenuButton.onClick.RemoveAllListeners();
        ui.mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene("Main Menu"));
    }
}