using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Veri")]
    public List<CharacterData> availableCharacters; // Tüm karakterler listesi
    private int currentIndex = 0;

    void Start()
    {
        // ------------------------------------------------------------
        // 1. LOBBY PANEL BAĞLANTILARI
        // ------------------------------------------------------------
        LobbyUI lobby = UIManager.Instance.GetPanel<LobbyUI>();
        if (lobby != null)
        {
            // SAVAŞ Butonu -> Artık direkt oyuna sokar (Seçili mod ile)
            lobby.playButton.onClick.RemoveAllListeners();
            lobby.playButton.onClick.AddListener(StartGameLoop);

            // Mod Seçim Butonu -> Mod panelini açar (Oyuna sokmaz)
            lobby.gameModeButton.onClick.RemoveAllListeners();
            lobby.gameModeButton.onClick.AddListener(OpenModeSelection);

            // Karakterler Butonu -> Lobby kapanır, Karakter menüsü açılır
            lobby.heroesButton.onClick.RemoveAllListeners();
            lobby.heroesButton.onClick.AddListener(GoToHeroes);

            // Arkadaş Listesi (Social) -> Henüz yapım aşamasında
            lobby.socialButton.onClick.RemoveAllListeners();
            lobby.socialButton.onClick.AddListener(() => Debug.Log("Arkadaş Listesi (Yakında)"));

            // Sıralama (Leaderboard) -> Lobby kapanmaz, üzerine açılır (Overlay)
            if (lobby.leaderboardButton != null)
            {
                lobby.leaderboardButton.onClick.RemoveAllListeners();
                lobby.leaderboardButton.onClick.AddListener(OpenLeaderboardOverlay);
            }

            // Ayarlar -> Ayarlar Paneli açılır
            lobby.settingsButton.onClick.RemoveAllListeners();
            lobby.settingsButton.onClick.AddListener(() => SettingsManager.Instance.OpenSettings());
        }

        // ------------------------------------------------------------
        // 2. HEROES (KARAKTER) PANEL BAĞLANTILARI
        // ------------------------------------------------------------
        MainMenuUI heroesUI = UIManager.Instance.GetPanel<MainMenuUI>();
        if (heroesUI != null)
        {
            heroesUI.nextButton.onClick.RemoveAllListeners();
            heroesUI.nextButton.onClick.AddListener(NextCharacter);

            heroesUI.prevButton.onClick.RemoveAllListeners();
            heroesUI.prevButton.onClick.AddListener(PrevCharacter);

            // Geri Butonu -> Karakter menüsü kapanır, Lobby açılır
            heroesUI.backButton.onClick.RemoveAllListeners();
            heroesUI.backButton.onClick.AddListener(GoToLobby);

            heroesUI.equipButton.onClick.RemoveAllListeners();
            heroesUI.equipButton.onClick.AddListener(EquipCharacter);

            heroesUI.buyGoldButton.onClick.RemoveAllListeners();
            heroesUI.buyGoldButton.onClick.AddListener(BuyWithGold);

            heroesUI.buyGemButton.onClick.RemoveAllListeners();
            heroesUI.buyGemButton.onClick.AddListener(BuyWithGems);
        }

        // ------------------------------------------------------------
        // 3. MOD SEÇİM PANELİ BAĞLANTILARI
        // ------------------------------------------------------------
        GameModeUI modeUI = UIManager.Instance.GetPanel<GameModeUI>();
        if (modeUI != null)
        {
            // Mod butonları sadece seçim yapar, oyunu başlatmaz
            modeUI.rankedButton.onClick.RemoveAllListeners();
            modeUI.rankedButton.onClick.AddListener(() => SelectMode(GameMode.Ranked));

            modeUI.casualButton.onClick.RemoveAllListeners();
            modeUI.casualButton.onClick.AddListener(() => SelectMode(GameMode.Casual));

            modeUI.practiceButton.onClick.RemoveAllListeners();
            modeUI.practiceButton.onClick.AddListener(() => SelectMode(GameMode.Practice));

            modeUI.closeButton.onClick.RemoveAllListeners();
            modeUI.closeButton.onClick.AddListener(() => modeUI.Hide());
        }

        // ------------------------------------------------------------
        // 4. BAŞLANGIÇ DURUMU
        // ------------------------------------------------------------
        GoToLobby();
    }

    // ========================================================================
    // PANEL GEÇİŞLERİ (NAVİGASYON)
    // ========================================================================

    public void GoToLobby()
    {
        UIManager.Instance.GetPanel<MainMenuUI>().Hide(); // Heroes kapanır
        UIManager.Instance.GetPanel<LobbyUI>().Show();    // Lobby açılır
        UpdateAllPanels();
    }

    public void GoToHeroes()
    {
        UIManager.Instance.GetPanel<LobbyUI>().Hide();    // Lobby kapanır
        UIManager.Instance.GetPanel<MainMenuUI>().Show(); // Heroes açılır
        UpdateAllPanels();
    }

    public void OpenModeSelection()
    {
        // Mod seçimi Overlay olarak açılır
        UIManager.Instance.GetPanel<GameModeUI>().Show();
    }

    public void OpenLeaderboardOverlay()
    {
        // Lobby GİZLENMEZ, üzerine açılır
        UIManager.Instance.GetPanel<LeaderboardUI>().Show();
    }

    // ========================================================================
    // OYUN AKIŞI (GAME FLOW)
    // ========================================================================

    // Mod Seçim Panelinden çağrılır
    void SelectMode(GameMode mode)
    {
        // 1. Modu Kaydet
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentMode = mode;
        }
        
        Debug.Log($"Mod Seçildi: {mode}");

        // 2. Paneli Kapat
        UIManager.Instance.GetPanel<GameModeUI>().Hide();

        // 3. Lobby'deki metni güncellemek için refresh at
        UpdateAllPanels();
    }

    // Lobby'deki "SAVAŞ" butonundan çağrılır
    void StartGameLoop()
    {
        if (GameManager.Instance != null)
        {
            GameMode currentMode = GameManager.Instance.currentMode;
            Debug.Log($"SAVAŞ BAŞLATILIYOR... Mod: {currentMode}");
            
            // FishNet entegrasyonu gelene kadar sahne yükleme:
            SceneManager.LoadScene("BattleScene");
        }
    }

    // ========================================================================
    // KARAKTER YÖNETİMİ
    // ========================================================================

    void NextCharacter()
    {
        currentIndex++;
        if (currentIndex >= availableCharacters.Count) currentIndex = 0;
        UpdateAllPanels();
    }

    void PrevCharacter()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = availableCharacters.Count - 1;
        UpdateAllPanels();
    }

    void EquipCharacter()
    {
        CharacterData selectedChar = availableCharacters[currentIndex];
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCharacter(selectedChar);
            GameManager.Instance.SaveGame();
        }

        UpdateAllPanels();
    }

    void BuyWithGold() { PerformPurchase(true); }
    void BuyWithGems() { PerformPurchase(false); }

    void PerformPurchase(bool useGold)
    {
        CharacterData charData = availableCharacters[currentIndex];
        PlayerData pData = GameManager.Instance.playerData;
        int price = useGold ? charData.goldPrice : charData.gemPrice;

        if (useGold && pData.gold >= price)
        {
            pData.gold -= price;
            UnlockCharacter(charData.characterName);
        }
        else if (!useGold && pData.gems >= price)
        {
            pData.gems -= price;
            UnlockCharacter(charData.characterName);
        }
        else
        {
            Debug.Log("Yetersiz Bakiye!");
        }
    }

    void UnlockCharacter(string charName)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerData.unlockedCharacters.Add(charName);
            GameManager.Instance.SaveGame();
            
            // Satın alınca otomatik seç
            GameManager.Instance.SetCharacter(availableCharacters[currentIndex]);
        }
        
        UpdateAllPanels();
        Debug.Log("Satın Alma Başarılı: " + charName);
    }

    // ========================================================================
    // UI GÜNCELLEME MERKEZİ
    // ========================================================================

    void UpdateAllPanels()
    {
        if (GameManager.Instance == null) return;

        PlayerData pData = GameManager.Instance.playerData;
        CharacterData currentViewChar = availableCharacters[currentIndex];
        CharacterData equippedChar = GameManager.Instance.selectedCharacter;

        // 1. HEROES PANELİNİ GÜNCELLE
        MainMenuUI heroesUI = UIManager.Instance.GetPanel<MainMenuUI>();
        if (heroesUI != null && heroesUI.gameObject.activeSelf)
        {
            bool isUnlocked = pData.unlockedCharacters.Contains(currentViewChar.characterName) || currentViewChar.isDefaultUnlocked;
            bool isEquipped = (equippedChar != null && equippedChar.characterName == currentViewChar.characterName);

            heroesUI.UpdateVisuals(currentViewChar, pData, isUnlocked, isEquipped);
        }

        // 2. LOBBY PANELİNİ GÜNCELLE
        LobbyUI lobby = UIManager.Instance.GetPanel<LobbyUI>();
        // Lobby her zaman güncel kalsın (Overlay altındayken bile)
        if (lobby != null && lobby.gameObject.activeSelf)
        {
            lobby.UpdateVisuals(pData, equippedChar, GameManager.Instance.currentMode);
        }
    }
}