using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;

// Bu script oyunun PlayFab beynidir.
public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;

    [Header("Durum")]
    public bool isLoggedIn = false;
    public string playFabId;
    public string displayName; // Oyuncunun görünen adı

    // Eventler (UI güncellemeleri için callback)
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
    // 1. KİMLİK VE GİRİŞ (AUTH & PROFILE)
    // =================================================================================

    public void Login()
    {
        Debug.Log("Sunucuya bağlanılıyor...");
        
        // Android/iOS build aldığında burası SystemInfo.deviceUniqueIdentifier yerine
        // LoginWithGoogle veya LoginWithApple kullanılacak şekilde güncellenebilir.
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId,
            // Profil bilgisini de girişte isteyelim
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
        
        // İsmi var mı kontrol et
        if (result.InfoResultPayload.PlayerProfile != null)
        {
            displayName = result.InfoResultPayload.PlayerProfile.DisplayName;
        }

        Debug.Log($"<color=green>GİRİŞ BAŞARILI!</color> ID: {playFabId}, İsim: {displayName}");

        // Verileri Çek
        LoadData();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"Giriş Hatası: {error.GenerateErrorReport()}");
    }

    // İsim Değiştirme (İlk açılışta veya Profilden)
    public void SubmitName(string nameInput, Action onSuccess = null, Action<string> onError = null)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = nameInput };
        
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result => 
        {
            displayName = result.DisplayName;
            Debug.Log("İsim Güncellendi: " + displayName);
            onSuccess?.Invoke();
        }, 
        error => 
        {
            Debug.LogError("İsim Hatası: " + error.ErrorMessage);
            onError?.Invoke(error.ErrorMessage);
        });
    }

    // =================================================================================
    // 2. VERİ YÖNETİMİ (CLOUD SAVE / LOAD) - JSON
    // =================================================================================

    public void SaveData(PlayerData data)
    {
        if (!isLoggedIn) return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PlayerProfile", JsonUtility.ToJson(data) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result => Debug.Log("Bulut Kayıt Başarılı ☁️"), OnError);
    }

    public void LoadData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("PlayerProfile"))
        {
            string json = result.Data["PlayerProfile"].Value;
            PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerData = loadedData;
                
                // Eğer sunucudaki isimle local veri uyuşmuyorsa eşitle
                if (!string.IsNullOrEmpty(displayName)) 
                    GameManager.Instance.playerData.username = displayName;
                
                Debug.Log("Veriler Yüklendi 📥");
            }
        }
        else
        {
            Debug.Log("Yeni Hesap: Varsayılan verilerle devam ediliyor.");
        }
    }

    // =================================================================================
    // 3. İSTATİSTİK VE LİDERLİK TABLOSU (STATS & LEADERBOARD)
    // =================================================================================

    // Maç sonu bu fonksiyon çağrılacak
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
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, result => 
        {
            // UI Manager'a haber ver (Observer Pattern)
            OnLeaderboardLoaded?.Invoke(result.Leaderboard);
        }, OnError);
    }

    // =================================================================================
    // 4. SOSYAL VE ARKADAŞLAR (SOCIAL)
    // =================================================================================

    public void AddFriend(string friendPlayFabId)
    {
        // PlayFab'da arkadaş ekleme
        var request = new AddFriendRequest { FriendPlayFabId = friendPlayFabId };
        PlayFabClientAPI.AddFriend(request, result => Debug.Log("Arkadaş Eklendi!"), OnError);
    }

    public void GetFriends()
    {
        var request = new GetFriendsListRequest();
        PlayFabClientAPI.GetFriendsList(request, result => 
        {
            OnFriendsLoaded?.Invoke(result.Friends);
        }, OnError);
    }

    // =================================================================================
    // YARDIMCILAR
    // =================================================================================

    void OnError(PlayFabError error)
    {
        Debug.LogError($"PlayFab Hatası: {error.GenerateErrorReport()}");
    }
}