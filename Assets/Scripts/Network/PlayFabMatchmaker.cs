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

    [Header("Durum")]
    private string ticketId;
    private Coroutine pollTicketCoroutine;
    private bool isMatchFound = false;

    private void Start()
    {
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            StartMatchmaking();
        }
        else
        {
            Debug.LogError("PlayFab Girişi Yapılmamış! Lütfen önce Login olun.");
        }
    }

    public void StartMatchmaking()
    {
        Debug.Log("Maç Bileti Oluşturuluyor...");

        PlayerData pData = GameManager.Instance.playerData;
        GameMode mode = GameManager.Instance.currentMode;

        // UI: Kendi bilgilerimizi (Level dahil) ekrana yaz
        uiController.SetLocalPlayerInfo(pData, GameManager.Instance.selectedCharacter);

        // --- ATTRIBUTES HAZIRLIĞI ---
        MatchmakingPlayerAttributes attributes = new MatchmakingPlayerAttributes();
        var dataDictionary = new Dictionary<string, object>();
        
        // ÖNEMLİ: Hangi modda olursak olalım, hem Elo hem Level bilgisini gönderiyoruz.
        // Böylece karşı taraf bizim levelimizi her zaman görebilir.
        dataDictionary.Add("Elo", pData.elo);
        dataDictionary.Add("Level", pData.level);

        attributes.DataObject = dataDictionary;

        // Kuyruk Adını Belirle
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
                GiveUpAfterSeconds = 120, 
                QueueName = queueName
            },
            OnTicketCreated,
            OnMatchmakingError
        );
    }

    private void OnTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        Debug.Log($"Bilet Oluşturuldu! ID: {ticketId}");
        pollTicketCoroutine = StartCoroutine(PollTicketStatus());
    }

    private IEnumerator PollTicketStatus()
    {
        while (!isMatchFound)
        {
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
            isMatchFound = true;
            Debug.LogWarning("Bilet iptal edildi.");
        }
    }

    private void GetMatchDetails(string matchId)
    {
        Debug.Log("Rakip Bilgileri Çekiliyor...");

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
            if (member.Entity.Id != myEntityId)
            {
                enemy = member;
                break;
            }
        }

        if (enemy != null)
        {
            // Verileri Varsayılan Olarak 0 Ayarla
            int enemyElo = 0;
            int enemyLevel = 0;

            if (enemy.Attributes != null && enemy.Attributes.DataObject != null)
            {
                var enemyData = (JsonObject)enemy.Attributes.DataObject;

                // Hem Elo hem Level verisini çekiyoruz
                if (enemyData.ContainsKey("Elo"))
                    enemyElo = int.Parse(enemyData["Elo"].ToString());
                
                if (enemyData.ContainsKey("Level"))
                    enemyLevel = int.Parse(enemyData["Level"].ToString());
            }

            Debug.Log($"Rakip: Elo {enemyElo}, Lvl {enemyLevel}");

            // UI'ya Gönder (Artık level parametresi de var)
            uiController.SetEnemyInfo("Enemy Player", enemyElo, enemyLevel, "Unknown Class");

            StartCoroutine(uiController.StartCountdownRoutine(StartGameScene));
        }
    }

    private void StartGameScene()
    {
        Debug.Log("🚀 SAVAŞ BAŞLIYOR! (FishNet Bağlantısı...)");
    }

    private void OnMatchmakingError(PlayFabError error)
    {
        Debug.LogError($"PlayFab Hatası: {error.GenerateErrorReport()}");
    }

    private void OnDestroy()
    {
        if (!isMatchFound && !string.IsNullOrEmpty(ticketId))
        {
            PlayFabMultiplayerAPI.CancelMatchmakingTicket(
                new CancelMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = GameManager.Instance.currentMode == GameMode.Ranked ? "RankedQueue" : "CasualQueue"
                },
                null, null
            );
        }
    }
}