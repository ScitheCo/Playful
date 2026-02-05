using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRankData", menuName = "Playful/Rank Data")]
public class RankData : ScriptableObject
{
    [System.Serializable]
    public struct RankTier
    {
        public string rankName;  // Bronze, Silver...
        public int minElo;       // 0, 1200, 1500...
        public Sprite rankIcon;  // Rütbe ikonu
    }

    public List<RankTier> ranks;

    // Elo puanına göre hangi rütbede olduğunu bulur
    public RankTier GetRankByElo(int elo)
    {
        // En yüksek puandan aşağı doğru tara
        for (int i = ranks.Count - 1; i >= 0; i--)
        {
            if (elo >= ranks[i].minElo)
                return ranks[i];
        }
        return ranks[0]; // Hiçbiri uymazsa en düşük rütbe
    }

    // Bir sonraki rütbeyi bulur (Slider hesaplamak için)
    public RankTier GetNextRank(int elo)
    {
        for (int i = 0; i < ranks.Count; i++)
        {
            if (ranks[i].minElo > elo)
                return ranks[i];
        }
        return ranks[ranks.Count - 1]; // Zaten son rütbedeyse aynısını dön
    }
}