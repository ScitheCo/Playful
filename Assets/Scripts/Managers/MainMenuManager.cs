using System;
using UnityEngine;
using System.Collections.Generic;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    private HeroesUI heroesUI;
    private LobbyUI lobbyUI;
    
    [Header("Veri")]
    public List<CharacterData> availableCharacters; 
    private int currentIndex = 0;

    private void Awake() { Instance = this; }

    private void OnEnable()
    {
        // Script aktif olunca dinlemeye başla
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDataUpdated += UpdateAllPanels;
        }
    }

    private void OnDisable()
    {
        // Script kapanınca (veya sahne değişince) dinlemeyi bırak
        // Bunu yapmazsan hafıza kaçağı (Memory Leak) olur!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDataUpdated -= UpdateAllPanels;
        }
    }

    void Start()
    {
        // ------------------------------------------------------------
        // 1. BAĞLANTI TEMİZLİĞİ (ZOMBİ EŞLEŞMEYİ ÖNLEME)
        // ------------------------------------------------------------
        
        // A) FishNet'i Kapat
        if (FishNetConnectionHandler.Instance != null)
        {
            FishNetConnectionHandler.Instance.StopConnection();
        }

        // B) PlayFab Biletlerini Zorla İptal Et (Hayalet Bilet Kalmasın)
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(
                new CancelAllMatchmakingTicketsForPlayerRequest
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabSettings.staticPlayer.EntityId,
                        Type = PlayFabSettings.staticPlayer.EntityType
                    },
                    QueueName = "RankedQueue" // Sırayla hepsini temizle
                }, null, null);

            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(
                new CancelAllMatchmakingTicketsForPlayerRequest
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabSettings.staticPlayer.EntityId,
                        Type = PlayFabSettings.staticPlayer.EntityType
                    },
                    QueueName = "CasualQueue"
                }, null, null);
            
            Debug.Log("🧹 Ana Menü: Eski biletler ve bağlantılar temizlendi.");
        }
        
        // ------------------------------------------------------------
        // 1. LOBBY PANEL BAĞLANTILARI
        // ------------------------------------------------------------
        lobbyUI = UIManager.Instance.GetPanel<LobbyUI>();
        heroesUI = UIManager.Instance.GetPanel<HeroesUI>();
        if (lobbyUI != null)
        {
            // SAVAŞ Butonu -> StartGameLoop fonksiyonuna gider
            lobbyUI.playButton.onClick.RemoveAllListeners();
            lobbyUI.playButton.onClick.AddListener(StartGameLoop);

            // Mod Seçim Butonu -> Mod panelini açar
            lobbyUI.gameModeButton.onClick.RemoveAllListeners();
            lobbyUI.gameModeButton.onClick.AddListener(OpenModeSelection);

            lobbyUI.settingsButton.onClick.RemoveAllListeners();
            lobbyUI.settingsButton.onClick.AddListener(() => SettingsManager.Instance.OpenSettings());
            
            heroesUI.backButton.onClick.RemoveAllListeners();
            heroesUI.backButton.onClick.AddListener(lobbyUI.GoToLobby);
            
            // BAŞLANGIÇ: Her şey hazırsa Lobby'e git
            lobbyUI.GoToLobby();
        }

        // ------------------------------------------------------------
        // 2. HEROES (KARAKTER) PANEL BAĞLANTILARI
        // ------------------------------------------------------------
        if (heroesUI != null)
        {
            heroesUI.nextButton.onClick.RemoveAllListeners();
            heroesUI.nextButton.onClick.AddListener(NextCharacter);

            heroesUI.prevButton.onClick.RemoveAllListeners();
            heroesUI.prevButton.onClick.AddListener(PrevCharacter);

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
            lobbyUI.GoToHeroes(); // Otomatik olarak karakter seçme ekranına atıyoruz
            return;
        }

        // 3. Her şey tamamsa Eşleşme Sahnesine git
        GameMode currentMode = GameManager.Instance.currentMode;
        Debug.Log($"Eşleşme Başlatılıyor... Mod: {currentMode}");
        
        SceneManager.LoadScene("MatchFindingScene");
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
            // EĞER ZATEN LİSTEDE VARSA EKLEME
            if (GameManager.Instance.playerData.unlockedCharacters.Contains(charName))
            {
                Debug.LogWarning("Bu karakter zaten açık: " + charName);
                return;
            }

            GameManager.Instance.playerData.unlockedCharacters.Add(charName);
            
            // Satın alınca otomatik seç ve kaydet
            GameManager.Instance.SetCharacter(availableCharacters[currentIndex]);
        }
        UpdateAllPanels();
    }

    // UI GÜNCELLEME
    public void UpdateAllPanels()
    {
        if (GameManager.Instance == null) return;

        PlayerData pData = GameManager.Instance.playerData;
        CharacterData currentViewChar = availableCharacters[currentIndex];
        CharacterData equippedChar = GameManager.Instance.selectedCharacter;

        // Heroes
        HeroesUI heroesUI = UIManager.Instance.GetPanel<HeroesUI>();
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