using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace LoadPort
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs"
        );

        private static readonly ConcurrentQueue<string> bufferStrings =
            new ConcurrentQueue<string>();

        // 委托定义
        public delegate void LogDelegate(int type, string info);
        public static LogDelegate AddLogToUI;

        private static readonly object fileLock = new object();

        static Logger()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (bufferStrings.TryDequeue(out var message))
                    {
                        message = message.Replace("\r", "").Replace("\n", "");
                        LogInfoInternal(message);
                    }
                    else
                    {
                        await Task.Delay(50);
                    }
                }
            });
        }

        /// <summary>
        /// 外部调用，放入队列，UI异步更新，文件同步写入，保证顺序
        /// </summary>
        public static void AddLog(string message)
        {
            bufferStrings.Enqueue(message);
        }

        private static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                string date = DateTime.Now.ToString("yyyy-MM-dd");
                string logFile = Path.Combine(LogDirectory, $"{date}.log");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";

                lock (fileLock) // 加锁保证多线程写文件顺序安全
                {
                    File.AppendAllText(logFile, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("写日志失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 内部调用，写文件且回调UI
        /// </summary>
        private static void LogInfoInternal(string message)
        {
            Log(message);
            AddLogToUI?.Invoke(1, message);
        }

        public static void LogInfo(string message)
        {
            AddLog("[INFO] " + message);
        }

        public static void LogError(string message)
        {
            AddLog("[ERROR] " + message);
        }
    }
}
