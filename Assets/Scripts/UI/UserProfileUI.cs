using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UserProfileUI : MonoBehaviour
{
    [Header("UI Referansları")]
    public TMP_InputField nameInputField;
    
    // Önizleme resimleri SİLİNDİ.

    [Header("Grid Ayarları")]
    public Transform avatarGridContent; 
    public Transform frameGridContent;
    
    [Header("Prefablar")]
    public GameObject avatarGroupPrefab; // Senin yaptığın 1. Prefab (Avatar Group)
    public GameObject frameGroupPrefab;  // Senin yaptığın 2. Prefab (Frame Group)

    // --- BUTON LİSTELERİ ---
    private List<GameObject> avatarButtons = new List<GameObject>();
    private List<GameObject> frameButtons = new List<GameObject>();

    // Geçici Seçimler
    private int selectedAvatarId;
    private int selectedFrameId;

    private void OnEnable()
    {
        LoadCurrentValues();
        GenerateGrids();
    }

    void LoadCurrentValues()
    {
        PlayerData data = GameManager.Instance.playerData;
        nameInputField.text = data.username;
        selectedAvatarId = data.avatarId;
        selectedFrameId = data.frameId;
    }

    void GenerateGrids()
    {
        // 1. Öncekileri Temizle
        foreach (Transform child in avatarGridContent) Destroy(child.gameObject);
        foreach (Transform child in frameGridContent) Destroy(child.gameObject);
        
        avatarButtons.Clear();
        frameButtons.Clear();

        // 2. AVATARLARI DİZ (AvatarGroup Prefabı Kullanarak)
        if (GameManager.Instance.avatarList != null)
        {
            for (int i = 0; i < GameManager.Instance.avatarList.Count; i++)
            {
                int index = i; 
                GameObject btnObj = Instantiate(avatarGroupPrefab, avatarGridContent);
                
                // Prefabın ana Image bileşenine avatar resmini koyuyoruz
                ItemGroup itemGroup = btnObj.GetComponent<ItemGroup>();
                if (itemGroup != null) itemGroup.SetImageSprite(GameManager.Instance.avatarList[i]);
                
                // Tıklama Olayı
                btnObj.GetComponent<Button>().onClick.AddListener(() => {
                    OnAvatarSelected(index);
                });

                avatarButtons.Add(btnObj);
            }
        }

        // 3. ÇERÇEVELERİ DİZ (FrameGroup Prefabı Kullanarak)
        if (GameManager.Instance.frameList != null)
        {
            for (int i = 0; i < GameManager.Instance.frameList.Count; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(frameGroupPrefab, frameGridContent);
                
                // Prefabın ana Image bileşenine çerçeve resmini koyuyoruz
                ItemGroup itemGroup = btnObj.GetComponent<ItemGroup>();
                if (itemGroup != null) itemGroup.SetImageSprite(GameManager.Instance.frameList[i]);
                
                // Tıklama Olayı
                btnObj.GetComponent<Button>().onClick.AddListener(() => {
                    OnFrameSelected(index);
                });

                frameButtons.Add(btnObj);
            }
        }

        // 4. Listeler oluşur oluşmaz ışıkları yak
        HighlightSelectedItems();
    }

    // --- SEÇİM VE IŞIK MANTIĞI ---

    void OnAvatarSelected(int index)
    {
        selectedAvatarId = index;
        HighlightSelectedItems(); // Sadece ışıkları güncelle
    }

    void OnFrameSelected(int index)
    {
        selectedFrameId = index;
        HighlightSelectedItems(); // Sadece ışıkları güncelle
    }

    void HighlightSelectedItems()
    {
        // Avatar Butonlarını Gez
        for (int i = 0; i < avatarButtons.Count; i++)
        {
            // Prefabın içindeki "SelectionBorder" isimli objeyi bul
            ItemGroup border = avatarButtons[i].GetComponent<ItemGroup>();
            
            if (border != null)
            {
                // Eğer bu butonun sırası (i), seçili ID'ye eşitse -> AÇ, değilse -> KAPAT
                bool isSelected = (i == selectedAvatarId);
                border.SetSelectionBorder(isSelected);
            }
        }

        // Çerçeve Butonlarını Gez
        for (int i = 0; i < frameButtons.Count; i++)
        {
            // Prefabın içindeki "SelectionBorder" isimli objeyi bul
            ItemGroup border = frameButtons[i].GetComponent<ItemGroup>();
            
            if (border != null)
            {
                // Eğer bu butonun sırası (i), seçili ID'ye eşitse -> AÇ, değilse -> KAPAT
                bool isSelected = (i == selectedFrameId);
                border.SetSelectionBorder(isSelected);
            }
        }
    }

    // --- KAYDETME ---

    public void OnSaveButtonClicked()
    {
        string newName = nameInputField.text;
        
        // 1. PlayFab İsim Güncelleme
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.SubmitName(newName, 
            () => { Debug.Log("İsim PlayFab'da güncellendi"); }, 
            (error) => { Debug.LogError("İsim hatası: " + error); });
        }

        // 2. Yerel Veriyi Güncelle
        GameManager.Instance.playerData.username = newName;
        GameManager.Instance.playerData.avatarId = selectedAvatarId;
        GameManager.Instance.playerData.frameId = selectedFrameId;

        // 3. Kaydet
        GameManager.Instance.SaveGame();
        MainMenuManager.Instance.UpdateAllPanels();
        gameObject.SetActive(false);
    }
    
    public void OnCloseButtonClicked()
    {
        gameObject.SetActive(false); 
    }
}