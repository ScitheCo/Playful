using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : BasePanel
{
    [Header("Üst Bar")]
    public Image avatarImage;
    public Image profileFrameImage;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI levelText;
    public Slider xpSlider;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI gemText;
    public Button settingsButton;

    [Header("Orta Alan")]
    public Image currentCharacterDisplay;
    public Button playButton;             // SAVAŞ Butonu
    public Button gameModeButton;         // Mod Seçim Butonu (Ranked/Casual yazan)
    public TextMeshProUGUI currentModeText;
    
    [Header("Oyuncu Cüzdanı (Referans)")]
    public TextMeshProUGUI playerGoldText;
    public TextMeshProUGUI playerGemText;
    
    [Header("Panels")]
    public GameObject userProfileUI;
    
    [Header("Alt Bar & Diğerleri")]
    public Button heroesButton;      
    public Button socialButton;      // BU ARTIK "ARKADAŞLAR" BUTONU
    public Button leaderboardButton; // (YENİ) BU "SIRALAMA" BUTONU
    public Button shopButton;        
    public Button questsButton;      // (YENİ) Görevler butonu varsa ekleyebilirsin
    public Button profileButton;
    
    public override void Init()
    {
        base.Init();
    }

    private void Start()
    {
        
        // Karakterler Butonu -> Lobby kapanır, Karakter menüsü açılır
        heroesButton.onClick.RemoveAllListeners();
        heroesButton.onClick.AddListener(GoToHeroes);

        // Sosyal ve Leaderboard
        socialButton.onClick.RemoveAllListeners();
        socialButton.onClick.AddListener(() => Debug.Log("Arkadaş Listesi (Yakında)"));
        
        profileButton.onClick.RemoveAllListeners();
        profileButton.onClick.AddListener(OpenProfilePanel);
            
            

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(MainMenuManager.Instance.OpenLeaderboardOverlay);
        }
    }

    public void UpdateVisuals(PlayerData data, CharacterData selectedChar, GameMode mode)
    {
        // 1. Profil
        playerNameText.text = string.IsNullOrEmpty(data.username) ? "Player" : data.username;
        levelText.text = "Lvl " + data.level;
        xpSlider.value = (float)data.currentXP / data.requiredXP;
        avatarImage.sprite = GameManager.Instance.GetAvatarSprite(data.avatarId);
        profileFrameImage.sprite = GameManager.Instance.GetFrameSprite(data.frameId);
        
        // 2. Cüzdan
        goldText.text = data.gold.ToString();
        gemText.text = data.gems.ToString();

        // 3. Karakter
        if (selectedChar != null)
            currentCharacterDisplay.sprite = selectedChar.avatar;
        
        // 4. Cüzdanı Güncelle
        playerGoldText.text = data.gold.ToString();
        playerGemText.text = data.gems.ToString();

        // 5. Mod Yazısı (Seçilen moda göre güncellenir)
        currentModeText.text = "" + mode;
    }
    
    public void GoToLobby()
    {
        UIManager.Instance.GetPanel<HeroesUI>().Hide();
        UIManager.Instance.GetPanel<LobbyUI>().Show();
        MainMenuManager.Instance.UpdateAllPanels();
    }

    public void GoToHeroes()
    {
        UIManager.Instance.GetPanel<LobbyUI>().Hide();
        UIManager.Instance.GetPanel<HeroesUI>().Show();
        MainMenuManager.Instance.UpdateAllPanels();
    }

    public void OpenProfilePanel()
    {
        userProfileUI.SetActive(true);
    }
}