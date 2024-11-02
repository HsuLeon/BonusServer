using Newtonsoft.Json.Linq;
using FunLobbyUtils.Database.Schema;

namespace FunLobbyUtils.Database
{
    public delegate void UpdateHandler<T>(T data);

    public enum SCORE_TYPE
    {
        None = -1,
        Trial = 0,
        Formal = 1,
        GuoZhao = 2,
        TrialGuoZhao = 3
    };

    public interface WinLobbyDB
    {
        void Dispose();

        string DBType();
        string Domain();
        int Port();
        AgentInfo Agent();
        string AgentAccount();
        DateTime GetTimeFromServer();
        AgentInfo QueryAgent();
        JObject SaveAgent(AgentInfo agentInfo);

        JObject CreateUser(string gm, string account, string password, string nickName, string phoneNo, DateTime birthday, string note, bool bGiftEnabled, int maxDailyReceivedGolds,string avatarInfo);
        //JObject SaveUser(UserInfo newInfo);
        string UpdateUser(string account, JObject objParams);
        JObject VerifyUser(string token);
        UserInfo QueryUser(string? account, string? id, string? token);
        UserInfo QueryUserByTel(string PhoneNo);
        JObject AwardUser(string account, string reason, string awardType, int award);
        List<UserAward> QueryAwards(string targetUser, List<string> awardTypes, DateTime sTime, DateTime eTime);
        List<UserInfo> GetValidateUserInfos(string gmAccount);
        string AddLoginInfo(string agentId, string appId, string token, string lobbyName);
        JObject CheckLoginInfo(string agentId, string appId);
        string UpdateUserScores(string account, JObject objScores);
        string RecordUserAction(string userId, string lobby, string machine, string action, string note);
        List<UserAction> GetUserActions(string account, List<string> actionFilter, int dataCount);
        List<UserAction> GetUserActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime);
        string UserSendGift(string account, string target, JObject objGift);
        List<GiftInfo> GetUserGiftRecords(string account, string target, DateTime sTime, DateTime eTime);
        string RecordUserFreeScore(string account, SCORE_TYPE scoreType, int scores, string note);
        List<UserFreeScore> GetUserFreeScores(string account, DateTime sTime, DateTime eTime);
        string? RecordBonusAward(string winType, float totalBet, float scoreInterval, string awardInfo, string urlTransferPoints, string objTransferResponse);
        string RecordScore(string account, string lobby, string machine, int deviceId, int machineId, long totalInScore, long totalOutScore);
        //List<ScoreInfo> GetScores(string account, DateTime sTime, DateTime eTime);
        string SummarizeScore(string account, string lobbyName, string machine, int deviceId, int machineId, SCORE_TYPE scoreType, DateTime sTime, DateTime eTime);
        string SummarizeUniPlay(string account, string uniPlayDomain, string uniPlayAccount, DateTime sTime, DateTime eTime);

        JObject CreateGM(string gm, string account, string password, string nickName, int permission, string phoneNo, string prefix, float benefitRatio, string note);
        //JObject SaveGM(GMInfo gmInfo);
        string UpdateGM(string account, JObject objParams);
        GMInfo QueryGM(string? account, string? id, string? token);
        List<GMInfo> GetValidateGMInfos(string gmAccount);
        string UpdateGMScores(string account, JObject objScores);
        string RecordGMAction(string account, string action, string note);
        List<GMAction> GetGMActions(string account, List<string> actionFilter, int dataCount);
        List<GMAction> GetGMActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime);

        string AddMachineScore(string lobbyName, int deviceId, int machineId, long inCount, long outCount, string userAccount);
        List<MachineScore> GetMachineScore(DateTime sTime, DateTime eTime, string lobbyName, int deviceId, int machineId, string userAccount);
        string ResetMachineScore(string lobbyName, int deviceId, int machineId);
        string RecordMachineLog(string lobbyName, int deviceId, int machineId, string action, JObject logObj, string userAccount);
        List<MachineLog> GetMachineLog(DateTime sTime, DateTime eTime, List<string> actions, string userAccount);
        JObject GetUnbillingAddSubCoinsRecords(string gm);
        string RecordBill(string account, int totalCoins, string note);
        List<BillInfo> GetBills(string account, bool bFiltGM, DateTime sTime, DateTime eTime);
        JArray GetAddSubCoinsRecords(string account, List<string> actions, DateTime sTime, DateTime eTime);
        string RecordWainZhu(string account, int scoreCnt, int minCnt, int maxCnt);
        List<WainZhu> GetWainZhuRecords(string account);
        string ClearWainZhuRecords();
        string RecordUnknownOutScore(string lobbyName, string machineName, int deviceId, int machineId, int singleOutScore, long outScoreCnt);
        List<UnknownOutScore> GetUnknownOutScore(DateTime sTime, DateTime eTime);
        string RemoveDataInCollection(string collection, DateTime sTime, DateTime eTime);
        string VerifySMSSender(string ip, int maxCnt);
        VerifyInfo CreateVerifyInfo(string userAccount, string phoneNo);
        VerifyInfo ConfirmVerifyInfo(string userAccount, string code, string ip);
        string CreateUniPlay(string name, string domain, string urlLogo, string desc, int profitPayRatio, float exchangeRatioToNTD, bool bAllowable, bool bAcceptable);
        string UpdateUniPlay(string domain, JObject objParams);
        List<UniPlayInfo> QueryUniPlay(string name, string domain);
        void CollectLog(string logger, JObject obj);

        Dictionary<string, List<JObject>> CheckInScoreCheating();
        string PatchDB();
        void Test();
    }
}
