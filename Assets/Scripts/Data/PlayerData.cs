using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public string username = "Player";
    public int gold = 0;
    public int gems = 0;
    
    // --- AYARLAR (YENİ) ---
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;
    
    // İlerleme Verileri
    public int elo = 1200;
    public int level = 1;
    public float currentXP = 0;
    public float requiredXP = 100;

    // Sahip Olunan Karakterler (İsimleri tutacağız)
    public List<string> unlockedCharacters = new List<string>() { "Warrior" };
    
    public string lastSelectedCharacterName = "";

    // Constructor (İlk kez oyun açıldığında varsayılan değerler)
    public PlayerData()
    {
        username = "NewPlayer";
        gold = 1000; // Başlangıç hediyesi
        gems = 10;
        elo = 1200;
        level = 1;
        currentXP = 0;
        requiredXP = 100;
        unlockedCharacters = new List<string>() { "Warrior" };
        lastSelectedCharacterName = unlockedCharacters[0];
    }
}