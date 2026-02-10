using UnityEngine;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Karakter Seçimi")]
    public CharacterData playerProfile; 
    public CharacterData enemyProfile; 

    [Header("Canlı Veriler (Sadece Gösterge)")]
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    public float currentMana = 0;
    
    private bool opponentDisconnected = false;
    
    [Header("Network Referansları")]
    public NetworkPlayerController localNetworkPlayer;
    public NetworkPlayerController remoteNetworkPlayer;

    private float playerMaxHP;
    private float enemyMaxHP;
    private float playerMaxMana;
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;
    public float roundTime = 120f;
    private bool isGameActive = true;

    [HideInInspector] public float totalDamageDealt = 0;
    [HideInInspector] public float matchDurationCounter = 0;
    [HideInInspector] public int maxCombo = 0;

    private void Awake() { Instance = this; }

    void Start()
    {
        // 1. Verileri Yükle
        if (GameManager.Instance.currentEnemyCharacter != null) enemyProfile = GameManager.Instance.currentEnemyCharacter;
        if (GameManager.Instance.selectedCharacter != null) playerProfile = GameManager.Instance.selectedCharacter;

        InitializeCharacters();
        
        // --- GÖRSEL YÜKLEME (Avatar/Çerçeve) ---
        var ui = UIManager.Instance?.GetPanel<BattleUI>();
        if (ui != null)
        {
            // Bizim Veriler
            Sprite myAv = GameManager.Instance.GetAvatarSprite(GameManager.Instance.playerData.avatarId);
            Sprite myFr = GameManager.Instance.GetFrameSprite(GameManager.Instance.playerData.frameId);
            
            // Rakip Veriler (GameManager'dan gelir)
            Sprite enAv = GameManager.Instance.GetAvatarSprite(GameManager.Instance.currentEnemyAvatarId);
            Sprite enFr = GameManager.Instance.GetFrameSprite(GameManager.Instance.currentEnemyFrameId);
            
            ui.SetAvatars(myAv, myFr, enAv, enFr);
        }
        // ----------------------------------------

        // Network Güvenlik Ağı
        NetworkPlayerController[] players = FindObjectsOfType<NetworkPlayerController>();
        foreach(var p in players)
        {
            if (p.IsOwner) RegisterNetworkPlayer(p, true);
            else RegisterNetworkPlayer(p, false);
        }

        UpdateAllUI();
        
        // --- DISCONNECT DİNLEME ---
        // Sadece Online maçlarda dinle
        bool isRealOnlineMatch = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;
        if (isRealOnlineMatch && InstanceFinder.NetworkManager != null)
        {
            // ServerManager: Sunucu tarafında bir client koptuğunda çalışır
            // ClientManager: Bizim bağlantımız koptuğunda çalışır
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }
    }
    
    private void OnDestroy()
    {
        // Event aboneliğini temizle
        if (InstanceFinder.NetworkManager != null)
        {
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
    }

    // --- RAKİP AYRILDI MI? ---
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        // Eğer bağlantı durumu 'Stopped' olduysa biri koptu demektir.
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.LogWarning($"Bir oyuncu koptu! ID: {conn.ClientId}");
            
            // Eğer oyun hala devam ediyorsa
            if (isGameActive && !opponentDisconnected)
            {
                opponentDisconnected = true;
                HandleOpponentDisconnect();
            }
        }
    }

    private void HandleOpponentDisconnect()
    {
        Debug.Log("RAKİP OYUNDAN AYRILDI! HÜKMEN GALİBİYET.");
        
        // UI'da mesaj göster (İstersen özel bir panel açabilirsin)
        // Şimdilik direkt kazanma ekranına atıyoruz
        
        // Rakibin canını 0 yap
        currentEnemyHealth = 0;
        
        // Oyunu bitir ve kazanma verisi gönder
        EndGame(true); // true = Rakip kaçtığı için özel mesaj gösterebilirsin EndGameManager'da
    }

    void InitializeCharacters()
    {
        if (playerProfile != null) { playerMaxHP = playerProfile.maxHealth; playerMaxMana = playerProfile.maxMana; }
        else { playerMaxHP = 1000; playerMaxMana = 100; }

        if (enemyProfile != null) enemyMaxHP = enemyProfile.maxHealth;
        else enemyMaxHP = 1000;

        currentPlayerHealth = playerMaxHP;
        currentEnemyHealth = enemyMaxHP;
        currentMana = 0;
    }

    public void RegisterNetworkPlayer(NetworkPlayerController npc, bool isLocal)
    {
        if (isLocal) localNetworkPlayer = npc;
        else remoteNetworkPlayer = npc;

        // Statları Sunucuya Bildir (Sadece Host yapar)
        if (npc.IsServer)
        {
            if (isLocal) npc.SetStatsServer(playerMaxHP, GameManager.Instance.playerData.username);
            else npc.SetStatsServer(enemyMaxHP, GameManager.Instance.currentEnemyName);
        }
    }

    // Network'ten veri gelince çalışır (Ayna Mantığı Burada İşler)
    public void UpdateNetworkUI(bool isOwnerOfObject, float current, float max)
    {
        var ui = UIManager.Instance?.GetPanel<BattleUI>();
        if (ui == null) return;

        // MANTIK: 
        // Eğer güncellenen obje benimse (isOwnerOfObject = true) -> Sol Bar (Local)
        // Eğer güncellenen obje benim değilse (Host veya Rakip) -> Sağ Bar (Remote)
        
        if (isOwnerOfObject) 
        { 
            currentPlayerHealth = current; 
            ui.UpdatePlayerBarOnly(current, max); 
        }
        else 
        { 
            currentEnemyHealth = current; 
            ui.UpdateEnemyBarOnly(current, max); 
        }
        
        CheckWinCondition();
    }

    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isGameActive) return;

        bool isRealOnlineMatch = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;

        if (isRealOnlineMatch)
        {
            // === ONLINE HASAR MANTIĞI ===
            // BURADA YEREL DEĞİŞKENLERE (currentPlayerHealth) DOKUNMUYORUZ!
            // Sadece emri veriyoruz, sonuç UpdateNetworkUI ile dönecek.

            if (!isPlayerTakingDamage)
            {
                // Ben rakibe vuruyorum -> Remote objeye RPC at
                if (remoteNetworkPlayer != null)
                {
                    if (playerProfile != null) damageAmount *= playerProfile.redDamageMultiplier;
                    
                    // "RequireOwnership = false" sayesinde Clone da bunu çağırabilir
                    remoteNetworkPlayer.TakeDamageServer(damageAmount);
                    
                    totalDamageDealt += damageAmount;
                    UIManager.Instance?.GetPanel<BattleUI>()?.ShakeScreen(0.2f, 10f);
                    Debug.Log("Rakibe Vurdum (RPC Gönderildi)");
                }
                else
                {
                    Debug.LogWarning("Rakip Network Objesi Bulunamadı! Hasar gitmedi.");
                }
            }
            else
            {
                // Kendime hasar veriyorum (Bomba vs.) -> Local objeye RPC at
                if (localNetworkPlayer != null)
                {
                    if (isPlayerShieldActive) damageAmount *= 0.3f;
                    localNetworkPlayer.TakeDamageServer(damageAmount);
                    UIManager.Instance?.GetPanel<BattleUI>()?.ShakeScreen(0.2f, 15f);
                }
            }
        }
        else
        {
            // === OFFLINE HASAR MANTIĞI ===
            if (isPlayerTakingDamage)
            {
                if (isPlayerShieldActive) damageAmount *= 0.3f;
                currentPlayerHealth -= damageAmount;
                UIManager.Instance?.GetPanel<BattleUI>()?.ShakeScreen(0.2f, 15f);
            }
            else
            {
                if (playerProfile != null) damageAmount *= playerProfile.redDamageMultiplier;
                currentEnemyHealth -= damageAmount;
                totalDamageDealt += damageAmount;
            }
            currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
            currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);
            var ui = UIManager.Instance?.GetPanel<BattleUI>();
            if (ui != null) ui.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
            CheckWinCondition();
        }
    }

    // --- DİĞERLERİ AYNI ---
    public void Heal(bool isPlayer, float amount) 
    {
         if (!isGameActive) return;
        if (isPlayer)
        {
            if (playerProfile != null) amount *= playerProfile.greenHealMultiplier;
            currentPlayerHealth += amount;
            currentPlayerHealth = Mathf.Min(currentPlayerHealth, playerMaxHP);
        }
        else
        {
            currentEnemyHealth += amount;
            currentEnemyHealth = Mathf.Min(currentEnemyHealth, enemyMaxHP);
        }
        // Online modda UI elle güncellenmez, Network'ten beklenir
        bool isRealOnlineMatch = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;
        if (!isRealOnlineMatch)
             UIManager.Instance?.GetPanel<BattleUI>()?.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
    }
    public void AddMana(float amount) {
        if (playerProfile != null) amount *= playerProfile.blueManaMultiplier;
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, playerMaxMana);
        UIManager.Instance?.GetPanel<BattleUI>()?.UpdateMana(currentMana, playerMaxMana);
    }
    public void UseSkill() {
        if (currentMana >= playerMaxMana && playerProfile != null && playerProfile.activeSkill != null)
        {
            playerProfile.activeSkill.Trigger(this, EnemyManager.Instance, true);
            currentMana = 0;
            UIManager.Instance?.GetPanel<BattleUI>()?.UpdateMana(currentMana, playerMaxMana);
        }
    }
    public void ActivatePlayerShield(float duration) {
        isPlayerShieldActive = true; shieldDuration = duration;
        UIManager.Instance?.GetPanel<BattleUI>()?.SetPlayerShield(true);
    }
    void DeactivateShield() {
        isPlayerShieldActive = false;
        UIManager.Instance?.GetPanel<BattleUI>()?.SetPlayerShield(false);
    }
    public void UpdateComboStats(int currentChain) { if (currentChain > maxCombo) maxCombo = currentChain; }
    void UpdateAllUI() {
        var ui = UIManager.Instance?.GetPanel<BattleUI>();
        if (ui != null) {
            ui.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
            ui.UpdateMana(currentMana, playerMaxMana);
        }
    }
    void CheckWinCondition() { if (currentEnemyHealth <= 0 || currentPlayerHealth <= 0) EndGame(); }
    void EndGame(bool opponentFled = false)
    {
        if (!isGameActive) return;
        isGameActive = false;
        
        if (EnemyManager.Instance != null) EnemyManager.Instance.StopBattle();

        bool isVictory = currentPlayerHealth > currentEnemyHealth;
        if (opponentFled) isVictory = true; // Rakip kaçtıysa kesin zafer

        if (EndGameManager.Instance != null)
        {
            // İstersen ProcessGameResult'a "opponentFled" parametresi ekleyip 
            // Sonuç ekranında "Rakip Korkup Kaçtı!" yazdırabilirsin.
            EndGameManager.Instance.ProcessGameResult(isVictory, totalDamageDealt, maxCombo, matchDurationCounter, currentPlayerHealth);
        }
    }
    public void EnemyUseSkill() {
        if (enemyProfile != null && enemyProfile.activeSkill != null) enemyProfile.activeSkill.Trigger(this, EnemyManager.Instance, false);
    }
}