using UnityEngine;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using Newtonsoft.Json; // JSON İşlemleri için

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Oyun Verileri")]
    public PlayerData playerData = new PlayerData();
    public CharacterData selectedCharacter;
    public GameMode currentMode;

    [Header("Veritabanı (Referans)")]
    // Oyundaki TÜM karakterleri buraya elle eklemelisin!
    // Oyun açıldığında kayıtlı ismi bu listede arayacağız.
    public List<CharacterData> allCharacters; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalSettings(); // Oyun Modunu yükle
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 1. GAME MODE KAYIT SİSTEMİ (PlayerPrefs) ---
    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        PlayerPrefs.SetInt("LastGameMode", (int)mode);
        PlayerPrefs.Save();
    }

    private void LoadLocalSettings()
    {
        if (PlayerPrefs.HasKey("LastGameMode"))
        {
            currentMode = (GameMode)PlayerPrefs.GetInt("LastGameMode");
        }
        else
        {
            currentMode = GameMode.Casual; // Varsayılan
        }
    }

    // --- 2. KARAKTER SEÇİM SİSTEMİ ---
    public void SetCharacter(CharacterData character)
    {
        selectedCharacter = character;
        
        // İsmi veriye kaydet
        if (character != null)
        {
            playerData.lastSelectedCharacterName = character.characterName;
            SaveGame(); // PlayFab'a yolla
        }
    }

    // PlayFab'dan veri gelince bu fonksiyonu çağıracağız (PlayFabManager içinden)
    public void OnDataLoadedFromPlayFab(PlayerData loadedData)
    {
        playerData = loadedData;

        // Kayıtlı karakter ismini bul ve objeyi seç
        if (!string.IsNullOrEmpty(playerData.lastSelectedCharacterName))
        {
            CharacterData foundChar = allCharacters.Find(x => x.characterName == playerData.lastSelectedCharacterName);
            if (foundChar != null)
            {
                selectedCharacter = foundChar;
                Debug.Log($"Otomatik Seçilen Karakter: {foundChar.characterName}");
            }
        }
        
        // Eğer hiç karakter yoksa ve varsayılan bir karakter varsa onu seçtir
        if (selectedCharacter == null && allCharacters.Count > 0)
        {
             // Opsiyonel: selectedCharacter = allCharacters[0];
        }
    }

    // --- 3. PLAYFAB KAYIT (Basitleştirilmiş Entegrasyon) ---
    public void SaveGame()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PlayerData", JsonConvert.SerializeObject(playerData) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, 
            result => Debug.Log("Veri Kaydedildi"), 
            error => Debug.LogError("Kayıt Hatası: " + error.ErrorMessage));
    }
}