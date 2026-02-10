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
        if (PlayFabClientAPI.IsClientLoggedIn()) StartMatchmaking();
        else Debug.LogError("PlayFab Girişi Yapılmamış!");
    }

    public void StartMatchmaking()
    {
        Debug.Log("Maç Bileti Oluşturuluyor...");
        currentTimer = 0f;
        isMatchFound = false;

        PlayerData pData = GameManager.Instance.playerData;
        GameMode mode = GameManager.Instance.currentMode;
        CharacterData myChar = GameManager.Instance.selectedCharacter;

        // UI Güncelle
        uiController.SetLocalPlayerInfo(pData, myChar);

        if (mode == GameMode.Practice)
        {
            StartCoroutine(FakeMatchRoutine(1f));
            return;
        }

        // --- DÜZELTME: İSİM VE KARAKTER BİLGİSİNİ DE GÖNDERİYORUZ ---
        MatchmakingPlayerAttributes attributes = new MatchmakingPlayerAttributes();
        var dataDictionary = new Dictionary<string, object>();
        
        dataDictionary.Add("Elo", pData.elo);
        dataDictionary.Add("Level", pData.level);
        // Karşı taraf ismimizi ve karakterimizi görsün diye bunları da ekliyoruz:
        dataDictionary.Add("Name", pData.username); 
        dataDictionary.Add("CharName", myChar != null ? myChar.characterName : "Warrior");
        
        dataDictionary.Add("AvatarId", pData.avatarId);
        dataDictionary.Add("FrameId", pData.frameId);

        attributes.DataObject = dataDictionary;
        // -----------------------------------------------------------

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
        if (result.Status == "Matched")
        {
            isMatchFound = true;
            StopCoroutine(pollTicketCoroutine);
            GetMatchDetails(result.MatchId);
        }
        else if (result.Status == "Canceled")
        {
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
        StartCoroutine(FakeMatchRoutine(0.5f));
    }

    private IEnumerator FakeMatchRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerData pData = GameManager.Instance.playerData;
        
        // Rastgele Bot
        string fName = botNames[Random.Range(0, botNames.Length)];
        int fElo = Mathf.Clamp(pData.elo + Random.Range(-50, 50), 0, 9999);
        int fLevel = Mathf.Clamp(pData.level + Random.Range(-2, 3), 1, 99);
        CharacterData fChar = null;
        if (GameManager.Instance.allCharacters.Count > 0)
            fChar = GameManager.Instance.allCharacters[Random.Range(0, GameManager.Instance.allCharacters.Count)];
        int fAvatarId = Random.Range(0, GameManager.Instance.avatarList.Count);
        int fFrameId = Random.Range(0, GameManager.Instance.frameList.Count);

        GameManager.Instance.isFakeBotMatch = true;
        
        // SetMatchOpponent ARTIK YENİ PARAMETRELERİ ALIYOR
        GameManager.Instance.SetMatchOpponent(fName, fElo, fLevel, fChar != null ? fChar.characterName : "Warrior", fAvatarId, fFrameId);

        uiController.SetEnemyInfo(fName, fElo, fLevel, fChar != null ? fChar.characterName : "Unknown", fAvatarId, fFrameId);
        StartCoroutine(uiController.StartCountdownRoutine(StartGameScene));
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
                
                // YENİLERİ OKU
                if (data.ContainsKey("AvatarId")) eAvatar = int.Parse(data["AvatarId"].ToString());
                if (data.ContainsKey("FrameId")) eFrame = int.Parse(data["FrameId"].ToString());
            }

            GameManager.Instance.isFakeBotMatch = false;
            
            // Veriyi GameManager'a taşı
            GameManager.Instance.SetMatchOpponent(eName, eElo, eLevel, eChar, eAvatar, eFrame);

            // UI Güncelle
            uiController.SetEnemyInfo(eName, eElo, eLevel, eChar, eAvatar, eFrame);
            StartCoroutine(uiController.StartCountdownRoutine(StartGameScene));
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
        amIHost = false;
#endif
        if (FishNetConnectionHandler.Instance != null) FishNetConnectionHandler.Instance.StartConnection(amIHost);
    }

    private void OnMatchmakingError(PlayFabError error)
    {
        if (error.Error == PlayFabErrorCode.MatchmakingTicketMembershipLimitExceeded) return;
        Debug.LogError($"PlayFab Hatası: {error.GenerateErrorReport()}");
    }

    private void OnDestroy()
    {
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