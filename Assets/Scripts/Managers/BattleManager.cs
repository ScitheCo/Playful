using UnityEngine;
using FishNet;
using FishNet.Object; 
using FishNet.Object.Synchronizing; 
using FishNet.Connection;
using FishNet.Transporting;
using System.Collections.Generic;

public class BattleManager : NetworkBehaviour 
{
    public static BattleManager Instance;
    public BattleUI battleUI;

    [Header("Karakter Seçimi")]
    public CharacterData playerProfile;
    public CharacterData enemyProfile; 

    // === ADİL BAŞLANGIÇ SİSTEMİ ===
    public readonly SyncVar<bool> _isMatchStarted = new SyncVar<bool>(false);
    private List<NetworkPlayerController> connectedPlayers = new List<NetworkPlayerController>();

    // === ZAMAN SİSTEMİ (DOĞRUDAN SENKRONİZASYON) ===
    // ServerUptime yerine direkt olarak kalan süreyi sunucudan gönderiyoruz.
    // Bu sayede "Host saati ile Client saati uyuşmazlığı" sorunu kökten çözülür.
    public readonly SyncVar<float> _networkedTimer = new SyncVar<float>(120f);

    public float TimeRemaining
    {
        get
        {
            // Veri henüz yüklenmediyse varsayılanı dön
            if (!isSetupComplete) return matchTime;

            // Bağlantı koptuysa 0
            if (opponentDisconnected) return 0;
            
            bool isOnline = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;

            // Maç başlamadıysa ful süre göster
            if (isOnline && !_isMatchStarted.Value) return matchTime;

            if (isOnline)
            {
                // ONLINE: Direkt sunucudan gelen veriyi kullan
                return Mathf.Max(0, _networkedTimer.Value);
            }
            else
            {
                // OFFLINE: Yerel değişkeni kullan (Aşağıda Update'te azalıyor)
                return Mathf.Max(0, _offlineTimer);
            }
        }
    }

    [Header("Canlı Veriler")]
    public float currentPlayerHealth;
    public float currentEnemyHealth;
    public float currentMana = 0;
    
    public float matchTime = 120f;
    private float _offlineTimer = 120f; // Offline mod için yerel sayaç
    
    private bool isSetupComplete = false;
    private bool opponentDisconnected = false;
    
    [Header("Network Referansları")]
    public NetworkPlayerController localNetworkPlayer;
    public NetworkPlayerController remoteNetworkPlayer;

    private float playerMaxHP;
    private float enemyMaxHP;
    private float playerMaxMana;
    
    private bool isPlayerShieldActive = false;
    private float shieldDuration = 0f;
    
    private bool isGameActive = true;
    [HideInInspector] public float totalDamageDealt = 0;
    [HideInInspector] public float matchDurationCounter = 0;
    [HideInInspector] public int maxCombo = 0;

    private void Awake() 
    { 
        Instance = this; 
        Application.runInBackground = true; 
        
        // Dinleyiciler
        _isMatchStarted.OnChange += OnMatchStatusChanged;
    }
    
    // Maç başladığında (Kilit açıldığında) çalışır
    private void OnMatchStatusChanged(bool prev, bool next, bool asServer)
    {
        if (next) Debug.Log($"[{(asServer ? "SERVER" : "CLIENT")}] MAÇ BAŞLADI! Süre akıyor.");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Süreyi sunucu tarafında hazırla
        _networkedTimer.Value = matchTime;
    }

