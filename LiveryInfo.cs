using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TSW3LM
{
    internal static class LiveryInfo
    {

        private static string Path;

        internal static void Init(string path)
        {
            Log.AddLogMessage("LiveryInfo initializing...", "LI:Init");
            Log.AddLogMessage($"|> Path: {path}", "LI:Init", Log.LogLevel.DEBUG);
            Path = path;

            if (File.Exists(Path))
            {
                Load();
            } else
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
            } catch (Exception e)
            {
                Log.AddLogMessage($"Error while saving livery info: {e.Message}", "LI:Save", Log.LogLevel.WARNING);
                return;
            }
            Log.AddLogMessage("Livery info saved.", "LI:Save", Log.LogLevel.DEBUG);
        }

        internal static Info Get(string liveryId)
        {
            return Data.GetValueOrDefault(liveryId, new Info(liveryId, "<unknown>"));
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
            while (true)
            {
                Process TswMonitor = Process.Start("TSW3Mon.exe", $"\"{Config.GamePath}\" 0.5");
                TswMonitor.WaitForExit();
                Game.Load();

                foreach (Game.Livery livery in Game.Liveries.Values)
                {
                    if (!Data.ContainsKey(livery.ID))
                    {
                        LiveryInfoCollector collector = new LiveryInfoCollector();
                        collector.ShowActivated = true;
                        if (collector.ShowDialog() == true)
                        {
                            Info info = new Info(collector.Name, collector.Model);
                            Data.Add(livery.ID, info);
                        }
                    }
                }

                Save();
            }
        }

        private static Dictionary<string, Info> Data = new Dictionary<string, Info>();

        internal class Info
        {
            internal string Name;
            internal string Model;

            internal Info(string name, string model)
            {
                Name = name;
                Model = model;
            }
        }
    }
}
