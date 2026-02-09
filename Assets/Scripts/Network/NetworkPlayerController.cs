using UnityEngine;
using FishNet.Object; // FishNet kütüphanesi
using FishNet.Connection;

// DİKKAT: MonoBehaviour değil, NetworkBehaviour kullanıyoruz!
public class NetworkPlayerController : NetworkBehaviour
{
    // Bu fonksiyon obje sahneye doğduğu an (Spawn) çalışır.
    // Start() yerine bunu kullanırız.
    public override void OnStartClient()
    {
        base.OnStartClient();

        // IsOwner: Bu obje benim bilgisayarımda mı yaratıldı?
        if (base.IsOwner)
        {
            Debug.Log("<color=green>BEN GELDİM! (Local Player)</color>");
            name = "MyNetworkPlayer";
            
            // GameManager'a haber ver: "Benim kontrolcüm bu!"
            // GameManager.Instance.SetLocalPlayer(this); (Bunu sonra yazacağız)
        }
        else
        {
            Debug.Log("<color=red>RAKİP GELDİ! (Remote Player)</color>");
            name = "EnemyNetworkPlayer";
            
            // GameManager'a haber ver: "Rakip de bağlandı!"
        }
    }
}