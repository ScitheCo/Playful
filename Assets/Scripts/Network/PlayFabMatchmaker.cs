using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using PlayFab.Json; 
using System.Collections;
using System.Collections.Generic;

public class PlayFabMatchmaker : MonoBehaviour
{
    [Header("Referanslar")]
    public MatchFindingUI uiController;

    private string ticketId;
    private Coroutine pollTicketCoroutine;
    private bool isMatchFound = false;
    
    // Timeout Ayarı
    private float matchTimeout = 120f;
    private float currentTimer = 0f;
    private string[] botNames = { "DragonSlayer", "ShadowHunter", "ProGamer99", "KnightX" };

    private void Start()
    {
        // Oturum açık mı kontrol et, değilse bekle
        if (PlayFabClientAPI.IsClientLoggedIn()) 
        {
            StartMatchmaking();
        }
        else 
        {
            Debug.LogWarning("⚠️ PlayFab oturumu kapalı görünüyor! Tekrar bağlanılıyor...");
            StartCoroutine(WaitForLoginAndStart());
        }
    }

    // --- YENİ: OTURUM BEKLEME MANTIĞI ---
    private IEnumerator WaitForLoginAndStart()
    {
        // PlayFabManager varsa giriş yapmayı tetikle
        if (PlayFabManager.Instance != null && !PlayFabManager.Instance.isLoggedIn)
        {
            PlayFabManager.Instance.Login();
        }

        // Giriş yapılana kadar bekle (Maksimum 10 saniye)
        float waitTime = 0;
        while (!PlayFabClientAPI.IsClientLoggedIn() && waitTime < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.Log("✅ Oturum tazelendi, maç aranıyor...");
            StartMatchmaking();
        }
        else
        {
            Debug.LogError("❌ PlayFab Girişi Yapılamadı! Lütfen internetini kontrol et.");
            // İstersen burada ana menüye geri atabilirsin
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }
    }
    // -------------------------------------

