using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Standart değişkenleri çiz (width, height vb.)
        DrawDefaultInspector();

        LevelData level = (LevelData)target;

        // Boyutlar değişirse diziyi yeniden oluştur
        if (GUILayout.Button("Izgarayı Oluştur / Sıfırla"))
        {
            level.Initialize();
        }

        // Dizi yoksa çizme
        if (level.activeSlots == null || level.activeSlots.Length == 0) return;

        GUILayout.Space(10);
        GUILayout.Label("Harita Tasarımı (Kırmızı = Duvar, Yeşil = Açık)", EditorStyles.boldLabel);

        // Grid Çizimi
        for (int y = level.height - 1; y >= 0; y--) // Unity UI aşağıdan yukarı çizer, ters çevirdik
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < level.width; x++)
            {
                int index = x + y * level.width;
                if (index >= level.activeSlots.Length) break;

                // Buton Rengi Ayarlama
                bool isActive = level.activeSlots[index];
                GUI.backgroundColor = isActive ? Color.green : Color.red;

                // Butonun kendisi
                if (GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(30)))
                {
                    // Tıklayınca durumu tersine çevir (Açık <-> Kapalı)
                    level.activeSlots[index] = !isActive;
                    EditorUtility.SetDirty(level); // Kaydetmeyi unutma
                }
            }
            GUILayout.EndHorizontal();
        }
        
        // Rengi normale döndür
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        GUILayout.Label("Not: Değişiklikler otomatik kaydolur.", EditorStyles.miniLabel);
    }
}