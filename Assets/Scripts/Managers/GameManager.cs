using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Oyun Verileri")]
    public PlayerData playerData = new PlayerData();
    public CharacterData selectedCharacter;
    public GameMode currentMode;
    
    [Header("Görsel Kütüphane (Inspector'dan Doldur!)")]
    public List<Sprite> avatarList; // Tüm avatarları buraya sürükle
    public List<Sprite> frameList;  // Tüm çerçeveleri buraya sürükle

    [Header("Mevcut Maç Verileri (YENİ)")]
    // Savaş sahnesine taşınacak rakip verileri
    public string currentEnemyName = "Enemy";
    public int currentEnemyElo = 1200;
    public int currentEnemyLevel = 1;
    public CharacterData currentEnemyCharacter; // Rakibin karakteri
    public int currentEnemyAvatarId = 0;
    public int currentEnemyFrameId = 0;
    
    // Fake Bot Kontrolü
    public bool isFakeBotMatch = false; 

    public event Action OnDataUpdated;

    [Header("Veritabanı")]
    public List<CharacterData> allCharacters; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Eğer yeni başlıyorsak ve karakter yoksa ekle
            if (playerData.unlockedCharacters.Count == 0) 
                playerData.unlockedCharacters.Add("Warrior");

            LoadLocalSettings();
        }
        else { Destroy(gameObject); }
    }
    
    // --- RESİM GETİRME YARDIMCILARI ---
    public Sprite GetAvatarSprite(int id)
    {
        if (avatarList != null && id >= 0 && id < avatarList.Count) return avatarList[id];
        return null; // veya varsayılan bir sprite
    }

    public Sprite GetFrameSprite(int id)
    {
        if (frameList != null && id >= 0 && id < frameList.Count) return frameList[id];
        return null;
    }

    // --- RAKİP VERİSİ KAYDETME (GÜNCELLENDİ) ---
    public void SetMatchOpponent(string name, int elo, int level, string charName, int avatarId, int frameId)
    {
        currentEnemyName = name;
        currentEnemyElo = elo;
        currentEnemyLevel = level;
        currentEnemyAvatarId = avatarId;
        currentEnemyFrameId = frameId;
        
        CharacterData foundChar = allCharacters.Find(x => x.characterName == charName);
        if (foundChar != null) currentEnemyCharacter = foundChar;
        else if (allCharacters.Count > 0) currentEnemyCharacter = allCharacters[0];
    }

    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        isFakeBotMatch = false; 
        PlayerPrefs.SetInt("LastGameMode", (int)mode);
        PlayerPrefs.Save();
    }

    private void LoadLocalSettings()
    {
        if (PlayerPrefs.HasKey("LastGameMode")) currentMode = (GameMode)PlayerPrefs.GetInt("LastGameMode");
        else currentMode = GameMode.Casual;
    }

    public void SetCharacter(CharacterData character)
    {
        selectedCharacter = character;
        if (character != null)
        {
            playerData.lastSelectedCharacterName = character.characterName;
            SaveGame(); 
        }
    }

    public void OnDataLoadedFromPlayFab(PlayerData loadedData)
    {
        // 1. KOPYA TEMİZLİĞİ (Distinct)
        // Listeyi tarar, aynı isimden birden fazla varsa teke düşürür.
        if (loadedData.unlockedCharacters != null)
        {
            loadedData.unlockedCharacters = loadedData.unlockedCharacters.Distinct().ToList();
        }

        // 2. GÜVENLİK (Eğer liste boş geldiyse varsayılan karakteri ekle)
        if (loadedData.unlockedCharacters == null || loadedData.unlockedCharacters.Count == 0)
        {
            loadedData.unlockedCharacters = new List<string>() { "Warrior" };
        }

        playerData = loadedData;

        if (!string.IsNullOrEmpty(playerData.lastSelectedCharacterName))
        {
            CharacterData foundChar = allCharacters.Find(x => x.characterName == playerData.lastSelectedCharacterName);
            if (foundChar != null) selectedCharacter = foundChar;
        }
        
        OnDataUpdated?.Invoke();
        
        // Temizlenmiş halini hemen kaydet ki buluttaki JSON da düzelsin
        SaveGame(); 
    }

    public void SaveGame()
    {
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.isLoggedIn)
            PlayFabManager.Instance.SaveData(playerData);
    }
    
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }
    private void OnApplicationQuit() { SaveGame(); }
}