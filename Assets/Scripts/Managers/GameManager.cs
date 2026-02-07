using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Oyun Verileri")]
    public PlayerData playerData;

    [Header("Seçilen Veriler")]
    public CharacterData selectedCharacter;
    
    // --- YENİ EKLENTİ ---
    [Header("Mevcut Durum")]
    public GameMode currentMode = GameMode.Practice; // Varsayılan
    // --------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        // DİKKAT: Artık buradan yükleme yapmıyoruz!
        // playerData = SaveManager.Load();  <-- BU SATIRI SİL VEYA YORUMA AL
        
        // Varsayılan boş bir veri oluştur ki hata vermesin
        if (playerData == null) playerData = new PlayerData();
    }

    // Oyun kapanırken veya alta atılınca kaydet (Telefonda çok önemlidir)
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveManager.Save(playerData);
    }

    private void OnApplicationQuit()
    {
        SaveManager.Save(playerData);
    }

    public void SetCharacter(CharacterData character)
    {
        selectedCharacter = character;
    }
    
    public void SaveGame()
    {
        // Hem yerele (yedek) hem buluta kaydet
        SaveManager.Save(playerData); // İstersen bunu tutabilirsin (Offline yedek)
        
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.SaveData(playerData);
        }
    }
}