namespace BonusServer.Services.QueueInfo
{
    public interface QueueInterface
    {
        void Start(string server, string userName, string password);
        void Stop();
        string? Publish(string channel, string message);
        bool FireHeartbeat();
    }
}
