using UnityEngine;
using UnityEngine.UI;

public class InGameMenuManager : MonoBehaviour
{
    [Header("Sahne Referansı")]
    public Button openMenuButton; // Sağ üstteki menü açma butonu

    void Start()
    {
        // 1. Menü Açma Butonunu Bağla (Sağ Üstteki Dişli)
        if (openMenuButton != null)
        {
            openMenuButton.onClick.RemoveAllListeners();
            openMenuButton.onClick.AddListener(OpenMenuPanel);
        }

        // 2. Panel İçi Butonları Bağla
        // UIManager üzerinden paneli buluyoruz
        InGameMenuUI ui = UIManager.Instance.GetPanel<InGameMenuUI>();
        if (ui != null)
        {
            // Butonları temizle ve yeniden bağla
            ui.resumeButton.onClick.RemoveAllListeners();
            ui.resumeButton.onClick.AddListener(CloseMenuPanel);

            ui.settingsButton.onClick.RemoveAllListeners();
            ui.settingsButton.onClick.AddListener(OnSettingsClicked); // BURAYI DÜZELTTİK

            ui.surrenderButton.onClick.RemoveAllListeners();
            ui.surrenderButton.onClick.AddListener(Surrender);
            
            // Başlangıçta paneli gizle
            ui.Hide();
        }
    }

    // Menü panelini açar
    void OpenMenuPanel()
    {
        UIManager.Instance.GetPanel<InGameMenuUI>().Show();
    }

    // Menü panelini kapatır (Devam Et)
    void CloseMenuPanel()
    {
        UIManager.Instance.GetPanel<InGameMenuUI>().Hide();
    }

    // Ayarlar butonuna basılınca çalışır
    void OnSettingsClicked()
    {
        // 1. Önce bu küçük menüyü gizle
        CloseMenuPanel();

        // 2. SettingsManager üzerinden büyük ayarlar panelini aç
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OpenSettings();
        }
        else
        {
            Debug.LogError("SettingsManager sahnede bulunamadı!");
        }
    }

    void Surrender()
    {
        if (BattleManager.Instance != null)
        {
            CloseMenuPanel();
            
            // Mevcut can kadar hasar alıp öl
            float currentHP = BattleManager.Instance.currentPlayerHealth;
            BattleManager.Instance.TakeDamage(true, currentHP + 100); 
            
            Debug.Log("Oyuncu Teslim Oldu.");
        }
    }
}