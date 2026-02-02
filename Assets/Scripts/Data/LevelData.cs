using UnityEngine;
using System.Collections.Generic;

// Sağ tık menüsüne "Create Level" ekler
[CreateAssetMenu(fileName = "NewLevel", menuName = "Playful/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 7;
    public int height = 8;

    // Tek boyutlu dizi (Inspector'da serileştirmesi kolaydır)
    // true = Oynanabilir Alan, false = Ölü Alan (Duvar)
    [HideInInspector] 
    public bool[] activeSlots; 

    // Veriyi güvenli şekilde başlatma
    public void Initialize()
    {
        if (activeSlots == null || activeSlots.Length != width * height)
        {
            activeSlots = new bool[width * height];
            for (int i = 0; i < activeSlots.Length; i++) activeSlots[i] = true; // Hepsi açık başlasın
        }
    }
}