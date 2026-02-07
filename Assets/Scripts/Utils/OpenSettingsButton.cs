using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenSettingsButton : MonoBehaviour
{
    private void Start()
    {
        // 1. Üzerindeki Buton komponentini bul
        Button btn = GetComponent<Button>();

        if (btn != null)
        {
            // 2. Tıklanma olayına dinleyici ekle
            // (Öncekileri temizle ki çift tıklama olmasın)
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        // 3. SettingsManager'a ulaş ve paneli açtır
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OpenSettings();
        }
        else
        {
            Debug.LogError("Hata: Sahnede SettingsManager bulunamadı!");
        }
    }
}