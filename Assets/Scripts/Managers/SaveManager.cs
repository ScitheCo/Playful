using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string saveFileName = "savefile.json";

    public static void Save(PlayerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            File.WriteAllText(path, json);
            // Debug.Log("Oyun Kaydedildi: " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Kayıt Hatası: " + e.Message);
        }
    }

    public static PlayerData Load()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Yükleme Hatası (Dosya bozuk olabilir): " + e.Message);
                return new PlayerData(); // Hata varsa yeni dosya aç
            }
        }
        else
        {
            Debug.Log("Kayıt dosyası bulunamadı, yeni oluşturuluyor.");
            return new PlayerData(); // Dosya yoksa sıfırdan oluştur
        }
    }
}