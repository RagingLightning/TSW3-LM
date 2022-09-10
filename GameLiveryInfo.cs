using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Navigation;

namespace TSW3LM
{
    internal static class GameLiveryInfo
    {
        internal static Process TswMonitor;
        internal static bool Running = true;

        private static string Path;
        private static LiveryInfoCollector InfoCollector;

        internal static bool BypassCollector = false;

        internal static void Init(string path)
        {
            Log.AddLogMessage("LiveryInfo initializing...", "LI:Init");
            Log.AddLogMessage($"|> Path: {path}", "LI:Init", Log.LogLevel.DEBUG);
            Path = path;
            InfoCollector = new LiveryInfoCollector();

            if (File.Exists(Path))
            {
                Load();
            }
            else
            {
                Log.AddLogMessage("|> File doesn't exist, initializing empty data", "LI:Init", Log.LogLevel.WARNING);
                Data = new Dictionary<string, Info>();
            }
        }

        internal static void Load()
        {
            Log.AddLogMessage("Loading Livery info...", "LI:Load");
            Data = JsonConvert.DeserializeObject<Dictionary<string, Info>>(File.ReadAllText(Path));
            Log.AddLogMessage("Livery info loaded.", "LI:Load", Log.LogLevel.DEBUG);
        }

        internal static void Save()
        {
            Log.AddLogMessage("Saving Livery info...", "LI:Save");
            try
            {
                File.WriteAllText(Path, JsonConvert.SerializeObject(Data));
            }
            catch (Exception e)
            {
                Log.AddLogMessage($"Error while saving livery info: {e.Message}", "LI:Save", Log.LogLevel.WARNING);
                return;
            }
            Log.AddLogMessage("Livery info saved.", "LI:Save", Log.LogLevel.DEBUG);
        }

        internal static Info Get(string liveryId, string? cause)
        {
            if (Data.ContainsKey(liveryId))
                return Data[liveryId];
            if (cause == null)
                return new Info();
            InfoCollector.Prepare(cause);
            if (InfoCollector.ShowDialog() == true)
            {
                Info info = new Info(InfoCollector.LiveryName, InfoCollector.LiveryModel);
                Data.Add(liveryId, info);
                return info;
            }
            return new Info();
        }

        internal static string SetInfo(string liveryId, string name, string model, bool replace = false)
        {
            Log.AddLogMessage($"Setting Info for Livery Id {liveryId}", "LI:SetInfo");

            if (Data.ContainsKey(liveryId))
            {
                Log.AddLogMessage($"Livery Id already exists (replace: {replace})", "LI:SetInfo", Log.LogLevel.DEBUG);
                if (!replace)
                {
                    do
                    {
                        int length = liveryId.Length - 2;
                        liveryId = Utils.GenerateHex(length);
                    } while (Data.ContainsKey(liveryId));
                    Log.AddLogMessage($"New Livery Id: {liveryId}");
                }
                Data.Add(liveryId, new Info(name, model));

                Save();
                return liveryId;
            }

            Data[liveryId].Name = name;
            Data[liveryId].Model = model;

            Save();
            return liveryId;
        }

        internal static void Collector()
        {
            LiveryInfoCollector Collector = new LiveryInfoCollector();
            while (true)
            {
                TswMonitor = Process.Start("TSW3Mon.exe", $"\"{Config.GamePath}\" 0.5");
                TswMonitor.WaitForExit();
                if (!Running) return;
                Thread.Sleep(15000);
                if (BypassCollector)
                {
                    BypassCollector = false;
                    continue;
                }
                Game.Load();

                List<string> gameIds = Game.Liveries.Values.Select(l => l.ID).ToList();

                foreach (string id in gameIds.Concat(Data.Keys))
                {
                    if (!Data.ContainsKey(id) && gameIds.Contains(id))
                    {
                        Collector.Prepare("You just saved a livery in TSW3");
                        if (Collector.ShowDialog() == true)
                        {
                            Info info = new Info(Collector.LiveryName, Collector.LiveryModel);
                            Data.Add(id, info);
                        }
                    }
                    else if (Data.ContainsKey(id) && !gameIds.Contains(id))
                        Data.Remove(id);
                }
                
                Save();
            }
        }

        public static Dictionary<string, Info> Data = new Dictionary<string, Info>();

        internal class Info
        {
            public string Name;
            public string Model;

            public Info(string name = "<unnamed>", string model = "<unknown>")
            {
                Name = name;
                Model = model;
            }
        }
    }
}
