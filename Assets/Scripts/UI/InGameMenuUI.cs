using UnityEngine;
using UnityEngine.UI;

public class InGameMenuUI : BasePanel
{
    [Header("Butonlar")]
    public Button resumeButton;    // Oyuna dön
    public Button settingsButton;  // Ayarları aç
    public Button surrenderButton; // Teslim Ol
    
    // Bu panel açıldığında oyun durmaz!
}