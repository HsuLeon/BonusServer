namespace BonusServer.Models
{
    public class NotifyData
    {
        public enum WinSpin
        {
            None,
            A,
            B,
            CR
        };

        public WinSpin WinSpinType { get; set; }
        public string WebSite { get; set; }
        public string Message {  get; set; }
        public int Scores { get; set; }
        public string MachineName { get; set; }
        public string UserAccount { get; set; }
        public DateTime CreateTime { get; set; }
        public int LifeInMinutes { get; set; }

        public NotifyData()
        {
            this.WinSpinType = WinSpin.None;
            this.WebSite = "";
            this.Message = "";
            this.Scores = 0;
            this.MachineName = "";
            this.UserAccount = "";
            this.CreateTime = DateTime.Now;
        }
    }
}
