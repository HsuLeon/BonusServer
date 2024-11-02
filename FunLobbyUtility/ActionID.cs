using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{
    // ActionId is for Machine controlling,
    // the cmd should be for specified lobby
    public class ActionID
    {
        public const string LockMachine = "LockMachine";
        public const string UnlockMachine = "UnlockMachine";
        public const string LockSpyMachine = "LockSpyMachine";
        public const string UnlockSpyMachine = "UnlockSpyMachine";
        public const string GameBtnDown = "GameBtnDown";
        public const string GameBtnUp = "GameBtnUp";
        public const string KickoutClient = "KickoutClient";
        public const string InScore = "InScore";
        public const string OutScore = "OutScore";
        public const string InTrialScore = "InTrialScore";
        public const string OutTrialScore = "OutTrialScore";
        public const string InGuoZhaoScore = "InGuoZhaoScore";
        public const string OutGuoZhaoScore = "OutGuoZhaoScore";
        public const string Gameplay = "Gameplay";

        public const string None = "None";

        public string Action { get; protected set; }
        public int DeviceId { get; protected set; }
        public int MachineId { get; protected set; }
        public JObject Data { get; set; }

        public static ActionID Create(string action, int deviceId, int machineId)
        {
            ActionID actionId = new ActionID();
            actionId.Action = action;
            actionId.DeviceId = deviceId;
            actionId.MachineId = machineId;
            return actionId;
        }

        protected ActionID()
        {
            this.Action = ActionID.None;
            this.DeviceId = -1;
            this.MachineId = -1;
            this.Data = new JObject();
        }
    }
}
