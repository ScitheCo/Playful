using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndGameUI : BasePanel
{
    [Header("Sonuç Göstergeleri")]
    public GameObject victoryIcon;
    public GameObject defeatIcon;
    public GameObject backgroundFrame; // Renk değişimi için (Opsiyonel)

    [Header("Elo Info Panel")]
    public GameObject eloInfoPanel;
    public TextMeshProUGUI currentEloText;
    public Slider rankSlider;
    public Image currentRankIcon;
    public Image nextRankIcon;
    
    [Header("Level Info")]
    public TextMeshProUGUI levelText;
    public Image levelRadialFill; // Level XP barı

    [Header("Statistics Panel")]
    public GameObject statisticsPanel;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI hpText;

    [Header("Butonlar")]
    public Button statisticButton; // Stats açar
    public Button eloStatsButton;  // Elo açar
    public Button rematchButton;
    public Button mainMenuButton;

    public override void Init()
    {
        // Buton Dinleyicileri
        statisticButton.onClick.AddListener(ShowStatistics);
        eloStatsButton.onClick.AddListener(ShowEloInfo);
        
        // Başlangıçta Elo Panel açık, Stats kapalı
        ShowEloInfo();
    }

    public void ShowEloInfo()
    {
        eloInfoPanel.SetActive(true);
        statisticsPanel.SetActive(false);
        
        eloStatsButton.gameObject.SetActive(false); // Kendi butonunu gizle
        statisticButton.gameObject.SetActive(true); // Diğer butonu göster
    }

    public void ShowStatistics()
    {
        eloInfoPanel.SetActive(false);
        statisticsPanel.SetActive(true);

        statisticButton.gameObject.SetActive(false);
        eloStatsButton.gameObject.SetActive(true);
    }

    // Dataları Ekrana Basma Fonksiyonları
    public void SetVictoryStatus(bool isVictory)
    {
        if (victoryIcon) victoryIcon.SetActive(isVictory);
        if (defeatIcon) defeatIcon.SetActive(!isVictory);
    }

    public void SetEloVisuals(int elo, int change, Sprite rankImg, Sprite nextRankImg, float sliderVal)
    {
        // "+25" veya "-15" formatında yaz
        string sign = change >= 0 ? "+" : "";
        currentEloText.text = $"ELO: {sign}{change}"; 
        currentEloText.color = change >= 0 ? Color.green : Color.red;

        currentRankIcon.sprite = rankImg;
        nextRankIcon.sprite = nextRankImg;
        rankSlider.value = sliderVal;
    }

    public void SetLevelVisuals(int level, float xpProgress)
    {
        levelText.text = level.ToString();
        levelRadialFill.fillAmount = xpProgress;
    }

    public void SetStats(float damage, int combo, float time, float hp)
    {
        damageText.text = Mathf.FloorToInt(damage).ToString();
        comboText.text = combo.ToString() + "x";
        
        int min = Mathf.FloorToInt(time / 60);
        int sec = Mathf.FloorToInt(time % 60);
        timeText.text = $"{min:00}:{sec:00}";
        
        hpText.text = Mathf.FloorToInt(hp).ToString();
    }
}