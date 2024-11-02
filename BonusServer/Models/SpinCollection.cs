using Newtonsoft.Json.Linq;
using FunLobbyUtils;

namespace BonusServer.Models
{
    public class CSpinCollection
    {
        public enum BETWIN_TYPE
        {
            Others = 0,
            Slot = 1,
            Cr = 2,
            ChinaCr = 3
        }

        public class CSpinData
        {
            public int TotalBet { get; set; }
            public int TotalWin { get; set; }
            public int WinA { get; set; }
            public int WinB { get; set; }

            public CSpinData()
            {
                this.TotalBet = 0;
                this.TotalWin = 0;
                this.WinA = 0;
                this.WinB = 0;
            }

            public static CSpinData FromJson(JObject obj)
            {
                CSpinData spinData = new CSpinData();
                spinData.TotalBet = obj.ContainsKey("TotalBet") ? obj.Value<int>("TotalBet") : 0;
                spinData.TotalWin = obj.ContainsKey("TotalWin") ? obj.Value<int>("TotalWin") : 0;
                spinData.WinA = obj.ContainsKey("WinA") ? obj.Value<int>("WinA") : 0;
                spinData.WinB = obj.ContainsKey("WinB") ? obj.Value<int>("WinB") : 0;
                return spinData;
            }
        }
        public string? UserAccount { get; set; }
        public BETWIN_TYPE BonusType { get; set; }
        public CSpinData? SpinData { get; set; }
        public long SyncTime { get; set; }
        public bool AbleToRushBonus { get; set; }

        public CSpinCollection()
        {
            this.UserAccount = null;
            this.BonusType = BETWIN_TYPE.Others;
            this.SpinData = null;
            this.SyncTime = 0;
            this.AbleToRushBonus = false;
        }

        public static CSpinCollection? FromJson(JObject obj)
        {
            CSpinCollection? spinCollection = null;
            try
            {
                if (obj.ContainsKey("SpinData") == false) throw new Exception("null SpinData");
                string? strContent = obj.Value<string>("SpinData");
                if (strContent == null) throw new Exception("null strContent");
                JObject objSpinData = JObject.Parse(strContent);

                spinCollection = new CSpinCollection();
                spinCollection.UserAccount = obj.ContainsKey("UserAccount") ? obj.Value<string>("UserAccount") : "";
                spinCollection.BonusType = obj.ContainsKey("BonusType") ? (BETWIN_TYPE)obj.Value<int>("BonusType") : BETWIN_TYPE.Others;
                spinCollection.SpinData = CSpinData.FromJson(objSpinData);
                spinCollection.SyncTime = obj.ContainsKey("SyncTime") ? obj.Value<long>("SyncTime") : 0;
                spinCollection.AbleToRushBonus = obj.ContainsKey("AbleToRushBonus") ? obj.Value<bool>("AbleToRushBonus") : false;
            }
            catch(Exception ex)
            {
                Log.StoreMsg(ex.Message);
            }
            return spinCollection;
        }
    }
}
