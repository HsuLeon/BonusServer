namespace BonusServer.Models
{
    public class BonusRecords
    {
        public List<NotifyData> WinAList { get; set; }
        public List<NotifyData> WinBList { get; set; }
        public List<NotifyData> WinCRList { get; set; }

        public BonusRecords()
        {
            this.WinAList = new List<NotifyData>();
            this.WinBList = new List<NotifyData>();
            this.WinCRList = new List<NotifyData>();
        }

        public BonusRecords Clone()
        {
            BonusRecords newBonusRecords = new BonusRecords();
            //
            newBonusRecords.WinAList = new List<NotifyData>();
            newBonusRecords.WinAList.AddRange(this.WinAList);
            newBonusRecords.WinBList = new List<NotifyData>();
            newBonusRecords.WinBList.AddRange(this.WinBList);
            newBonusRecords.WinCRList = new List<NotifyData>();
            newBonusRecords.WinCRList.AddRange(this.WinCRList);
            return newBonusRecords;
        }
    }
}
