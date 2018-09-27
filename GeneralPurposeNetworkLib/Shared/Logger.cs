using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace GeneralPurposeNetworkLib.Shared
{
    public static class Logger
    {
        public enum Level
        {
            Everything,
            Info,
            Warning,
            Error,
            Always
        }

        public static Level Verbosity { get; set; } = Level.Everything;

        private static TextWriter writer = Console.Out;
        private static TextReader reader = Console.In;

        public static void SetIOStream(Stream s)
        {
            if (s == null)
            {
                if (writer != null) writer.Close();
                if (reader != null) reader.Close();

                writer = Console.Out;
                reader = Console.In;
            }
            else
            {
                if (s.CanWrite) writer = new StreamWriter(s);
                if (s.CanRead) reader = new StreamReader(s);
            }
        }

        public static void WriteLine(string msg, Level logLevel = Level.Always, bool prependTimestamp = true, [CallerMemberName] string caller = "", [CallerFilePath] string callerFilePath = "")
        {
            if (logLevel < Verbosity) return;
            if (prependTimestamp) msg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + msg;
            var callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);
            writer?.WriteLine(callerTypeName + "->" + caller + ": " + msg);
        }
        public static void Write(string msg, Level logLevel = Level.Always, bool prependTimestamp = true, [CallerMemberName] string caller = "", [CallerFilePath] string callerFilePath = "")
        {
            if (logLevel < Verbosity) return;
            var callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);
            writer?.Write(callerTypeName + "->" + caller + ": " + msg);
        }

        public static string ReadLine() => reader?.ReadLine();
    }
}
