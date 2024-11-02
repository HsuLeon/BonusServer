using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{
    public class MachineStatus
    {
        public string User { get; set; }
        public string UserName { get; set; }
        public string Finalizer { get; set; }
        public int ArduinoId { get { return this.DeviceId; } }
        public string LobbyName { get; set; }
        public int DeviceId { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public string MachineType { get; set; }
        public bool Master { get; set; }
        public bool Spyable { get; set; }
        public bool Maintain { get; set; }

        public bool Blocked { get; set; }
        public bool LobbyUsed { get; set; }
        public bool Broken { get; set; }
        public bool Trial { get; set; }
        public bool GuoZhao { get; set; }
        public int GuoZhaoTimeOut { get; set; }
        public int GuoZhaoIn { get; set; }
        public int GuoZhaoOut { get; set; }
        public int GuoZhaoUnlock { get; set; }
        public int GuoZhaoLeft { get; set; }
        public string GuoZhaoUser { get; set; }
        public string Reserved { get; set; }
        public string TypePic { get; set; }
        public string LockTime { get; set; }

        protected MachineStatus()
        {
        }

        static public MachineStatus FromJson(JObject obj)
        {
            MachineStatus machineStatus = new MachineStatus();

            if (obj.ContainsKey("User")) machineStatus.User = obj["User"].Value<string>();
            if (obj.ContainsKey("UserName")) machineStatus.UserName = obj["UserName"].Value<string>();
            if (obj.ContainsKey("Finalizer")) machineStatus.Finalizer = obj["Finalizer"].Value<string>();
            if (obj.ContainsKey("DeviceId")) machineStatus.DeviceId = obj["DeviceId"].Value<int>();
            if (obj.ContainsKey("MachineId")) machineStatus.MachineId = obj["MachineId"].Value<int>();
            if (obj.ContainsKey("MachineName")) machineStatus.MachineName = obj["MachineName"].Value<string>();
            if (obj.ContainsKey("MachineType")) machineStatus.MachineType = obj["MachineType"].Value<string>();
            if (obj.ContainsKey("Master")) machineStatus.Master = obj["Master"].Value<bool>();
            if (obj.ContainsKey("Spyable")) machineStatus.Spyable = obj["Spyable"].Value<bool>();
            if (obj.ContainsKey("Maintain")) machineStatus.Maintain = obj["Maintain"].Value<bool>();
            if (obj.ContainsKey("Blocked")) machineStatus.Blocked = obj["Blocked"].Value<bool>();
            if (obj.ContainsKey("LobbyUsed")) machineStatus.LobbyUsed = obj["LobbyUsed"].Value<bool>();
            if (obj.ContainsKey("Broken")) machineStatus.Broken = obj["Broken"].Value<bool>();
            if (obj.ContainsKey("Trial")) machineStatus.Trial = obj["Trial"].Value<bool>();
            if (obj.ContainsKey("GuoZhao")) machineStatus.GuoZhao = obj["GuoZhao"].Value<bool>();
            if (obj.ContainsKey("GuoZhaoTimeOut")) machineStatus.GuoZhaoTimeOut = obj["GuoZhaoTimeOut"].Value<int>();
            if (obj.ContainsKey("GuoZhaoIn")) machineStatus.GuoZhaoIn = obj["GuoZhaoIn"].Value<int>();
            if (obj.ContainsKey("GuoZhaoOut")) machineStatus.GuoZhaoOut = obj["GuoZhaoOut"].Value<int>();
            if (obj.ContainsKey("GuoZhaoUnlock")) machineStatus.GuoZhaoUnlock = obj["GuoZhaoUnlock"].Value<int>();
            if (obj.ContainsKey("GuoZhaoLeft")) machineStatus.GuoZhaoLeft = obj["GuoZhaoLeft"].Value<int>();
            if (obj.ContainsKey("GuoZhaoUser")) machineStatus.GuoZhaoUser = obj["GuoZhaoUser"].Value<string>();
            if (obj.ContainsKey("Reserved")) machineStatus.Reserved = obj["Reserved"].Value<string>();
            if (obj.ContainsKey("TypePic")) machineStatus.TypePic = obj["TypePic"].Value<string>();
            if (obj.ContainsKey("LockTime")) machineStatus.LockTime = obj["LockTime"].Value<string>();

            return machineStatus;
        }

        static public JObject ToJson(MachineStatus machineStatus)
        {
            JObject objStatus = new JObject();
            objStatus["User"] = machineStatus.User;
            objStatus["UserName"] = machineStatus.UserName;
            objStatus["Finalizer"] = machineStatus.Finalizer;
            objStatus["DeviceId"] = machineStatus.DeviceId;
            objStatus["MachineId"] = machineStatus.MachineId;
            objStatus["MachineName"] = machineStatus.MachineName;
            objStatus["MachineType"] = machineStatus.MachineType;
            objStatus["Master"] = machineStatus.Master;
            objStatus["Spyable"] = machineStatus.Spyable;
            objStatus["Maintain"] = machineStatus.Maintain;
            objStatus["Blocked"] = machineStatus.Blocked;
            objStatus["LobbyUsed"] = machineStatus.LobbyUsed;
            objStatus["Broken"] = machineStatus.Broken;
            objStatus["Trial"] = machineStatus.Trial;
            objStatus["GuoZhao"] = machineStatus.GuoZhao;
            objStatus["GuoZhaoTimeOut"] = machineStatus.GuoZhaoTimeOut;
            objStatus["GuoZhaoIn"] = machineStatus.GuoZhaoIn;
            objStatus["GuoZhaoOut"] = machineStatus.GuoZhaoOut;
            objStatus["GuoZhaoUnlock"] = machineStatus.GuoZhaoUnlock;
            objStatus["GuoZhaoLeft"] = machineStatus.GuoZhaoLeft;
            objStatus["GuoZhaoUser"] = machineStatus.GuoZhaoUser;
            objStatus["Reserved"] = machineStatus.Reserved;
            objStatus["TypePic"] = machineStatus.TypePic;
            objStatus["LockTime"] = machineStatus.LockTime;
            return objStatus;
        }

        public bool Equals(MachineStatus status)
        {
            if (status == null)
                return false;

            if (this.User == status.User &&
                this.UserName == status.UserName &&
                this.Finalizer == status.UserName &&
                this.DeviceId == status.DeviceId &&
                this.MachineId == status.MachineId &&
                this.MachineName == status.MachineName &&
                this.MachineType == status.MachineType &&
                this.Master == status.Master &&
                this.Spyable == status.Spyable &&
                this.Maintain == status.Maintain &&
                this.Blocked == status.Blocked &&
                this.LobbyUsed == status.LobbyUsed &&
                this.Broken == status.Broken &&
                this.Trial == status.Trial &&
                this.GuoZhao == status.GuoZhao &&
                this.GuoZhaoTimeOut == status.GuoZhaoTimeOut &&
                this.GuoZhaoIn == status.GuoZhaoIn &&
                this.GuoZhaoOut == status.GuoZhaoOut &&
                this.GuoZhaoUnlock == status.GuoZhaoUnlock &&
                this.GuoZhaoLeft == status.GuoZhaoLeft &&
                this.GuoZhaoUser == status.GuoZhaoUser &&
                this.Reserved == status.Reserved &&
                this.TypePic == status.TypePic &&
                this.LockTime == status.LockTime)
                return true;
            else
                return false;
        }
    }
}
