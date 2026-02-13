using System;
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
        GameMode currentMode = GameManager.Instance.currentMode;

        // --- 1. ELO HESAPLAMA (Sadece Ranked) ---
        int eloChange = 0;
        if (currentMode == GameMode.Ranked)
        {
            // Basit Elo Mantığı (İleride rakibin elosuna göre zorlaştırabiliriz)
            eloChange = isVictory ? 25 : -15;
            
            // Elo 0'ın altına düşmesin
            if (data.elo + eloChange < 0) eloChange = -data.elo;
            
            data.elo += eloChange;
            Debug.Log($"Ranked Bitti. Elo Değişimi: {eloChange}");
        }

        // --- 2. ÖDÜL SİSTEMİ (Altın & XP) ---
        int goldEarned = 0;
        int xpEarned = 0;

        if (isVictory)
        {
            if (currentMode == GameMode.Ranked)      { goldEarned = 150; xpEarned = 100; }
            else if (currentMode == GameMode.Casual) { goldEarned = 50;  xpEarned = 50; }
            else if (currentMode == GameMode.Practice){ goldEarned = 10;  xpEarned = 10; }
        }
        else // Kaybetme Tesellisi
        {
            goldEarned = 20;
            xpEarned = 15;
        }

        data.gold += goldEarned;
        data.currentXP += xpEarned;

        // --- 3. LEVEL UP SİSTEMİ ---
        bool hasLeveledUp = false;
        if (data.currentXP >= data.requiredXP)
        {
            data.currentXP -= data.requiredXP;
            data.level++;
            data.requiredXP *= 1.2f; // Her levelde zorlaşsın
            hasLeveledUp = true;
            
            // Level Up Ödülü (Örn: 10 Elmas)
            data.gems += 10;
            Debug.Log("LEVEL UP! +10 Elmas kazandın.");
        }

        // --- 4. MAÇ GEÇMİŞİNE KAYDET (MATCH HISTORY) ---
        MatchRecord newRecord = new MatchRecord();
        newRecord.opponentName = GameManager.Instance.currentEnemyName; // Rakip ismi
        newRecord.result = isVictory ? "Victory" : "Defeat";
        newRecord.eloChange = eloChange;
        newRecord.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); // Tarih formatı

        // Listeye ekle (En başa ekle ki en son maç en üstte olsun)
        data.matchHistory.Insert(0, newRecord);
        
        // Listeyi sınırla (Son 20 maçı tutsun, veritabanı şişmesin)
        if (data.matchHistory.Count > 20)
            data.matchHistory.RemoveAt(data.matchHistory.Count - 1);

        // --- 5. KAYIT VE SENKRONİZASYON ---
        GameManager.Instance.SaveGame(); // PlayFab'a JSON olarak gider

        // Sadece Ranked ise Leaderboard güncelle
        if (currentMode == GameMode.Ranked)
        {
            PlayFabManager.Instance.SendLeaderboardStats(data.elo, data.level);
        }

        // --- 6. UI GÜNCELLEME ---
        var currentRank = rankData.GetRankByElo(data.elo);
        var nextRank = rankData.GetNextRank(data.elo);

        float range = nextRank.minElo - currentRank.minElo;
        float progress = (range > 0) ? (float)(data.elo - currentRank.minElo) / range : 1;

        ui.SetVictoryStatus(isVictory);
        ui.SetEloVisuals(data.elo, eloChange, currentRank.rankIcon, nextRank.rankIcon, progress);
        
        // Eğer Level atladıysa UI'da bunu belirtmek için XP barını full gösterip sonra sıfırlayabilirsin
        // Şimdilik standart gösterim:
        ui.SetLevelVisuals(data.level, data.currentXP / data.requiredXP);
        
        ui.SetStats(damage, combo, time, remainingHP);

        // Özel Mesaj (Level atladıysa)
        if (hasLeveledUp)
        {
            // Buraya ileride LevelUp efektini tetikleyen kod gelecek
            // ui.ShowLevelUpEffect();
            Debug.Log("UI: Level Up Animasyonu Oynamalı");
        }

        // Butonlar
        ui.rematchButton.onClick.RemoveAllListeners();
        ui.rematchButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        ui.mainMenuButton.onClick.RemoveAllListeners();
        ui.mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene("Main Menu"));
    }
}