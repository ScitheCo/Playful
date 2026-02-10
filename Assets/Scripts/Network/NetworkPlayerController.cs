using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkPlayerController : NetworkBehaviour
{
    // SyncVar: Sunucudan herkese yayılan veri
    public readonly SyncVar<float> _currentHealth = new SyncVar<float>(1000f);
    public readonly SyncVar<float> _maxHealth = new SyncVar<float>(1000f);
    public readonly SyncVar<string> _playerName = new SyncVar<string>("Player");

    public float currentHealth { get => _currentHealth.Value; set => _currentHealth.Value = value; }
    public float maxHealth { get => _maxHealth.Value; set => _maxHealth.Value = value; }
    public string playerName { get => _playerName.Value; set => _playerName.Value = value; }

    private void Awake()
    {
        _currentHealth.OnChange += OnHealthChanged;
    }

    private void OnDestroy()
    {
        _currentHealth.OnChange -= OnHealthChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (base.IsOwner)
        {
            name = "Local_NetworkPlayer";
            if (BattleManager.Instance != null) BattleManager.Instance.RegisterNetworkPlayer(this, true);
        }
        else
        {
            name = "Remote_NetworkPlayer";
            if (BattleManager.Instance != null) BattleManager.Instance.RegisterNetworkPlayer(this, false);
        }
        
        UpdateUI();
    }

    // --- YENİ: Sunucu Tarafından Statları Ayarla ---
    // Bunu BattleManager çağıracak
    public void SetStatsServer(float maxHP, string pName)
    {
        // Sadece sunucu SyncVar değiştirebilir
        if (!base.IsServer) return;

        maxHealth = maxHP;
        currentHealth = maxHP; // Canı fulle
        playerName = pName;
        
        // Debug.Log($"[Server] Statlar Ayarlandı: {pName} - HP: {maxHP}");
    }
    // ----------------------------------------------

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServer(float amount)
    {
        float newVal = currentHealth - amount;
        if (newVal < 0) newVal = 0;
        currentHealth = newVal;
    }

    private void OnHealthChanged(float prev, float next, bool asServer)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.UpdateNetworkUI(base.IsOwner, currentHealth, maxHealth);
        }
    }
}