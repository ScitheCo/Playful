using UnityEngine;
using FishNet;
using FishNet.Object;

public class BattleSceneStarter : MonoBehaviour
{
    // Adım 1'de oluşturduğun BattleManager Prefab'ını buraya sürükleyeceksin
    public GameObject battleManagerPrefab; 

    private void Start()
    {
        // Eğer sunucuysak veya Host isek
        if (InstanceFinder.IsServer)
        {
            SpawnBattleManager();
        }
        else
        {
            // Eğer Client isek ve sunucuya sonradan bağlandıysak (Offline sahneden geldiysek)
            // FishNet eventlerini dinleyip sunucu olduğumuz an spawn etmeliyiz.
            InstanceFinder.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
    }

    private void ServerManager_OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            SpawnBattleManager();
        }
    }

    private void SpawnBattleManager()
    {
        // Zaten sahnede varsa tekrar yaratma (Defansif kod)
        if (FindObjectOfType<BattleManager>() != null) return;

        // Prefab'ı oluştur
        GameObject go = Instantiate(battleManagerPrefab);
        
        // AĞ ÜZERİNDE DOĞUR (Spawn)
        // Bu komut, bağlı olan tüm Client'lara "Bu objeyi yarat ve ID'sini ben veriyorum" der.
        InstanceFinder.ServerManager.Spawn(go); 
    }
}