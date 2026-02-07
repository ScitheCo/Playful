using UnityEngine;
using UnityEngine.UI;

public class GameModeUI : BasePanel
{
    [Header("Mod Butonları")]
    public Button rankedButton;
    public Button casualButton;
    public Button practiceButton;
    // FriendMatch butonu Arkadaş Listesi pencresinde olacak

    [Header("Diğer")]
    public Button closeButton; // Paneli kapatır

    public override void Init()
    {
        base.Init();
    }
}