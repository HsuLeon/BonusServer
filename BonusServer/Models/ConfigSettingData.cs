namespace BonusServer.Models
{
    public class ConfigSettingData
    {
        public string? BonusServerDomain { get; set; }
        public int? BonusServerPort { get; set; }
        public string? BonusServerPassword { get; set; }
        public string? UpperDomain { get; set; }
        public List<ConfigSetting.BonusLogin>? SubDomains { get; set; }
        public string? APITransferPoints { get; set; }
        public float? CollectSubScale { get; set; }
        public string? RabbitMQServer { get; set; }
        public string? RabbitMQUserName { get; set; }
        public string? RabbitMQPassword { get; set; }
        public string? ConditionWinA { get; set; }
        public string? ConditionWinB { get; set; }
        public string? ConditionWinCR { get; set; }
    }
}