    void Start()
    {
        isSetupComplete = false;
        battleUI = UIManager.Instance?.GetPanel<BattleUI>();

        if (GameManager.Instance.currentEnemyCharacter != null) enemyProfile = GameManager.Instance.currentEnemyCharacter;
        if (GameManager.Instance.selectedCharacter != null) playerProfile = GameManager.Instance.selectedCharacter;

        InitializeCharacters();
        
        if (currentPlayerHealth <= 0) currentPlayerHealth = 1000;
        if (currentEnemyHealth <= 0) currentEnemyHealth = 1000;

        UpdateAllUI();

        // OFFLINE ZAMANLAYICI HAZIRLIĞI
        bool isOnline = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;
        if (!isOnline)
        {
            _offlineTimer = matchTime;
        }

        if (isOnline && InstanceFinder.NetworkManager != null)
        {
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        isSetupComplete = true; 
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkPlayerController[] players = FindObjectsOfType<NetworkPlayerController>();
        foreach(var p in players)
        {
            if (p.IsOwner) RegisterNetworkPlayer(p, true);
            else RegisterNetworkPlayer(p, false);
        }
    }
    
    private void OnDestroy()
    {
        _isMatchStarted.OnChange -= OnMatchStatusChanged;
        
        if (InstanceFinder.NetworkManager != null)
        {
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    // --- HOST ve CLIENT Bağlantı Kopma Olayları ---
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            if (isGameActive && !opponentDisconnected)
            {
                Debug.Log($"[HOST] Rakip Oyuncu ({conn.ClientId}) Ayrıldı.");
                opponentDisconnected = true;
                HandleOpponentDisconnect();
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            if (isGameActive && !IsServer && !opponentDisconnected)
            {
                Debug.Log("[CLIENT] Sunucu Bağlantısı Koptu (Host Ayrıldı).");
                opponentDisconnected = true;
                HandleOpponentDisconnect();
            }
        }
    }

    private void HandleOpponentDisconnect()
    {
        Debug.Log("RAKİP AYRILDI! HÜKMEN GALİBİYET.");
        currentEnemyHealth = 0;
        EndGame(true);
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

        if (npc.IsServer)
        {
            if (isLocal) npc.SetStatsServer(playerMaxHP, GameManager.Instance.playerData.username);
            else npc.SetStatsServer(enemyMaxHP, GameManager.Instance.currentEnemyName);

            // --- OYUNCU SAYMA VE BAŞLATMA ---
            if (!connectedPlayers.Contains(npc))
            {
                connectedPlayers.Add(npc);
                Debug.Log($"[SERVER] Oyuncu Bağlandı: {connectedPlayers.Count}/2");

                if (connectedPlayers.Count >= 2)
                {
                    StartMatchServer();
                }
            }
        }
    }

    // --- OYUNU BAŞLATAN FONKSİYON ---
    private void StartMatchServer()
    {
        if (_isMatchStarted.Value) return;

        Debug.Log("[SERVER] Herkes Hazır! Maç Başlıyor.");
        
        // Sadece kilidi açıyoruz. Süre Update'te düşecek.
        _networkedTimer.Value = matchTime;
        _isMatchStarted.Value = true;
    }

    public void UpdateNetworkUI(bool isOwnerOfObject, float current, float max)
    {
        if (battleUI == null) return;
        
        if (isOwnerOfObject) 
        { 
            currentPlayerHealth = current;
            battleUI.UpdatePlayerBarOnly(current, max); 
        }
        else 
        { 
            currentEnemyHealth = current;
            battleUI.UpdateEnemyBarOnly(current, max); 
        }
        CheckWinCondition();
    }

    public void UpdateNetworkShield(bool isOwnerOfObject, bool isActive)
    {
        if (battleUI == null) return;

        if (isOwnerOfObject)
        {
            battleUI.SetPlayerShield(isActive);
            isPlayerShieldActive = isActive; 
        }
        else
        {
            if (battleUI.enemyShieldIcon != null) battleUI.enemyShieldIcon.SetActive(isActive);
        }
    }

    public void TakeDamage(bool isPlayerTakingDamage, float damageAmount)
    {
        if (!isSetupComplete || !isGameActive) return;
        if (opponentDisconnected) return;

        bool isRealOnlineMatch = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;

        // --- KİLİT KONTROLÜ ---
        if (isRealOnlineMatch && !_isMatchStarted.Value) return; 
        // ---------------------

        if (isRealOnlineMatch)
        {
            if (!isPlayerTakingDamage)
            {
                if (remoteNetworkPlayer != null)
                {
                    if (playerProfile != null) damageAmount *= playerProfile.redDamageMultiplier;
                    remoteNetworkPlayer.TakeDamageServer(damageAmount);
                    totalDamageDealt += damageAmount;
                    battleUI.ShakeScreen(0.2f, 10f);
                    battleUI.ShowFloatingText(true, damageAmount, false);
                }
            }
            else
            {
                if (localNetworkPlayer != null)
                {
                    if (isPlayerShieldActive) damageAmount *= 0.3f;
                    localNetworkPlayer.TakeDamageServer(damageAmount);
                    battleUI.ShakeScreen(0.2f, 15f);
                    battleUI.ShowFloatingText(false, damageAmount, false);
                }
            }
        }
        else
        {
            if (isPlayerTakingDamage)
            {
                if (isPlayerShieldActive) damageAmount *= 0.3f;
                currentPlayerHealth -= damageAmount;
                battleUI.ShakeScreen(0.2f, 15f);
                battleUI.ShowFloatingText(false, damageAmount, false);
            }
            else
            {
                if (playerProfile != null) damageAmount *= playerProfile.redDamageMultiplier;
                currentEnemyHealth -= damageAmount;
                totalDamageDealt += damageAmount;
                battleUI.ShowFloatingText(true, damageAmount, false);
            }
            currentPlayerHealth = Mathf.Max(0, currentPlayerHealth);
            currentEnemyHealth = Mathf.Max(0, currentEnemyHealth);
            
            battleUI.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
            CheckWinCondition();
        }
    }

    public void ActivatePlayerShield(float duration)
    {
        isPlayerShieldActive = true;
        shieldDuration = duration;
        
        if (!opponentDisconnected && localNetworkPlayer != null) localNetworkPlayer.SetShieldServer(true);
        else battleUI.SetPlayerShield(true);
    }

    void Update()
    {
        if (!isSetupComplete || !isGameActive) return;

        bool isOnline = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;

        // --- MAÇ BAŞLAMADIYSA BEKLE ---
        if (isOnline && !_isMatchStarted.Value) return;
        // ------------------------------
        
        matchDurationCounter += Time.deltaTime;

        if (opponentDisconnected) return;

        // === ZAMANLAYICI MANTIĞI ===
        if (isOnline)
        {
            // SADECE SUNUCU ZAMANI DÜŞÜRÜR
            if (IsServer)
            {
                if (_networkedTimer.Value > 0)
                {
                    _networkedTimer.Value -= Time.deltaTime;
                }
                else
                {
                    EndGame(); // Süre bitti
                }
            }
            // Client sadece _networkedTimer.Value değerini okur (Yukarıdaki TimeRemaining içinde)
        }
        else
        {
            // OFFLINE: Yerel sayacı düşür
            if (_offlineTimer > 0)
            {
                _offlineTimer -= Time.deltaTime;
                if (_offlineTimer <= 0) EndGame();
            }
        }

        // UI Güncelle
        float currentSeconds = TimeRemaining;
        battleUI.UpdateTimer(currentSeconds);

        if (isPlayerShieldActive)
        {
            shieldDuration -= Time.deltaTime;
            if (shieldDuration <= 0) DeactivateShield();
        }
    }

    void DeactivateShield()
    {
        isPlayerShieldActive = false;
        if (!opponentDisconnected && localNetworkPlayer != null) localNetworkPlayer.SetShieldServer(false);
        else battleUI.SetPlayerShield(false);
    }

    void CheckWinCondition() 
    { 
        if (!isSetupComplete) return;
        if (currentEnemyHealth <= 0 || currentPlayerHealth <= 0) EndGame(); 
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
            battleUI.ShowFloatingText(false, amount, true);
        }
        else
        {
            currentEnemyHealth += amount;
            currentEnemyHealth = Mathf.Min(currentEnemyHealth, enemyMaxHP);
            battleUI.ShowFloatingText(true, amount, true);
        }
        bool isRealOnlineMatch = (GameManager.Instance.currentMode != GameMode.Practice) && !GameManager.Instance.isFakeBotMatch;
        if (!isRealOnlineMatch)
            battleUI.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
    }
    public void AddMana(float amount) {
        if (playerProfile != null) amount *= playerProfile.blueManaMultiplier;
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, playerMaxMana);
        battleUI.UpdateMana(currentMana, playerMaxMana);
    }
    public void UseSkill() {
        if (currentMana >= playerMaxMana && playerProfile != null && playerProfile.activeSkill != null)
        {
            playerProfile.activeSkill.Trigger(this, EnemyManager.Instance, true);
            currentMana = 0;
            battleUI.UpdateMana(currentMana, playerMaxMana);
        }
    }
    public void UpdateComboStats(int currentChain) { if (currentChain > maxCombo) maxCombo = currentChain; }
    void UpdateAllUI() {
        if (battleUI != null) {
            battleUI.UpdateBattleBars(currentPlayerHealth, playerMaxHP, currentEnemyHealth, enemyMaxHP);
            battleUI.UpdateMana(currentMana, playerMaxMana);
        }
    }
    void EndGame(bool opponentFled = false)
    {
        if (!isGameActive) return;
        isGameActive = false;
        if (EnemyManager.Instance != null) EnemyManager.Instance.StopBattle();
        bool isVictory = currentPlayerHealth > currentEnemyHealth;
        if (opponentFled) isVictory = true;
        if (EndGameManager.Instance != null) EndGameManager.Instance.ProcessGameResult(isVictory, totalDamageDealt, maxCombo, matchDurationCounter, currentPlayerHealth);
    }
    public void EnemyUseSkill() {
        if (enemyProfile != null && enemyProfile.activeSkill != null) enemyProfile.activeSkill.Trigger(this, EnemyManager.Instance, false);
    }
}