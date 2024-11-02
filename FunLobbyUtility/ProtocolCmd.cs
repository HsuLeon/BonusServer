namespace FunLobbyUtils
{
    public class ProtocolCmd
    {
        public const string IdentifyDevice = "IdentifyDevice";
        public const string CloseSocket = "CloseSocket";
        public const string Heartbeat = "Heartbeat";
        public const string SendAction = "SendAction";
        public const string RequestConnectionInfo = "RequestConnectionInfo";
        public const string ServerResponse = "ServerResponse";
        public const string RequestConnectionDetails = "RequestConnectionDetails";
        public const string RequestConfig = "RequestConfig";
        public const string UpdateConfig = "UpdateConfig";
        public const string RequestLobbies = "RequestLobbies";
        public const string Kickout = "Kickout";
        public const string Join = "Join";
        public const string Leave = "Leave";
        public const string RequestGroupInfo = "RequestGroupInfo";



        public const string ConnectionStatus = "ConnectionStatus";
        public const string Action = "Action";
        public const string Request = "ClientRequest";
        public const string Response = "onResponse";
        public const string IISAck = "onIISAck";
        public const string ConnectionInfo = "onConnectionInfo";
        public const string NotifyConnectionDetails = "NotifyConnectionDetails";
        public const string NotifyConfig = "NotifyConfig";
        public const string NotifyLobbies = "NotifyLobbies";
        public const string NotifyLeave = "NotifyLeave";
        public const string NotifyJoin = "NotifyJoin";
        public const string ResponseGroupInfo = "ResponseGroupInfo";
        public const string OnTest = "OnTest";
    }
}