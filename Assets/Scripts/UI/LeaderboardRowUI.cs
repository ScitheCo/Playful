using UnityEngine;
using TMPro;

public class LeaderboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;  // 1., 2., 3.
    public TextMeshProUGUI nameText;  // DragonSlayer
    public TextMeshProUGUI scoreText; // 1500 Elo

    public void Setup(int rank, string playerName, int score)
    {
        rankText.text = (rank + 1).ToString() + ".";
        nameText.text = string.IsNullOrEmpty(playerName) ? "Unknown Warrior" : playerName;
        scoreText.text = score.ToString();
        
        // İstersen: İlk 3 kişiyi altın/gümüş/bronz renk yapabilirsin
        if (rank == 0) rankText.color = Color.yellow;
    }
}