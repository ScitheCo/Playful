using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;
using Newtonsoft.Json; // ARTIK STANDART BU

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;

    [Header("Durum")]
    public bool isLoggedIn = false;
    public string playFabId;
    public string displayName;

    // Eventler
    public static event Action<List<PlayFab.ClientModels.PlayerLeaderboardEntry>> OnLeaderboardLoaded;
    public static event Action<List<FriendInfo>> OnFriendsLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Login(); // Oyunu açınca otomatik gir
    }

    // =================================================================================
    // 1. KİMLİK VE GİRİŞ (AUTH)
    // =================================================================================

    public void Login()
    {
        Debug.Log("Sunucuya bağlanılıyor...");

        // Varsayılan ID (Gerçek Cihaz ID'si)
        string customId = SystemInfo.deviceUniqueIdentifier;

        // --- PARRELSYNC AYARI (Sadece Editörde Çalışır) ---
#if UNITY_EDITOR
        // Eğer ParrelSync klonu ise, ID'yi değiştir ki farklı oyuncu sayılsın
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("ParrelSync Klonu Algılandı: Farklı ID kullanılıyor.");
            customId += "_Clone"; // Örn: DeviceID_Clone olur
        }
#endif
        // --------------------------------------------------

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId, // Güncellenmiş ID'yi kullan
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true,
                GetTitleData = true 
            }
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        isLoggedIn = true;
        playFabId = result.PlayFabId;
        
        if (result.InfoResultPayload.PlayerProfile != null)
        {
            displayName = result.InfoResultPayload.PlayerProfile.DisplayName;
        }

        Debug.Log($"<color=green>GİRİŞ BAŞARILI!</color> ID: {playFabId}, İsim: {displayName}");
        
        // Giriş yapar yapmaz verileri çek
        LoadData();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"Giriş Hatası: {error.GenerateErrorReport()}");
    }

    public void SubmitName(string nameInput, Action onSuccess = null, Action<string> onError = null)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = nameInput };
        
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result => 
        {
            displayName = result.DisplayName;
            // GameManager'daki ismi de güncelle
            if (GameManager.Instance != null) GameManager.Instance.playerData.username = displayName;
            
            Debug.Log("İsim Güncellendi: " + displayName);
            onSuccess?.Invoke();
        }, 
        error => 
        {
            onError?.Invoke(error.ErrorMessage);
        });
    }

    // =================================================================================
    // 2. VERİ YÖNETİMİ (TEK STANDART: NEWTONSOFT)
    // =================================================================================

    public void SaveData(PlayerData data)
    {
        if (!isLoggedIn) return;

        // DÜZELTME: JsonUtility yerine Newtonsoft kullanıyoruz.
        // Key olarak "PlayerProfile" yerine "PlayerData" kullanıyoruz.
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PlayerData", JsonConvert.SerializeObject(data) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result => Debug.Log("Bulut Kayıt Başarılı ☁️"), OnError);
    }

    public void LoadData()
    {
        if (!isLoggedIn) return;
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        // DÜZELTME: Anahtar kelime "PlayerData"
        if (result.Data != null && result.Data.ContainsKey("PlayerData"))
        {
            string json = result.Data["PlayerData"].Value;
            
            // DÜZELTME: Newtonsoft ile okuma
            PlayerData loadedData = JsonConvert.DeserializeObject<PlayerData>(json);
            
            if (GameManager.Instance != null)
            {
                // GameManager'a veriyi teslim et, orası karakter seçimini vs. halleder
                GameManager.Instance.OnDataLoadedFromPlayFab(loadedData);
                
                // İsim senkronizasyonu
                if (!string.IsNullOrEmpty(displayName)) 
                    GameManager.Instance.playerData.username = displayName;

                Debug.Log("Veriler Yüklendi ve İşlendi 📥");
            }
        }
        else
        {
            Debug.Log("Yeni Hesap veya 'PlayerData' anahtarı yok. Varsayılan verilerle devam.");
            // Yeni hesapsa ve GameManager varsa, eldeki varsayılan veriyi kaydet ki PlayFab'da yer açılsın
            if (GameManager.Instance != null) 
                SaveData(GameManager.Instance.playerData);
        }
    }

    // =================================================================================
    // 3. İSTATİSTİK (STATS)
    // =================================================================================

    public void SendLeaderboardStats(int elo, int level)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = "RankedElo", Value = elo },
                new StatisticUpdate { StatisticName = "PlayerLevel", Value = level }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, result => Debug.Log("İstatistikler Gönderildi 📊"), OnError);
    }

    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "RankedElo",
            StartPosition = 0,
            MaxResultsCount = 10,
            ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
        };
        
        PlayFabClientAPI.GetLeaderboard(request, result => 
        {
            OnLeaderboardLoaded?.Invoke(result.Leaderboard);
        }, OnError);
    }

    // =================================================================================
    // 4. SOSYAL
    // =================================================================================

    public void AddFriend(string friendPlayFabId)
    {
        var request = new AddFriendRequest { FriendPlayFabId = friendPlayFabId };
        PlayFabClientAPI.AddFriend(request, result => Debug.Log("Arkadaş Eklendi!"), OnError);
    }

    /*public void GetFriends()
    {
        var request = new GetFriendsListRequest { IncludePlayFabId = true, IncludeSteamId = false };
        PlayFabClientAPI.GetFriendsList(request, result => 
        {
            OnFriendsLoaded?.Invoke(result.Friends);
        }, OnError);
    }*/

    void OnError(PlayFabError error)
    {
        Debug.LogError($"PlayFab Hatası: {error.GenerateErrorReport()}");
    }
}