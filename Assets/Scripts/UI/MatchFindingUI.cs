using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MatchFindingUI : MonoBehaviour
{
    [Header("Sol Panel (Biz)")]
    public Image localAvatar;
    public Image localFrame;
    public TextMeshProUGUI localNameText;
    public TextMeshProUGUI localEloText;
    public TextMeshProUGUI localLevelText;
    public Image localCharImage; // Karakter Resmi
    public TextMeshProUGUI localCharNameText;

    [Header("Sağ Panel (Rakip)")]
    public GameObject enemySearchingGroup; 
    public GameObject enemyInfoGroup;      
    public Image enemyAvatar;
    public Image enemyFrame;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyEloText;
    public TextMeshProUGUI enemyLevelText;
    public Image enemyCharImage; // Rakip Karakter Resmi
    public TextMeshProUGUI enemyCharNameText;

    [Header("Genel")]
    public TextMeshProUGUI countdownText; 
    public Button cancelButton;

    private void Start()
    {
        ResetUI();
    }

    public void ResetUI()
    {
        enemyInfoGroup.SetActive(false);     
        enemySearchingGroup.SetActive(true);
        countdownText.gameObject.SetActive(false); 
    }

    public void SetLocalPlayerInfo(PlayerData pData, CharacterData cData)
    {
        localNameText.text = pData.username;
        localEloText.text = $"Elo: {pData.elo}";
        localLevelText.text = $"Lvl {pData.level}";
        localAvatar.sprite = GameManager.Instance.GetAvatarSprite(pData.avatarId);
        localFrame.sprite = GameManager.Instance.GetFrameSprite(pData.frameId);
        
        if (cData != null)
        {
            localCharImage.sprite = cData.avatar;
            localCharNameText.text = cData.characterName;
            
            // HATALI KOD SİLİNDİ: localAvatar.sprite = cData.avatar; YAPMIYORUZ.
            // Eğer User Profile resmin varsa buraya ayrıca eklersin.
        }
    }

    public void SetEnemyInfo(string name, int elo, int level, string charName, int avatarId, int frameId)
    {
        enemySearchingGroup.SetActive(false);
        enemyInfoGroup.SetActive(true);

        enemyNameText.text = name;
        enemyEloText.text = $"Elo: {elo}";
        enemyLevelText.text = $"Lvl {level}";
        enemyCharNameText.text = charName;
        
        enemyAvatar.sprite = GameManager.Instance.GetAvatarSprite(avatarId);
        enemyFrame.sprite = GameManager.Instance.GetFrameSprite(frameId);

        // Rakip karakter resmini bul
        if (GameManager.Instance != null && GameManager.Instance.allCharacters != null)
        {
            CharacterData foundChar = GameManager.Instance.allCharacters.Find(x => x.characterName == charName);
            
            if (foundChar != null)
            {
                if (enemyCharImage != null) enemyCharImage.sprite = foundChar.avatar;
                // HATALI KOD SİLİNDİ: enemyAvatar'a karakter resmi koymuyoruz.
            }
        }
    }

    public IEnumerator StartCountdownRoutine(System.Action onComplete)
    {
        countdownText.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(false);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "SAVAŞ!";
        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke();
    }
}