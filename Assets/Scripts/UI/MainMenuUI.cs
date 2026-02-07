using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bu script SADECE Karakter Seçim/Satın Alma Panelini yönetir.
public class MainMenuUI : BasePanel
{
    [Header("Navigasyon")]
    public Button backButton; // Lobby'e dönen buton (<)
    public Button nextButton; // Sonraki Karakter (>)
    public Button prevButton; // Önceki Karakter (<)

    [Header("Karakter Bilgileri")]
    public Image characterAvatar;       // Ortadaki büyük resim
    public TextMeshProUGUI nameText;    // "Warrior"
    public TextMeshProUGUI classText;   // "Melee / Tank"
    public TextMeshProUGUI descText;    // Yetenek açıklaması

    [Header("İstatistik Barları")]
    public Slider healthBar;
    public Slider manaBar;
    public Slider damageBar;

    [Header("Aksiyon Butonları")]
    public Button equipButton;          // "SEÇ" veya "SEÇİLDİ" butonu
    public TextMeshProUGUI equipText;   // Butonun üzerindeki yazı
    
    [Header("Satın Alma Paneli")]
    public GameObject buyPanel;         // Kilitliyse açılacak panel
    public Button buyGoldButton;        // Altınla Al
    public TextMeshProUGUI goldPriceText;
    public Button buyGemButton;         // Elmasla Al
    public TextMeshProUGUI gemPriceText;

    [Header("Oyuncu Cüzdanı (Referans)")]
    public TextMeshProUGUI playerGoldText;
    public TextMeshProUGUI playerGemText;

    public override void Init()
    {
        base.Init();
        // Buton dinleyicilerini Manager atayacak
    }

    // Ekranı Verilerle Doldur
    public void UpdateVisuals(CharacterData charData, PlayerData playerData, bool isUnlocked, bool isEquipped)
    {
        // 1. Temel Bilgiler
        characterAvatar.sprite = charData.avatar;
        nameText.text = charData.characterName;
        classText.text = charData.className;
        
        if (charData.activeSkill != null)
            descText.text = $"<color=yellow>{charData.activeSkill.skillName}:</color> {charData.activeSkill.description}";
        else
            descText.text = "Özel yetenek yok.";

        // 2. İstatistikler (Değerleri normalize et)
        healthBar.value = charData.maxHealth / 2000f; 
        manaBar.value = charData.maxMana / 200f;
        damageBar.value = charData.baseAttackDamage / 100f;

        // 3. Cüzdanı Güncelle
        playerGoldText.text = playerData.gold.ToString();
        playerGemText.text = playerData.gems.ToString();

        // 4. Kilit / Satın Alma Mantığı
        if (isUnlocked)
        {
            buyPanel.SetActive(false);
            equipButton.gameObject.SetActive(true);

            if (isEquipped)
            {
                equipButton.interactable = false; // Zaten seçili, tekrar basılamasın
                equipText.text = "SEÇİLDİ";
                equipText.color = Color.green;
            }
            else
            {
                equipButton.interactable = true;
                equipText.text = "SEÇ";
                equipText.color = Color.white;
            }
        }
        else
        {
            // Karakter Kilitli
            equipButton.gameObject.SetActive(false);
            buyPanel.SetActive(true);

            goldPriceText.text = charData.goldPrice.ToString();
            gemPriceText.text = charData.gemPrice.ToString();

            // Paran yetmiyorsa buton sönük olsun
            buyGoldButton.interactable = (playerData.gold >= charData.goldPrice);
            buyGemButton.interactable = (playerData.gems >= charData.gemPrice);
        }
    }
}