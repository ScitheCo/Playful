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

    // YENİ: "int combo" parametresi eklendi
    public void ProcessGameResult(bool isVictory, float damage, int combo, float time, float remainingHP)
    {
        EndGameUI ui = UIManager.Instance.GetPanel<EndGameUI>();
        if (ui == null) return;
        
        UIManager.Instance.ShowPanel<EndGameUI>();

        int currentElo = 1200; 
        int eloChange = isVictory ? 25 : -15;
        int newElo = currentElo + eloChange;
        
        var currentRank = rankData.GetRankByElo(newElo);
        var nextRank = rankData.GetNextRank(newElo);

        float range = nextRank.minElo - currentRank.minElo;
        float progress = 0;
        if (range > 0) progress = (float)(newElo - currentRank.minElo) / range;
        else progress = 1; 

        ui.SetVictoryStatus(isVictory);
        ui.SetEloVisuals(newElo, eloChange, currentRank.rankIcon, nextRank.rankIcon, progress);

        int currentLevel = 2;
        float xpFill = 0.4f + (isVictory ? 0.2f : 0.05f); 
        ui.SetLevelVisuals(currentLevel, xpFill);

        // YENİ: combo değişkeni buraya gönderiliyor
        ui.SetStats(damage, combo, time, remainingHP);

        ui.rematchButton.onClick.RemoveAllListeners();
        ui.rematchButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        ui.mainMenuButton.onClick.RemoveAllListeners();
        ui.mainMenuButton.onClick.AddListener(() => Debug.Log("Ana Menüye Dönülüyor..."));
    }
}