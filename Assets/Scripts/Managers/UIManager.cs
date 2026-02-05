using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // Tüm panelleri tipine göre tutan liste
    private List<BasePanel> allPanels;

    private void Awake()
    {
        // Singleton Yapısı
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sahnedeki tüm BasePanel türevlerini bul ve listeye ekle
        // (inactive olanları da bulması için true parametresi verdik)
        allPanels = FindObjectsOfType<BasePanel>(true).ToList();

        // Panelleri Başlat
        foreach (var panel in allPanels)
        {
            panel.Init();
            if (panel.startActive) panel.Show();
            else panel.Hide();
        }
    }

    // İstediğimiz paneli türüne göre (Örn: GetPanel<BattleUI>()) getiren fonksiyon
    public T GetPanel<T>() where T : BasePanel
    {
        foreach (var panel in allPanels)
        {
            if (panel is T) return (T)panel;
        }
        Debug.LogError($"Panel bulunamadı: {typeof(T).Name}");
        return null;
    }

    // Bir paneli aç, diğer her şeyi kapat (Opsiyonel kullanım)
    public void ShowPanel<T>() where T : BasePanel
    {
        foreach (var panel in allPanels)
        {
            if (panel is T) panel.Show();
            else panel.Hide();
        }
    }
}