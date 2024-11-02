
export default class CollectData
{
    TotalBet;
    TotalWin;
    WinA;
    WinB;
    BonusType;

    constructor(
        totalBet,
        totalWin,
        winA,
        winB,
        bonusType)
    {
        this.TotalBet = totalBet ? parseInt(totalBet) : 0;
        this.TotalWin = totalWin ? parseInt(totalWin) : 0;
        this.WinA = winA ? parseInt(winA) : 0;
        this.WinB = winB ? parseInt(winB) : 0;
        this.BonusType = bonusType ? parseInt(bonusType) : 0;
    }

    Clone()
    {
        const newData = new CollectData(
            this.TotalBet,
            this.TotalWin,
            this.WinA,
            this.WinB,
            this.BonusType
        );
        return newData;
    }

    Reset()
    {
        this.TotalBet = 0;
        this.TotalWin = 0;
        this.WinA = 0;
        this.WinB = 0;
    }
}