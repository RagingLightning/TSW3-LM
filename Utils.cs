#nullable enable
using GvasConverter;
using GvasFormat.Serialization.UETypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

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

        internal static string? CheckUpdate(string version)
        {
            WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW3-LM/deploy/version.dat");
            string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
            Log.Message($"Got version information: {version}->{UpdateResponse}", "U:CheckUpdate", Log.LogLevel.DEBUG);
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
            Log.Message($"Got version information: {version}->{UpdateResponse}", "U:CheckDevUpdate", Log.LogLevel.DEBUG);
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

        internal static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Exception("An Exception occured that was not handled within the program!", (Exception)e.ExceptionObject, "EX");
            if (e.IsTerminating)
            {
                Log.Message("The program is forced to terminate!", "EX", Log.LogLevel.ERROR);
            }
            else
            {
                Log.Message("The program can continue, correct behaviour is not guaranteed though!", "EX", Log.LogLevel.ERROR);
                MainWindow.INSTANCE.ShowStatusText("[ERR] Unhandled exception, please restart the program asap!");
            }
            throw new NotImplementedException();
        }

        internal static Game.Livery ConvertTSW2(byte[] tsw2Data, bool catchFormatError)
        {
            byte[] data = new byte[tsw2Data.Length];
            for (int i = 1; i <= tsw2Data.Length; i++)
            {
                if (i == tsw2Data.Length)
                    data[i - 1] = 0;
                else
                    data[i - 1] = tsw2Data[i];
            }

            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            UEGenericStructProperty prop = new UEGenericStructProperty();
            prop.StructType = "ReskinSave";
            prop.Name = "Reskins";
            prop.Type = "StructProperty";
            prop.ValueLength = 0; //TODO: Determine
            while (UEProperty.Read(reader) is UEProperty p)
            {
                prop.Properties.Add(p);
            }
            try
            {
                ValidateTsw2Import(prop);
            }
            catch (Exception e)
            {
                if (!catchFormatError)
                    throw e;
                else
                {
                    Log.Exception("Error converting TSW2 livery", e, "U:ConvertTSW2", Log.LogLevel.WARNING);
                }
            }

            return new Game.Livery(prop, false);
        }

        internal static void ValidateTsw2Import(UEGenericStructProperty prop)
        {
            //ID
            prop.Properties.First(p => p is UEStringProperty && p.Type == "NameProperty" && p.Name == "ID" && ((UEStringProperty)p).Value.StartsWith("L_"));
            //CreatedDate
            prop.Properties.First(p => p is UEDateTimeStructProperty && p.Name == "CreatedDate");
            //DisplayName
            prop.Properties.First(p => p is UETextProperty && p.Name == "DisplayName");
            //BaseDefinition
            prop.Properties.First(p => p is UEStringProperty && p.Name == "BaseDefinition" && p.Type == "SoftObjectProperty");
            //ReskinnedElements
            UEArrayProperty reskinnedElements = ((UEArrayProperty)prop.Properties.First(p => p is UEArrayProperty && p.Name == "ReskinnedElements" && ((UEArrayProperty)p).ItemType == "StructProperty"));
            foreach (UEGenericStructProperty t in reskinnedElements.Items.ToList())
            {
                if (t.Name != "ReskinnedElements" || t.StructType != "DTGReskinEntry")
                {
                    if (IdentifyMisplacedProperty(t) == Property.UNKNOWN) throw new FormatException($"ReskinnedElements contains foreign property {t.Name} (Type: {t.GetType()})");
                    List<UEProperty> tmp = reskinnedElements.Items.ToList();
                    tmp.Remove(t);
                    reskinnedElements.Items = tmp.ToArray();
                    prop.Properties.Add(t);
                    continue;
                }
                if (t.Properties.Count == 0)
                {
                    reskinnedElements.Count--;
                    List<UEProperty> tmp = reskinnedElements.Items.ToList();
                    tmp.Remove(t);
                    reskinnedElements.Items = tmp.ToArray();
                    continue;
                }
                //contains only "Channels" and UENone
                foreach (UEProperty p2 in t.Properties.ToList())
                {
                    if (p2 is UENoneProperty || p2.Name == "Channels") continue;
                    if (IdentifyMisplacedProperty(p2) == Property.UNKNOWN) throw new FormatException($"ReskinnedElements entry contains foreign property {p2.Name} (Type: {p2.GetType()})");
                    t.Properties.Remove(p2);
                    prop.Properties.Add(p2);
                }
                //last is UENone
                if (!(t.Properties[t.Properties.Count - 1] is UENoneProperty)) t.Properties.Add(new UENoneProperty());
            }
            //ReskinEditorData
            UEGenericStructProperty prp = (UEGenericStructProperty)prop.Properties.First(p => p is UEGenericStructProperty && p.Name == "ReskinEditorData" && ((UEGenericStructProperty)p).StructType == "DTGReskinEditData");
            foreach (UEProperty t in prp.Properties)
            {
                if (t is UEArrayProperty && t.Name == "LastUsedColours" && ((UEArrayProperty)t).ItemType == "StructProperty")
                {
                    if(((UEArrayProperty)t).Count != 0)
                    foreach (UEProperty p2 in ((UEArrayProperty)t).Items.Where(p => !(p is UELinearColorStructProperty)).ToList())
                    {
                        if (IdentifyMisplacedProperty(p2) == Property.UNKNOWN) throw new FormatException($"LastUsedColours contains foreign property {p2.Name} (Type: {p2.GetType()})");
                        List<UEProperty> tmp = ((UEArrayProperty)t).Items.ToList();
                        tmp.Remove(p2);
                        ((UEArrayProperty)t).Items = tmp.ToArray();
                        prop.Properties.Add(p2);
                    }
                }
                else if (!(t is UENoneProperty)) throw new FormatException($"ReskinEditorData contains foreign property {t.Name} (Type: {t.GetType()})");
            }
            //last is UENone
            if (!(prp.Properties[^1] is UENoneProperty)) prp.Properties.Add(new UENoneProperty());

            if (!(prop.Properties[^1] is UENoneProperty)) prop.Properties.Add(new UENoneProperty());

            if (prop.Properties.Count != 7) throw new FormatException($"Number of properties is incorrect (expected: 7, actual: {prop.Properties.Count})");
        }

        private static Property IdentifyMisplacedProperty(UEProperty p)
        {
            if (p is UEStringProperty)
            {
                if (p.Name == "ID" && p.Type == "NameProperty" && ((UEStringProperty)p).Value.StartsWith("L_")) return Property.ID;
                if (p.Name == "BaseDefinition" && p.Type == "SoftObjectProperty") return Property.BASE_DEFINITION;
            }
            else if (p is UEDateTimeStructProperty)
            {
                if (p.Name == "CreatedDate") return Property.CREATED_DATE;
            }
            else if (p is UETextProperty)
            {
                if (p.Name == "DisplayName") return Property.DISPLAY_NAME;
            }
            else if (p is UEArrayProperty)
            {
                if (p.Name == "ReskinnedElements" && ((UEArrayProperty)p).ItemType == "StructProperty") return Property.RESKINNED_ELEMENTS;
            }
            else if (p is UEGenericStructProperty)
            {
                if (p.Name == "ReskinEditorData" && ((UEGenericStructProperty)p).StructType == "DTGReskinEditData") return Property.RESKIN_EDITOR_DATA;
            }
            return Property.UNKNOWN;
        }

        private enum Property
        {
            UNKNOWN = 0,
            ID = 1,
            CREATED_DATE = 2,
            DISPLAY_NAME = 3,
            BASE_DEFINITION = 4,
            RESKINNED_ELEMENTS = 5,
            RESKIN_EDITOR_DATA = 6
        }
    }

    class Log
    {
        private static readonly object locker = new object();

        private static readonly Dictionary<string, LogLevel> LogPaths = new Dictionary<string, LogLevel>();

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
                File.AppendAllText(path, $"+-------------------\n+ Log file added on {DateTime.Now:MMddTHH:mm:ss.fff} at level {level}\n+-------------------\n");
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
        public static void Message(string message, string stack = "-", LogLevel level = LogLevel.INFO)
        {
            lock (locker)
            {
                string Timestamp = DateTime.Now.ToString("MMddTHH:mm:ss.fff");
                string LogLine = $"[{level}] {Timestamp} {stack} | {message}\n";
                if (ConsoleLevel <= level)
                {
                    Trace.Write(LogLine);
                    Console.Write(LogLine);
                }
                foreach (KeyValuePair<string, LogLevel> p in LogPaths.Where(p => p.Value <= level))
                {
                    File.AppendAllText(p.Key, LogLine);
                }
            }
        }

        public static void Exception(string message, Exception e, string stack = "-", LogLevel level = LogLevel.ERROR)
        {
            Message(message, stack, level);
            lock (locker)
            {
                string Timestamp = DateTime.Now.ToString("MMddTHH:mm:ss.fff");
                LogLevel stackLevel = level == LogLevel.ERROR ? LogLevel.ERROR : LogLevel.DEBUG;
                string LogLine = $"[{level}] {Timestamp} {stack} | {e.Message}\n{e.StackTrace}\n";
                if (ConsoleLevel <= level)
                {
                    Trace.Write(LogLine);
                    Console.Write(LogLine);
                }
                foreach (KeyValuePair<string, LogLevel> p in LogPaths.Where(p => p.Value <= level))
                {
                    File.AppendAllText(p.Key, LogLine);
                }
            }
        }

        public enum LogLevel
        {
            ERROR = 4,
            WARNING = 3,
            INFO = 2,
            DEBUG = 1
        }

    }

    class LibLivJsonConverter : GvasJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Library.Livery).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            Library.Livery livery = existingValue == null ? new Library.Livery() : (Library.Livery)existingValue;
            JObject jo = JObject.Load(reader);
            try
            {
#pragma warning disable CS8602,CS8600
                livery.Name = jo["Name"].ToString();
                livery.Model = jo["Model"].ToString();
                livery.GvasBaseProperty = (UEGenericStructProperty)ReadUEProperty((JObject)jo["GvasBaseProperty"]);
                return livery;
#pragma warning restore CS8602,CS8600
            }
            catch (Exception)
            {
                throw new FormatException("Unable to deserialize library livery");
            }
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
