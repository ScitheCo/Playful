// Oyun Modları
public enum GameMode
{
    Practice,   // Botlara karşı alıştırma (Elo değişmez)
    Casual,     // İnsanlara karşı eğlence (Elo değişmez, ödül az)
    Ranked,     // Rekabetçi (Elo değişir, ödül çok)
    FriendMatch // Arkadaşla özel oda (Elo değişmez)
}