    public void StartMatchmaking()
    {
        Debug.Log("Maç Bileti Oluşturuluyor...");
        currentTimer = 0f;
        isMatchFound = false;

        PlayerData pData = GameManager.Instance.playerData;
        GameMode mode = GameManager.Instance.currentMode;
        CharacterData myChar = GameManager.Instance.selectedCharacter;

        // UI Güncelle (Hata burada çözülecek)
        if (uiController != null)
        {
            uiController.SetLocalPlayerInfo(pData, myChar);
        }
        else
        {
            Debug.LogError("MatchFindingUI referansı eksik!");
            return;
        }

        if (mode == GameMode.Practice)
        {
            StartCoroutine(FakeMatchRoutine(1f));
            return;
        }

        // Özellikleri Hazırla
        MatchmakingPlayerAttributes attributes = new MatchmakingPlayerAttributes();
        var dataDictionary = new Dictionary<string, object>();
        
        dataDictionary.Add("Elo", pData.elo);
        dataDictionary.Add("Level", pData.level);
        dataDictionary.Add("Name", pData.username);
        dataDictionary.Add("CharName", myChar != null ? myChar.characterName : "Warrior");
        dataDictionary.Add("AvatarId", pData.avatarId);
        dataDictionary.Add("FrameId", pData.frameId);

        attributes.DataObject = dataDictionary;

        string queueName = (mode == GameMode.Ranked) ? "RankedQueue" : "CasualQueue";

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabSettings.staticPlayer.EntityId,
                        Type = PlayFabSettings.staticPlayer.EntityType
                    },
                    Attributes = attributes
                },
                GiveUpAfterSeconds = (int)matchTimeout, 
                QueueName = queueName
            },
            OnTicketCreated,
            OnMatchmakingError
        );
    }

    private void OnTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        Debug.Log($"Bilet Oluşturuldu: {ticketId}");
        pollTicketCoroutine = StartCoroutine(PollTicketStatus());
    }

    private IEnumerator PollTicketStatus()
    {
        while (!isMatchFound)
        {
            currentTimer += 6.0f;
            if (currentTimer >= matchTimeout)
            {
                CancelTicketAndStartFakeMatch();
                yield break;
            }

            yield return new WaitForSeconds(6.0f);

            PlayFabMultiplayerAPI.GetMatchmakingTicket(
                new GetMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue"
                },
                OnTicketStatusReceived,
                OnMatchmakingError
            );
        }
    }

    private void OnTicketStatusReceived(GetMatchmakingTicketResult result)
    {
        // Debug.Log($"Bilet Durumu: {result.Status}"); // Çok spam yaparsa kapatabilirsin

        if (result.Status == "Matched")
        {
            isMatchFound = true;
            if (pollTicketCoroutine != null) StopCoroutine(pollTicketCoroutine);
            Debug.Log("Eşleşme Bulundu! Detaylar alınıyor...");
            GetMatchDetails(result.MatchId);
        }
        else if (result.Status == "Canceled")
        {
            Debug.LogWarning("Bilet iptal edilmiş. Fake maça geçiliyor.");
            if (!isMatchFound) CancelTicketAndStartFakeMatch();
        }
    }

    private void CancelTicketAndStartFakeMatch()
    {
        isMatchFound = true;
        if (pollTicketCoroutine != null) StopCoroutine(pollTicketCoroutine);

        if (!string.IsNullOrEmpty(ticketId))
        {
            PlayFabMultiplayerAPI.CancelMatchmakingTicket(
                new CancelMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue"
                }, null, null);
        }
        Debug.Log("Zaman aşımı veya iptal. Bot maçı başlatılıyor.");
        StartCoroutine(FakeMatchRoutine(0.5f));
    }

    private IEnumerator FakeMatchRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerData pData = GameManager.Instance.playerData;
        
        string fName = botNames[Random.Range(0, botNames.Length)];
        int fElo = Mathf.Clamp(pData.elo + Random.Range(-50, 50), 0, 9999);
        int fLevel = Mathf.Clamp(pData.level + Random.Range(-2, 3), 1, 99);
        
        CharacterData fChar = null;
        if (GameManager.Instance.allCharacters.Count > 0)
            fChar = GameManager.Instance.allCharacters[Random.Range(0, GameManager.Instance.allCharacters.Count)];
            
        int fAvatarId = Random.Range(0, GameManager.Instance.avatarList.Count);
        int fFrameId = Random.Range(0, GameManager.Instance.frameList.Count);

        GameManager.Instance.isFakeBotMatch = true;
        GameManager.Instance.SetMatchOpponent(fName, fElo, fLevel, fChar != null ? fChar.characterName : "Warrior", fAvatarId, fFrameId);
        
        if (uiController != null)
            uiController.SetEnemyInfo(fName, fElo, fLevel, fChar != null ? fChar.characterName : "Unknown", fAvatarId, fFrameId);
            
        if (uiController != null)
            StartCoroutine(uiController.StartCountdownRoutine(StartGameScene));
        else 
            StartGameScene();
    }

    private void GetMatchDetails(string matchId)
    {
        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue",
                ReturnMemberAttributes = true, 
                EscapeObject = false 
            },
            OnMatchDetailsReceived,
            OnMatchmakingError
        );
    }

    private void OnMatchDetailsReceived(GetMatchResult result)
    {
        string myEntityId = PlayFabSettings.staticPlayer.EntityId;
        MatchmakingPlayerWithTeamAssignment enemy = null;

        foreach (var member in result.Members)
        {
            if (member.Entity.Id != myEntityId) { enemy = member; break; }
        }

        if (enemy != null)
        {
            int eElo = 0; int eLevel = 0; int eAvatar = 0; int eFrame = 0;
            string eName = "Enemy"; string eChar = "Warrior";

            if (enemy.Attributes != null && enemy.Attributes.DataObject != null)
            {
                var data = (JsonObject)enemy.Attributes.DataObject;
                if (data.ContainsKey("Elo")) eElo = int.Parse(data["Elo"].ToString());
                if (data.ContainsKey("Level")) eLevel = int.Parse(data["Level"].ToString());
                if (data.ContainsKey("Name")) eName = data["Name"].ToString();
                if (data.ContainsKey("CharName")) eChar = data["CharName"].ToString();
                if (data.ContainsKey("AvatarId")) eAvatar = int.Parse(data["AvatarId"].ToString());
                if (data.ContainsKey("FrameId")) eFrame = int.Parse(data["FrameId"].ToString());
            }

            GameManager.Instance.isFakeBotMatch = false;
            GameManager.Instance.SetMatchOpponent(eName, eElo, eLevel, eChar, eAvatar, eFrame);
            
            if (uiController != null)
            {
                uiController.SetEnemyInfo(eName, eElo, eLevel, eChar, eAvatar, eFrame);
                StartCoroutine(uiController.StartCountdownRoutine(StartGameScene));
            }
            else
            {
                StartGameScene();
            }
        }
    }

    private void StartGameScene()
    {
        if (GameManager.Instance.isFakeBotMatch)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
        }
        else
        {
            ConnectNetwork();
        }
    }

    private void ConnectNetwork()
    {
        bool amIHost = false;
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone()) amIHost = false; else amIHost = true;
#else
        amIHost = false; // Gerçek build'de PlayFab sunucusu host olur veya P2P mantığı değişir. P2P için şimdilik false kalsın.
#endif
        if (FishNetConnectionHandler.Instance != null) 
            FishNetConnectionHandler.Instance.StartConnection(amIHost);
    }

    private void OnMatchmakingError(PlayFabError error)
    {
        if (error.Error == PlayFabErrorCode.MatchmakingTicketMembershipLimitExceeded)
        {
            Debug.LogWarning("⚠️ Aktif bilet çakışması! Eski biletler temizleniyor...");
            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(
                new CancelAllMatchmakingTicketsForPlayerRequest
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabSettings.staticPlayer.EntityId,
                        Type = PlayFabSettings.staticPlayer.EntityType
                    },
                    QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue"
                },
                result => 
                {
                    Debug.Log("Bilet temizlendi. Tekrar deneniyor...");
                    Invoke(nameof(StartMatchmaking), 1f); 
                },
                err => Debug.LogError("Temizleme hatası: " + err.GenerateErrorReport())
            );
            return;
        }

        Debug.LogError($"PlayFab Hatası: {error.GenerateErrorReport()}");
    }

    private void OnDestroy()
    {
        // Sahneden çıkarken bilet iptal et (Eğer maç bulunmadıysa)
        if (!isMatchFound && !string.IsNullOrEmpty(ticketId))
        {
            PlayFabMultiplayerAPI.CancelMatchmakingTicket(new CancelMatchmakingTicketRequest
            {
                TicketId = ticketId,
                QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue"
            }, null, null);
        }
    }
}