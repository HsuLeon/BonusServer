namespace BonusServer.Models
{
    public class SpinData
    {
        public string? Domain { get; set; }
        public int Port { get; set; }
        public string? WebSite { get; set; }
        public string? MachineName { get; set; }
        public string? UserAccount { get; set; }
        public int Win { get; set; }
        public long UtcTicks { get; set; }

        public SpinData()
        {

        }
    }
}
