using System.Timers;

namespace FunLobbyUtils
{
    public class Log
    {
        static Log mInstance = null;
        System.Timers.Timer mFlushTimer = null;
        StreamWriter mWriter = null;
        Mutex mWriteMutex = null;
        Mutex mQueueMutex = null;
        Queue<string> mQueue = null;

        public static string Path { get; set; }

        static private Log Instance
        {
            get
            {
                if (mInstance == null) mInstance = new Log(Path);
                return mInstance;
            }
        }

        private Log(string path = null)
        {
            if (path == null) path = "./Log";
            path = path.Replace("\\", "/");
            string tmpPath = "";
            string[] strPath = path.Split("/");
            for (int i = 0; i < strPath.Length; i++)
            {
                tmpPath += strPath[i];
                if (Directory.Exists(tmpPath) == false) Directory.CreateDirectory(tmpPath);
                tmpPath += "/";
            }
            string fileName = path + "/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";
            FileStream logStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            mWriter = new StreamWriter(logStream);
            mWriteMutex = new Mutex();
            mQueueMutex = new Mutex();
            mQueue = new Queue<string>();

            if (Config.Instance.ExportLog)
            {
                mFlushTimer = new System.Timers.Timer();
                mFlushTimer.Interval = 20 * 1000; // do flush per 20 seconds
                mFlushTimer.Elapsed += FlushHandler;
                mFlushTimer.Start();
            }
        }

        static public void StoreMsg(string msg)
        {
            if (Config.Instance == null ||
                Config.Instance.ExportLog == false)
                return;

            Instance.mQueueMutex.WaitOne();
            string log = DateTime.Now.ToString("HH:mm:ss") + " => " + msg;
            Instance.mQueue.Enqueue(log);

            if (Instance.mQueue.Count >= 200)
            {
                Flush(true);
            }
            Instance.mQueueMutex.ReleaseMutex();
        }

        static public void Flush(bool bWait = false)
        {
            if (Config.Instance == null ||
                Config.Instance.ExportLog == false ||
                Instance.mQueue.Count == 0)
                return;

            Action action = new Action(() =>
            {
                Instance.mWriteMutex.WaitOne();
                try
                {
                    if (Instance.mWriter != null)
                    {
                        while (Instance.mQueue.Count > 0)
                        {
                            string msg = Instance.mQueue.Dequeue();
                            Instance.mWriter.WriteLine(msg);
                        }
                        Instance.mWriter.Flush();
                    }
                    else
                    {
                        Instance.mQueue.Clear();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Instance.mWriteMutex.ReleaseMutex();
            });
            Task taskExport = new Task(action);
            taskExport.Start();

            if (bWait) taskExport.Wait();
        }

        protected void FlushHandler(object sender, ElapsedEventArgs e)
        {
            Flush();
        }
    }
}
