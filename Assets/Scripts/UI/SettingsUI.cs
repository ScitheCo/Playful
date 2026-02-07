using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : BasePanel
{
    [Header("Ses Ayarları")]
    public Slider musicSlider;
    public Slider sfxSlider;
    
    [Header("Butonlar")]
    public Button closeButton; 

    // --- BU KISIM EKLENDİ (SİHİRLİ DOKUNUŞ) ---
    private void OnEnable()
    {
        // Panel her açıldığında (SetActive true olunca) burası çalışır
        if (GameManager.Instance != null)
        {
            float musicVol = GameManager.Instance.playerData.musicVolume;
            float sfxVol = GameManager.Instance.playerData.sfxVolume;
            
            // Sliderları güncelle ama Listener'ları tetikleme!
            // (Yoksa sonsuz döngüye girip sesi bozabilir)
            musicSlider.SetValueWithoutNotify(musicVol);
            sfxSlider.SetValueWithoutNotify(sfxVol);
        }
    }
    // -------------------------------------------

    public override void Init()
    {
        base.Init();
    }
    
    public void UpdateVisuals(float musicVol, float sfxVol)
    {
        // Manager'dan manuel çağırmak istersek diye kalsın
        musicSlider.value = musicVol;
        sfxSlider.value = sfxVol;
    }
}