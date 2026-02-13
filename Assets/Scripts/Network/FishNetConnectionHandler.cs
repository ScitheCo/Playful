using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Managing.Scened;

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
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    public void StartConnection(bool amIHost)
    {
        if (_networkManager == null) return;

        // Önce temizlik yap (Garanti olsun)
        StopConnection();

        if (amIHost)
        {
            Debug.Log("<color=green>Rol: HOST - Sunucu Başlatılıyor...</color>");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("<color=yellow>Rol: CLIENT - Sunucuya Bağlanılıyor...</color>");
            _networkManager.ClientManager.StartConnection();
        }
    }

    // --- YENİ EKLENEN: BAĞLANTIYI KOPAR ---
    public void StopConnection()
    {
        if (_networkManager == null) return;

        // Hem Sunucuyu hem Client'ı durdur
        Debug.Log("🔌 FishNet Bağlantısı Temizleniyor...");
        _networkManager.ServerManager.StopConnection(true);
        _networkManager.ClientManager.StopConnection();
    }
    // ---------------------------------------

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("✅ Sunucu Hazır! Sahne Yükleniyor...");
            LoadBattleScene();
        }
    }

    private void LoadBattleScene()
    {
        SceneLoadData sld = new SceneLoadData("BattleScene");
        sld.ReplaceScenes = ReplaceOption.All;
        _networkManager.SceneManager.LoadGlobalScenes(sld);
    }
}