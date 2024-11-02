
namespace FunLobbyUtils
{
    public class MainSettings
    {
        public class GuoZhaoPeriod
        {
            public int Begin { get; set; }
            public int Duration { get; set; }

            public GuoZhaoPeriod(int beginInMinute, int durationInMinute)
            {
                this.Begin = beginInMinute;
                this.Duration = durationInMinute;
            }
        }
        public class UserLevel
        {
            public string level { get; set; }
            public int maxPay { get; set; }
            public int bonusRate { get; set; }

            public UserLevel()
            {
                level = "";
                maxPay = 0;
                bonusRate = 0;
            }
        }

        public enum Award_Type
        {
            None = 0,
            TrialCoins = 1,
            Coins = 2,
        }

        public string Domain { get; set; }
        public float ScoreRatioToNTD { get; set; }
        public bool MultiLogin { get; set; }
        public ulong UserNoHeartbeatAckKickout { get; set; }
        public ulong FinalizeTimeOut { get; set; }
        public int MaxDailyReceivedGolds { get; set; }
        public int PeriodMaxGuoZhaoCnt { get; set; }
        public List<GuoZhaoPeriod> GuoZhaoPeriods { get; set; }
        public int InScoreBonusBase { get; set; }
        public int InScoreBonusFormal { get; set; }
        public int InScoreBonusTrial { get; set; }
        public bool CanExchangeTrialToFormal { get; set; }
        public bool CanExchangeFormalToTrial { get; set; }
        public int ExchangeRatio { get; set; }
        public int ExchangeMinLeft { get; set; }
        public int ExchangeReceiverRate { get; set; }
        public int GiftTaxRate { get; set; }
        public int DailyAward { get; set; }
        public Award_Type AwardType { get; set; }
        public int MaxDailyAward { get; set; }
        public int AwardDuration { get; set; }
        public int StopAwardAt { get; set; }
        public int MaxPlayerScore { get; set; }
        public bool VerifyUserInfo { get; set; }
        public bool MachineStopGuoZhaoTEMP { get; set; }
        public int WainZhuMinCnt { get; set; }
        public int WainZhuMaxCnt { get; set; }
        public int MinExchangeBetReward { get; set; }
        public int MaxExchangeBetReward { get; set; }
        public bool EnableUserInfoSortIndex { get; set; }
        public bool ShowBetReward { get; set; }
        public bool ShowPlayerLevel { get; set; }
        public Dictionary<string, MainSettings.UserLevel> UserLevels { get; set; }

        public MainSettings()
        {

        }

        public bool AddGuoZhaoPeriod(int beginInMinute, int durationInMinute)
        {
            const int maxDurationInMinute = 24 * 60;

            beginInMinute = beginInMinute > 0 ? beginInMinute : 0;
            beginInMinute = beginInMinute < maxDurationInMinute ? beginInMinute : maxDurationInMinute;
            beginInMinute = beginInMinute % maxDurationInMinute;

            durationInMinute = durationInMinute > 0 ? durationInMinute : 0;
            durationInMinute = durationInMinute < maxDurationInMinute ? durationInMinute : maxDurationInMinute;

            List<GuoZhaoPeriod> list = this.GuoZhaoPeriods;
            for (int i = 0; i < list.Count; i++)
            {
                int offset = beginInMinute - list[i].Begin;
                if (durationInMinute - list[i].Begin > offset)
                    return false;
            }
            list.Add(new GuoZhaoPeriod(beginInMinute, durationInMinute));
            this.GuoZhaoPeriods = list;
            return true;
        }

        public void RemoveGuoZhaoPeriod(int beginInMinute, int durationInMinute)
        {
            const int maxDurationInMinute = 24 * 60;

            beginInMinute = beginInMinute > 0 ? beginInMinute : 0;
            beginInMinute = beginInMinute < maxDurationInMinute ? beginInMinute : maxDurationInMinute;
            beginInMinute = beginInMinute % maxDurationInMinute;

            durationInMinute = durationInMinute > 0 ? durationInMinute : 0;
            durationInMinute = durationInMinute < maxDurationInMinute ? durationInMinute : maxDurationInMinute;
            //durationInMinute = durationInMinute % maxDurationInMinute;

            List<GuoZhaoPeriod> list = this.GuoZhaoPeriods;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Begin >= beginInMinute &&
                    list[i].Begin < beginInMinute + durationInMinute)
                {
                    list.RemoveAt(i);
                }
            }
            this.GuoZhaoPeriods = list;
        }
    }
}
