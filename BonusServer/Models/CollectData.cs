using FunLobbyUtils;

namespace BonusServer.Models
{
    public class CollectData
    {
        public float TotalBet { get; set; }
        public float TotalWin { get; set; }
        public float WinA { get; set; }
        public float WinB { get; set; }
        public int BonusType { get; set; }

        public CollectData(
            float totalBet = 0,
            float totalWin = 0,
            float winA = 0,
            float winB = 0,
            int bonusType = 0)
        {
            this.TotalBet = totalBet;
            this.TotalWin = totalWin;
            this.WinA = winA;
            this.WinB = winB;
            this.BonusType = bonusType;
        }

        public CollectData Clone()
        {
            CollectData newData = new CollectData(
                this.TotalBet,
                this.TotalWin,
                this.WinA,
                this.WinB,
                this.BonusType
            );
            return newData;
        }

        public void Reset()
        {
            this.TotalBet = 0;
            this.TotalWin = 0;
            this.WinA = 0;
            this.WinB = 0;
        }
    }
}
