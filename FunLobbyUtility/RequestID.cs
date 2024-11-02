
namespace FunLobbyUtils
{

    // RequestID is for DB or center server processing,
    // so it may not have specified lobby to handle it
    public class RequestID
    {
        public const string IdentifyClient = "IdentifyClient";
        public const string Chat = "Chat";
        public const string GetLoginToken = "GetLoginToken";
        public const string AddLoginInfo = "AddLoginInfo";
        public const string CheckLoginInfo = "CheckLoginInfo";
        public const string MachineInfos = "MachineInfos";
        public const string LoginGM = "LoginGM";
        public const string LogoutGM = "LogoutGM";
        public const string QueryUser = "QueryUser";
        public const string QueryUserByTel = "QueryUserByTel";
        public const string CreateUser = "CreateUser";
        public const string DeleteUser = "DeleteUser";
        public const string QueryGM = "QueryGM";
        public const string CreateGM = "CreateGM";
        public const string DeleteGM = "DeleteGM";
        public const string IncreaseGMCoins = "IncreaseGMCoins";
        public const string SubtractGMCoins = "SubtractGMCoins";
        public const string IncreaseGMTrialCoins = "IncreaseGMTrialCoins";
        public const string SubtractGMTrialCoins = "SubtractGMTrialCoins";
        public const string IncreaseUserCoins = "IncreaseUserCoins";
        public const string SubtractUserCoins = "SubtractUserCoins";
        public const string IncreaseUserTrialCoins = "IncreaseUserTrialCoins";
        public const string SubtractUserTrialCoins = "SubtractUserTrialCoins";
        public const string SaveUser = "SaveUser";
        public const string SaveGM = "SaveGM";
        public const string ChangePassword = "ChangePassword";
        public const string ChangeUserInfo = "ChangeUserInfo";
        public const string GetUserActions = "GetUserActions";
        public const string RecordGMAction = "RecordGMAction";
        public const string GetUnbillingAddSubCoinsRecords = "GetUnbillingAddSubCoinsRecords";
        public const string RecordBill = "RecordBill";
        public const string GetGMActions = "GetGMActions";
        public const string GetBills = "GetBills";
        public const string AddBroadcastMsg = "AddBroadcasrMsg";
        public const string ModifyBroadcastMsg = "ModifyBroadcasrMsg";
        public const string RemoveBroadcastMsg = "RemoveBroadcasrMsg";
        public const string GetValidateUserInfos = "GetValidateUserInfos";
        public const string GetValidateGMInfos = "GetValidateGMInfos";
        public const string UserTransferCoins = "UserTransferCoins";
        public const string GetUserGiftRecords = "GetUserGiftRecords";
        public const string GetUserPlayRecords = "GetUserPlayRecords";
        public const string ExportBillRecords = "ExportBillRecords";
        public const string ExportUniPlayRecords = "ExportUniPlayRecords";
        public const string GetUserBillRecords = "GetUserBillRecords";
        public const string GetGMBillRecords = "GetGMBillRecords";
        public const string GetMachineBillRecords = "GetMachineBillRecords";
        public const string GetMachineSimplifyRecords = "GetMachineSimplifyRecords";
        public const string GetSpecialAccountsInteractRecords = "GetSpecialAccountsInteractRecords";
        public const string GetPlayerCoinRecords = "GetPlayerCoinRecords";
        public const string QueryScore = "QueryScore";
        public const string CanUserTransferUserCoins = "CanUserTransferUserCoins";
        public const string GetRemoteClients = "GetRemoteClients";
        public const string KickoutRemoteClient = "KickoutRemoteClient";
        public const string KickoutMachine = "KickoutMachine";
        public const string KickoutGuoZhao = "KickoutGuoZhao";
        public const string KickoutUniPlay = "KickoutUniPlay";
        public const string TerminateScoring = "TerminateScoring";
        public const string BlockedMachine = "BlockedMachine";
        public const string UnbindMachine = "UnbindMachine";
        public const string GetDefinedKey = "GetDefinedKey";
        public const string SyncBroadcasts = "SyncBroadcasts";
        public const string GetBroadcasts = "GetBroadcasts";
        public const string ExchangeTrialToFormal = "ExchangeTrialToFormal";
        public const string ExchangeFormalToTrial = "ExchangeFormalToTrial";
        public const string ExchangeBetRewardToFormal = "ExchangeBetRewardToFormal";
        public const string ExportExchangeCoins = "ExportExchangeCoins";
        public const string StartUniPlay = "StartUniPlay";
        public const string StopUniPlay = "StopUniPlay";
        public const string CheckConnectState = "CheckConnectState";
        public const string NotifySync = "NotifySync";
        public const string GateServerCloseClient = "GateServerCloseClient";
        public const string GateServerCloseManager = "GateServerCloseManager";
        public const string GateServerNoticeCloseClient = "GateServerNoticeCloseClient";
        public const string GetUnknownOutScore = "GetUnknownOutScore";
        public const string RecordUnknownOutScore = "RecordUnknownOutScore";
        public const string BookMachine = "BookMachine";
        public const string UnbookMachine = "UnbookMachine";
        public const string PayBookMachines = "PayBookMachines";
    }
}