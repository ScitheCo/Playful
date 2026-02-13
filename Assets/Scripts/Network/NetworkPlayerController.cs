using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkPlayerController : NetworkBehaviour
{
    // --- MEVCUT DEĞİŞKENLER ---
    public readonly SyncVar<float> _currentHealth = new SyncVar<float>(1000f);
    public readonly SyncVar<float> _maxHealth = new SyncVar<float>(1000f);
    public readonly SyncVar<string> _playerName = new SyncVar<string>("Player");

    // --- YENİ: KALKAN DURUMU (SYNCVAR) ---
    // Bu değişken değiştiğinde "OnShieldChanged" fonksiyonu herkes için çalışır.
    public readonly SyncVar<bool> _isShieldActive = new SyncVar<bool>(false);

    // --- WRAPPERS ---
    public float currentHealth { get => _currentHealth.Value; set => _currentHealth.Value = value; }
    public float maxHealth { get => _maxHealth.Value; set => _maxHealth.Value = value; }
    public string playerName { get => _playerName.Value; set => _playerName.Value = value; }
    
    // Kalkan Wrapper
    public bool isShieldActive { get => _isShieldActive.Value; set => _isShieldActive.Value = value; }

    private void Awake()
    {
        _currentHealth.OnChange += OnHealthChanged;
        _isShieldActive.OnChange += OnShieldChanged; // Dinleyici ekle
    }

    private void OnDestroy()
    {
        _currentHealth.OnChange -= OnHealthChanged;
        _isShieldActive.OnChange -= OnShieldChanged; // Temizle
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

    public void SetStatsServer(float maxHP, string pName)
    {
        if (!base.IsServer) return;
        maxHealth = maxHP;
        currentHealth = maxHP;
        playerName = pName;
    }

    // --- YENİ: KALKAN RPC ---
    // İstemci (Biz) sunucuya "Kalkanımı aç/kapat" der.
    [ServerRpc]
    public void SetShieldServer(bool active)
    {
        isShieldActive = active;
        // SyncVar değiştiği için OnShieldChanged otomatik tetiklenir
    }

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

    // --- YENİ: KALKAN DEĞİŞİNCE ÇALIŞAN FONKSİYON ---
    private void OnShieldChanged(bool prev, bool next, bool asServer)
    {
        // UI Güncellemesini BattleManager'a bildir
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.UpdateNetworkShield(base.IsOwner, next);
        }
    }

    private void UpdateUI()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.UpdateNetworkUI(base.IsOwner, currentHealth, maxHealth);
        }
    }
}