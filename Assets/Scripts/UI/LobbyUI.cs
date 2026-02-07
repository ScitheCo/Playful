using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : BasePanel
{
    [Header("Üst Bar")]
    public Image avatarImage;
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

    [Header("Alt Bar & Diğerleri")]
    public Button heroesButton;      
    public Button socialButton;      // BU ARTIK "ARKADAŞLAR" BUTONU
    public Button leaderboardButton; // (YENİ) BU "SIRALAMA" BUTONU
    public Button shopButton;        
    public Button questsButton;      // (YENİ) Görevler butonu varsa ekleyebilirsin
    
    public override void Init()
    {
        base.Init();
    }

    public void UpdateVisuals(PlayerData data, CharacterData selectedChar, GameMode mode)
    {
        // 1. Profil
        playerNameText.text = string.IsNullOrEmpty(data.username) ? "Player" : data.username;
        levelText.text = "Lvl " + data.level;
        xpSlider.value = (float)data.currentXP / data.requiredXP;
        
        // 2. Cüzdan
        goldText.text = data.gold.ToString();
        gemText.text = data.gems.ToString();

        // 3. Karakter
        if (selectedChar != null)
            currentCharacterDisplay.sprite = selectedChar.avatar;

        // 4. Mod Yazısı (Seçilen moda göre güncellenir)
        currentModeText.text = mode.ToString().ToUpper();
    }
}