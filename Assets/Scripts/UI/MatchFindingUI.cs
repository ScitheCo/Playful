using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MatchFindingUI : MonoBehaviour
{
    [Header("Sol Panel (Biz)")]
    public Image localAvatar;
    public TextMeshProUGUI localNameText;
    public TextMeshProUGUI localEloText;
    public TextMeshProUGUI localLevelText; // YENİ: Level Yazısı
    public Image localCharImage;
    public TextMeshProUGUI localCharNameText;

    [Header("Sağ Panel (Rakip)")]
    public GameObject enemySearchingGroup; 
    public GameObject enemyInfoGroup;      
    public Image enemyAvatar;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyEloText;
    public TextMeshProUGUI enemyLevelText; // YENİ: Level Yazısı
    public Image enemyCharImage;
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

    // Bizim bilgilerimizi doldurur
    public void SetLocalPlayerInfo(PlayerData pData, CharacterData cData)
    {
        localNameText.text = pData.username;
        localEloText.text = $"Elo: {pData.elo}";
        localLevelText.text = $"Lvl {pData.level}"; // Level'i ayrı yazıyoruz
        
        if (cData != null)
        {
            localCharImage.sprite = cData.avatar;
            localCharNameText.text = cData.characterName;
        }
    }

    // Rakip bulununca çalışır
    public void SetEnemyInfo(string name, int elo, int level, string charName) 
    {
        enemySearchingGroup.SetActive(false);
        enemyInfoGroup.SetActive(true);

        enemyNameText.text = name;
        enemyEloText.text = $"Elo: {elo}";
        enemyLevelText.text = $"Lvl {level}"; // Level'i ayrı yazıyoruz
        
        enemyCharNameText.text = charName;
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