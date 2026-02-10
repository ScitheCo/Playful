using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public string username = "NewPlayer";
    
    public int avatarId = 0; // Seçili avatarın ID'si (List sırası)
    public int frameId = 0;  // Seçili çerçevenin ID'si
    
    // Ekonomi
    public int gold = 1000;
    public int gems = 10;
    
    // Ayarlar
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;
    
    // İlerleme
    public int elo = 1200;
    public int level = 1;
    public float currentXP = 0;
    public float requiredXP = 100;
    
    // Karakterler
    public List<string> unlockedCharacters = new List<string>() { "Warrior" }; // Varsayılan açık karakter
    public string lastSelectedCharacterName = "Warrior"; // Varsayılan seçili

    public PlayerData()
    {
        // Constructor: İlk açılış değerleri
        username = "NewPlayer";
        gold = 1000;
        gems = 10;
        elo = 1200;
        level = 1;
        unlockedCharacters = new List<string>() { "Warrior" };
        lastSelectedCharacterName = unlockedCharacters[0];
    }
}