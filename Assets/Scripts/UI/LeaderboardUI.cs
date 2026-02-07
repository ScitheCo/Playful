using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PlayFab.ClientModels;
using TMPro;

public class LeaderboardUI : BasePanel
{
    [Header("Referanslar")]
    public Transform contentParent; // Scroll View'in "Content" objesi
    public GameObject rowPrefab;    // Az önce yaptığımız satır prefabı
    public Button closeButton;

    [Header("Durum")]
    public TextMeshProUGUI statusText; // "Yükleniyor..." yazısı için

    public override void Init()
    {
        base.Init();
        closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        // Panel açılınca PlayFabManager'ı dinlemeye başla
        PlayFabManager.OnLeaderboardLoaded += UpdateUI;
        
        // Veriyi iste
        statusText.text = "Sıralama Yükleniyor...";
        statusText.gameObject.SetActive(true);
        
        // Mevcut listeyi temizle
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // İsteği gönder
        if (PlayFabManager.Instance != null)
            PlayFabManager.Instance.GetLeaderboard();
    }

    private void OnDisable()
    {
        // Panel kapanınca dinlemeyi bırak (Hata vermemesi için)
        PlayFabManager.OnLeaderboardLoaded -= UpdateUI;
    }

    void UpdateUI(List<PlayerLeaderboardEntry> leaderboard)
    {
        statusText.gameObject.SetActive(false); // Yükleniyor yazısını gizle

        foreach (var entry in leaderboard)
        {
            // Yeni satır oluştur
            GameObject newRow = Instantiate(rowPrefab, contentParent);
            
            // Veriyi içine doldur
            LeaderboardRowUI rowScript = newRow.GetComponent<LeaderboardRowUI>();
            rowScript.Setup(entry.Position, entry.DisplayName, entry.StatValue);
        }
    }
}