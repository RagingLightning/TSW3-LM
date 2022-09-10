#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls;

namespace TSW3LM
{
    static class Utils
    {
        private static Random random = new Random();

        internal static string GenerateHex(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        internal static string? checkUpdate(string version)
        {
            WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW3-LM/deploy/version.dat");
            string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
            Log.AddLogMessage($"Got version information: {version}->{UpdateResponse}", "U:CheckUpdate", Log.LogLevel.DEBUG);
            string[] NewVersion = UpdateResponse.Split('.');
            string[] CurrentVersion = version.Split('.');
            char CurrentSuffix = ' ';
            if (!int.TryParse(CurrentVersion[^1], out int _))
            {
                CurrentSuffix = version.Last();
                CurrentVersion[^1] = CurrentVersion[^1].Split(CurrentSuffix)[0];
            }
            int update = 0;
            bool fullVersionUpdate = true;
            for (int i = 0; i < NewVersion.Length; i++)
            {
                if (int.Parse(NewVersion[i]) < int.Parse(CurrentVersion[i]))
                {
                    update -= (int)Math.Pow(10, 2 - i);
                }
                if (int.Parse(NewVersion[i]) > int.Parse(CurrentVersion[i]))
                {
                    update += (int)Math.Pow(10, 2 - i);
                }
                if (int.Parse(NewVersion[i]) != int.Parse(CurrentVersion[i])) fullVersionUpdate = false;
            }
            if (update > 0 || (fullVersionUpdate && CurrentSuffix != ' ')) return UpdateResponse;
            return null;
        }

        internal static string? CheckDevUpdate(string version)
        {
            WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW3-LM/deploy/devversion.dat");
            string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
            Log.AddLogMessage($"Got version information: {version}->{UpdateResponse}", "U:CheckDevUpdate", Log.LogLevel.DEBUG);
            string[] NewVersion = UpdateResponse.Split('.');
            string[] CurrentVersion = version.Split('.');
            char NewSuffix = ' ';
            char CurrentSuffix = ' ';
            if (!int.TryParse(NewVersion[^1], out int _))
            {
                NewSuffix = UpdateResponse.Last();
                NewVersion[^1] = NewVersion[^1].Split(NewSuffix)[0];
            }
            if (!int.TryParse(CurrentVersion[^1], out int _))
            {
                CurrentSuffix = version.Last();
                CurrentVersion[^1] = CurrentVersion[^1].Split(CurrentSuffix)[0];
            }
            bool update = false;
            bool devUpdate = NewSuffix != ' ' && (NewSuffix > CurrentSuffix || CurrentSuffix == ' ');
            for (int i = 0; i < NewVersion.Length; i++)
            {
                if (NewSuffix == ' ' && int.Parse(NewVersion[i]) > int.Parse(CurrentVersion[i]))
                {
                    update = true;
                }
                if (int.Parse(NewVersion[i]) < int.Parse(CurrentVersion[i])) devUpdate = false;
            }
            if (devUpdate || update) return UpdateResponse;
            return null;
        }

        /// <summary>searches a byte array for a given sequence of bytes</summary>
        /// <param name="hay">The hay stack to be searched</param>
        /// <param name="needle">The needle</param>
        /// <returns>the starting index of the first occurrence found</returns>
        internal static int LocateInByteArray(byte[] hay, byte[] needle)
        {
            if (hay == null || needle == null || hay.Length == 0 || needle.Length == 0 || needle.Length > hay.Length) return -1;

            for (int i = 0; i < hay.Length; i++)
            {
                if (hay[i] == needle[0])
                {
                    if (needle.Length == 1) return i;
                    if (i + needle.Length > hay.Length) return -1;
                    for (int j = 1; j < needle.Length; j++)
                    {
                        if (hay[i + j] != needle[j]) break;
                        if (j == needle.Length - 1) return i;
                    }
                }
            }
            return -1;
        }

        internal static string SanitizeFileName(string name)
        {
            char[] illegalCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            foreach (char c in illegalCharacters)
            {
                name = name.Replace(c, '-');
            }
            return name;
        }

    }

    class Log
    {
        private static readonly object locker = new object();

        private static Dictionary<string, LogLevel> LogPaths = new Dictionary<string, LogLevel>();

        /// <summary>The highest LogLevel shown on the Console</summary>
        public static LogLevel ConsoleLevel = LogLevel.INFO;

        /// <summary>  Adds a log file to be written to</summary>
        /// <param name="path">The path of the new log file, relative or absolute.</param>
        /// <param name="level">The highest LogLevel that will be written to this file.</param>
        /// <returns>true, if the file was added successfully, false, if the file was already added, only the level has been changed</returns>
        /// <exception cref="IOException">There was an error accessing {path} in an attempt to add it as a level {level} log file.</exception>
        public static bool AddLogFile(string path, LogLevel level)
        {
            if (LogPaths.ContainsKey(path))
            {
                LogPaths[path] = level;
                return false;
            }
            try
            {
                File.OpenWrite(path).Close();
                LogPaths.Add(path, level);
                return true;
            }
            catch (Exception)
            {
                throw new IOException($"There was an error accessing {path} in an attempt to add it as a level {level} log file.");
            }
        }

        /// <summary>  Logs a message in the format "[&lt;LEVEL&gt;] &lt;Time&gt; &lt;stack&gt; | &lt;message&gt;"</summary>
        /// <param name="message">The log message.</param>
        /// <param name="stack">A simple string representation of the call stack</param>
        /// <param name="level">The LogLevel of this message, it will only be logged to each file that has a LogLevel at or above that of the message</param>
        public static void AddLogMessage(string message, string? stack = "-", LogLevel? level = LogLevel.INFO)
        {
            lock (locker)
            {
                string Timestamp = DateTime.Now.ToString("MMddTHH:mm:ss.fff");
                string LogLine = $"[{level.ToString()}] {Timestamp} {stack} | {message}\n";
                if (ConsoleLevel >= level)
                {
                    Trace.Write(LogLine);
                    Console.Write(LogLine);
                }
                foreach (KeyValuePair<string, LogLevel> p in LogPaths.Where(pair => pair.Value >= level))
                {
                    File.AppendAllText(p.Key, LogLine);
                }
            }
        }

        public enum LogLevel
        {
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4
        }

    }
}
