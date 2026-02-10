using UnityEngine;
using UnityEngine.UI; // Slider ve Image için şart!
using TMPro;

public class BattleUI : BasePanel
{
    [Header("Bar Referansları")]
    public Slider battleSlider; // Çakışmayı önlemek için tam adını yazdım
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;
    public GameObject playerShieldIcon;
    public GameObject enemyShieldIcon;
    
    [Header("Profil Görselleri")]
    public Image playerAvatar;
    public Image playerFrame;
    public Image enemyAvatar;
    public Image enemyFrame;

    [Header("Mana & Skill")]
    public Image manaBarFill;
    public Button skillButton;

    [Header("Zamanlayıcı")]
    public TextMeshProUGUI timerText;

    [Header("Efektler")]
    public ObjectShake cameraShaker;

    // Tekli güncellemeler için son değerleri hafızada tutuyoruz
    private float _cachedPlayerHP;
    private float _cachedEnemyHP;

    public override void Init()
    {
        // Başlangıçta kalkanları gizle
        if (playerShieldIcon) playerShieldIcon.SetActive(false);
        if (enemyShieldIcon) enemyShieldIcon.SetActive(false);
        if (skillButton) skillButton.interactable = false;
        
        // Shake referansı yoksa otomatik bul
        if (cameraShaker == null) cameraShaker = GetComponent<ObjectShake>();
    }
    
    public void SetAvatars(Sprite pAvatar, Sprite pFrame, Sprite eAvatar, Sprite eFrame)
    {
        if(playerAvatar) playerAvatar.sprite = pAvatar;
        if(playerFrame) playerFrame.sprite = pFrame;
        if(enemyAvatar) enemyAvatar.sprite = eAvatar;
        if(enemyFrame) enemyFrame.sprite = eFrame;
    }

    // --- ESKİ FONKSİYON (Geriye uyumluluk için) ---
    public void UpdateBattleBars(float currentHP, float maxHP, float enemyHP, float maxEnemyHP)
    {
        _cachedPlayerHP = currentHP;
        _cachedEnemyHP = enemyHP;
        RefreshVisuals();
    }

    // Sadece Oyuncu (Sol) Güncellenince
    public void UpdatePlayerBarOnly(float current, float max)
    {
        _cachedPlayerHP = current;
        RefreshVisuals();
    }

    // Sadece Rakip (Sağ) Güncellenince
    public void UpdateEnemyBarOnly(float current, float max)
    {
        _cachedEnemyHP = current;
        RefreshVisuals();
    }

    // Ortak Görsel Güncelleme Mantığı
    private void RefreshVisuals()
    {
        // 1. Yazıları Güncelle
        if (playerHealthText != null) playerHealthText.text = Mathf.FloorToInt(_cachedPlayerHP).ToString();
        if (enemyHealthText != null) enemyHealthText.text = Mathf.FloorToInt(_cachedEnemyHP).ToString();

        // 2. Slider'ı Güncelle
        // Önce Slider bağlı mı diye kontrol et (Hata vermemesi için)
        if (battleSlider != null)
        {
            float totalHealth = _cachedPlayerHP + _cachedEnemyHP;
            
            if (totalHealth > 0)
            {
                float ratio = _cachedPlayerHP / totalHealth;
                
                // DÜZELTME: 100f yerine Slider'ın kendi MaxValue değerini kullanıyoruz.
                // Eğer Inspector'da MaxValue 1 ise 1 ile, 100 ise 100 ile çarpar.
                battleSlider.value = ratio * battleSlider.maxValue; 
            }
            else
            {
                battleSlider.value = 0; // Herkes öldüyse
            }
        }
        else
        {
            // Eğer buraya düşüyorsa Inspector'dan Slider'ı tekrar sürükle!
            Debug.LogError("BattleUI: Battle Slider Inspector'da bağlı değil!");
        }
    }

    // --- DİĞER FONKSİYONLAR ---

    public void UpdateMana(float currentMana, float maxMana)
    {
        if (manaBarFill != null) manaBarFill.fillAmount = currentMana / maxMana;
        if (skillButton != null) skillButton.interactable = (currentMana >= maxMana);
    }

    public void SetPlayerShield(bool isActive)
    {
        if (playerShieldIcon) playerShieldIcon.SetActive(isActive);
    }

    public void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60F);
        int seconds = Mathf.FloorToInt(timeRemaining % 60F);
        if (timerText) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ShakeScreen(float duration, float magnitude)
    {
        if (cameraShaker) cameraShaker.Shake(duration, magnitude);
    }
}