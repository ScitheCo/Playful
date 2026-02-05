using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUI : BasePanel
{
    [Header("Bar Referansları")]
    public Slider battleSlider;
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;
    public GameObject playerShieldIcon;
    public GameObject enemyShieldIcon;

    [Header("Mana & Skill")]
    public Image manaBarFill;
    public Button skillButton;

    [Header("Zamanlayıcı")]
    public TextMeshProUGUI timerText;

    [Header("Efektler")]
    public ObjectShake cameraShaker; // ObjectShake scripti bu panelde olmalı

    public override void Init()
    {
        // Başlangıçta kalkanları gizle
        if (playerShieldIcon) playerShieldIcon.SetActive(false);
        if (enemyShieldIcon) enemyShieldIcon.SetActive(false);
        if (skillButton) skillButton.interactable = false;
        
        // Shake referansı yoksa otomatik bulmaya çalış
        if (cameraShaker == null) cameraShaker = GetComponent<ObjectShake>();
    }

    // Can Barlarını Güncelle
    public void UpdateBattleBars(float currentHP, float maxHP, float enemyHP, float maxEnemyHP)
    {
        float totalHealth = currentHP + enemyHP;
        if (totalHealth > 0)
        {
            float ratio = currentHP / totalHealth;
            battleSlider.value = ratio * 100f; // Slider MaxValue 100 olmalı

            if (playerHealthText) playerHealthText.text = Mathf.FloorToInt(currentHP).ToString();
            if (enemyHealthText) enemyHealthText.text = Mathf.FloorToInt(enemyHP).ToString();
        }
    }

    // Mana Barını Güncelle
    public void UpdateMana(float currentMana, float maxMana)
    {
        if (manaBarFill) manaBarFill.fillAmount = currentMana / maxMana;
        if (skillButton) skillButton.interactable = (currentMana >= maxMana);
    }

    // Kalkan İkonu Kontrolü
    public void SetPlayerShield(bool isActive)
    {
        if (playerShieldIcon) playerShieldIcon.SetActive(isActive);
    }

    // Zamanlayıcıyı Güncelle
    public void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60F);
        int seconds = Mathf.FloorToInt(timeRemaining % 60F);
        if (timerText) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Sarsıntı Efekti
    public void ShakeScreen(float duration, float magnitude)
    {
        if (cameraShaker) cameraShaker.Shake(duration, magnitude);
    }
}