using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileUI : BasePanel
{
    [Header("Input")]
    public TMP_InputField nameInput;
    public Button saveButton;
    public Button closeButton;
    
    [Header("Feedback")]
    public TextMeshProUGUI statusText; // "Kaydedildi" veya "Hata" mesajı

    public override void Init()
    {
        base.Init();
        saveButton.onClick.AddListener(SubmitName);
        closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        // Panel açılınca mevcut ismi input'a yaz
        if (PlayFabManager.Instance != null)
        {
            nameInput.text = PlayFabManager.Instance.displayName;
        }
        statusText.text = "";
    }

    void SubmitName()
    {
        string newName = nameInput.text;
        
        if (newName.Length < 3)
        {
            statusText.text = "İsim çok kısa!";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Kaydediliyor...";
        saveButton.interactable = false; // Çift tıklamayı önle

        PlayFabManager.Instance.SubmitName(newName, 
            () => 
            {
                statusText.text = "İsim Değiştirildi!";
                statusText.color = Color.green;
                saveButton.interactable = true;
                // İstersen paneli kapatabilirsin: Hide();
            },
            (error) => 
            {
                statusText.text = "Hata: " + error; // Örn: İsim alınmışsa
                statusText.color = Color.red;
                saveButton.interactable = true;
            }
        );
    }
}