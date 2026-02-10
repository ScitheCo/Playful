using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Managing.Scened; // Sahne yönetimi

public class FishNetConnectionHandler : MonoBehaviour
{
    public static FishNetConnectionHandler Instance;
    private NetworkManager _networkManager;

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

    private void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        
        if (_networkManager == null)
            Debug.LogError("FishNetConnectionHandler: Sahnede NetworkManager yok!");
        else
        {
            // Sunucu durumunu dinlemeye başla
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }
    }

    private void OnDestroy()
    {
        // Dinlemeyi bırak (Hafıza kaçağını önle)
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    // PlayFabMatchmaker buradan tetikler
    public void StartConnection(bool amIHost)
    {
        if (_networkManager == null) return;

        if (amIHost)
        {
            Debug.Log("<color=green>Rol: HOST - Sunucu Başlatılıyor...</color>");
            // Sadece sunucuyu başlat, sahneyi event içinde yükleyeceğiz
            _networkManager.ServerManager.StartConnection();
            
            // Host aynı zamanda bir Client'tır, kendini de bağla
            _networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("<color=yellow>Rol: CLIENT - Sunucuya Bağlanılıyor...</color>");
            // Client direkt bağlanır (IP: localhost varsayılan)
            _networkManager.ClientManager.StartConnection();
        }
    }

    // Sunucu durumu değişince FishNet bu fonksiyonu otomatik çağırır
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        // Eğer sunucu başarıyla başladıysa (Started)
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("✅ Sunucu Hazır! Sahne Yükleniyor...");
            LoadBattleScene();
        }
    }

    private void LoadBattleScene()
    {
        SceneLoadData sld = new SceneLoadData("BattleScene");
        sld.ReplaceScenes = ReplaceOption.All; // Eski sahneleri kapat
        _networkManager.SceneManager.LoadGlobalScenes(sld);
    }
}