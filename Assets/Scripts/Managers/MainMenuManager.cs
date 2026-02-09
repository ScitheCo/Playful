using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Veri")]
    public List<CharacterData> availableCharacters; 
    private int currentIndex = 0;

    void Start()
    {
        // ------------------------------------------------------------
        // 1. LOBBY PANEL BAĞLANTILARI
        // ------------------------------------------------------------
        LobbyUI lobby = UIManager.Instance.GetPanel<LobbyUI>();
        if (lobby != null)
        {
            // SAVAŞ Butonu -> StartGameLoop fonksiyonuna gider
            lobby.playButton.onClick.RemoveAllListeners();
            lobby.playButton.onClick.AddListener(StartGameLoop);

            // Mod Seçim Butonu -> Mod panelini açar
            lobby.gameModeButton.onClick.RemoveAllListeners();
            lobby.gameModeButton.onClick.AddListener(OpenModeSelection);

            // Karakterler Butonu -> Lobby kapanır, Karakter menüsü açılır
            lobby.heroesButton.onClick.RemoveAllListeners();
            lobby.heroesButton.onClick.AddListener(GoToHeroes);

            // Sosyal ve Leaderboard
            lobby.socialButton.onClick.RemoveAllListeners();
            lobby.socialButton.onClick.AddListener(() => Debug.Log("Arkadaş Listesi (Yakında)"));

            if (lobby.leaderboardButton != null)
            {
                lobby.leaderboardButton.onClick.RemoveAllListeners();
                lobby.leaderboardButton.onClick.AddListener(OpenLeaderboardOverlay);
            }

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
            modeUI.rankedButton.onClick.RemoveAllListeners();
            modeUI.rankedButton.onClick.AddListener(() => SelectMode(GameMode.Ranked));

            modeUI.casualButton.onClick.RemoveAllListeners();
            modeUI.casualButton.onClick.AddListener(() => SelectMode(GameMode.Casual));

            modeUI.practiceButton.onClick.RemoveAllListeners();
            modeUI.practiceButton.onClick.AddListener(() => SelectMode(GameMode.Practice));

            modeUI.closeButton.onClick.RemoveAllListeners();
            modeUI.closeButton.onClick.AddListener(() => modeUI.Hide());
        }

        // BAŞLANGIÇ: Her şey hazırsa Lobby'e git
        GoToLobby();
    }

    // ========================================================================
    // OYUN AKIŞI (GAME FLOW) - KRİTİK DÜZELTME BURADA
    // ========================================================================

    void StartGameLoop()
    {
        // 1. GameManager Kontrolü
        if (GameManager.Instance == null) return;

        // 2. KARAKTER SEÇİLİ Mİ KONTROLÜ (YENİ)
        if (GameManager.Instance.selectedCharacter == null)
        {
            Debug.LogWarning("⚠️ Bir karakter seçmelisin!");
            
            // Kullanıcıya görsel bir uyarı ver (Popup veya Karakter ekranına yönlendir)
            GoToHeroes(); // Otomatik olarak karakter seçme ekranına atıyoruz
            return;
        }

        // 3. Her şey tamamsa Eşleşme Sahnesine git
        GameMode currentMode = GameManager.Instance.currentMode;
        Debug.Log($"Eşleşme Başlatılıyor... Mod: {currentMode}");
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MatchFindingScene");
    }

    void SelectMode(GameMode mode)
    {
        if (GameManager.Instance != null)
        {
            // Modu hem değişkene hem de PlayerPrefs'e kaydeder (GameManager içindeki metod sayesinde)
            GameManager.Instance.SetGameMode(mode);
        }
        
        Debug.Log($"Mod Seçildi: {mode}");
        UIManager.Instance.GetPanel<GameModeUI>().Hide();
        UpdateAllPanels();
    }

    // ... (GoToLobby, GoToHeroes, OpenModeSelection, OpenLeaderboardOverlay AYNI) ...
    
    public void GoToLobby()
    {
        UIManager.Instance.GetPanel<MainMenuUI>().Hide();
        UIManager.Instance.GetPanel<LobbyUI>().Show();
        UpdateAllPanels();
    }

    public void GoToHeroes()
    {
        UIManager.Instance.GetPanel<LobbyUI>().Hide();
        UIManager.Instance.GetPanel<MainMenuUI>().Show();
        UpdateAllPanels();
    }

    public void OpenModeSelection() => UIManager.Instance.GetPanel<GameModeUI>().Show();
    public void OpenLeaderboardOverlay() => UIManager.Instance.GetPanel<LeaderboardUI>().Show();

    // ... (Karakter Yönetimi (Next, Prev, Equip, Buy) AYNI) ...
    
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
            GameManager.Instance.SetCharacter(selectedChar); // Hem seçer hem PlayFab'a kaydeder
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
            // Satın alınca otomatik seç ve kaydet
            GameManager.Instance.SetCharacter(availableCharacters[currentIndex]);
        }
        UpdateAllPanels();
    }

    // UI GÜNCELLEME
    void UpdateAllPanels()
    {
        if (GameManager.Instance == null) return;

        PlayerData pData = GameManager.Instance.playerData;
        CharacterData currentViewChar = availableCharacters[currentIndex];
        CharacterData equippedChar = GameManager.Instance.selectedCharacter;

        // Heroes
        MainMenuUI heroesUI = UIManager.Instance.GetPanel<MainMenuUI>();
        if (heroesUI != null && heroesUI.gameObject.activeSelf)
        {
            bool isUnlocked = pData.unlockedCharacters.Contains(currentViewChar.characterName) || currentViewChar.isDefaultUnlocked;
            bool isEquipped = (equippedChar != null && equippedChar.characterName == currentViewChar.characterName);
            heroesUI.UpdateVisuals(currentViewChar, pData, isUnlocked, isEquipped);
        }

        // Lobby
        LobbyUI lobby = UIManager.Instance.GetPanel<LobbyUI>();
        if (lobby != null && lobby.gameObject.activeSelf)
        {
            lobby.UpdateVisuals(pData, equippedChar, GameManager.Instance.currentMode);
        }
    }
}