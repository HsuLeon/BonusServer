
export default class BonusRecords
{
    WinAList;
    WinBList;
    WinCRList;

    constructor()
    {
        this.WinAList = [];
        this.WinBList = [];
        this.WinCRList = [];
    }

    Clone()
    {
        const newBonusRecords = new BonusRecords();
        //
        const contentA = JSON.stringify(this.this.WinAList);
        newBonusRecords.WinAList = JSON.parse(contentA);
        const contentB = JSON.stringify(this.WinBList);
        newBonusRecords.WinBList = JSON.parse(contentB);
        const contentCR = JSON.stringify(this.WinCRList);
        newBonusRecords.WinCRList = JSON.parse(contentCR);
        return newBonusRecords;
    }
}