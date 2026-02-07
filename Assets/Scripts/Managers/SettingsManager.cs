using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    private void Awake()
    {
        // --- KRİTİK AYAR ---
        // Eğer bu script GameManager objesi üzerindeyse (ki öyle olmalı),
        // Sahneler arası yok olmaz.
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // GameManager zaten DDOL ise buna gerek yok
        }
        else
        {
            // Eğer yeni sahnede unutulmuş bir SettingsManager varsa onu yok et
            Destroy(gameObject);
            return;
        }
    }

    public void OpenSettings()
    {
        SettingsUI ui = UIManager.Instance.GetPanel<SettingsUI>();
        
        if (ui != null)
        {
            // 1. Önceki dinleyicileri temizle (Çakışmayı önle)
            ui.musicSlider.onValueChanged.RemoveAllListeners();
            ui.sfxSlider.onValueChanged.RemoveAllListeners();
            ui.closeButton.onClick.RemoveAllListeners();

            // 2. Yeni dinleyicileri ekle
            ui.musicSlider.onValueChanged.AddListener(SetMusicVolume);
            ui.sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            ui.closeButton.onClick.AddListener(CloseSettings);

            // 3. Paneli Aç (OnEnable burada otomatik çalışacak ve veriyi yükleyecek)
            ui.Show();
        }
    }

    public void CloseSettings()
    {
        UIManager.Instance.GetPanel<SettingsUI>().Hide();
        // Ayarlar kapanınca kaydet
        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    void SetMusicVolume(float value)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerData.musicVolume = value;
            // AudioManager.Instance.SetMusic(value);
        }
    }

    void SetSFXVolume(float value)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerData.sfxVolume = value;
            // AudioManager.Instance.SetSFX(value);
        }
    }
}