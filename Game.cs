using GvasFormat;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TSW3LM
{
    internal static class Game
    {
        private static Gvas GameData;
        private static UEArrayProperty GvasReskinArray;
        private static UEArrayProperty GvasLegacyArray;

        internal static Dictionary<int, Livery> Liveries = new Dictionary<int, Livery>();

        internal static string? Load()
        {
            Liveries.Clear();

            Log.AddLogMessage("Loading game liveries...", "G:Load");

            if (Config.GamePath == string.Empty)
            {
                return "Configuration error - Please make sure, you selected a valid game data folder.";
            }
            try
            {
                Log.AddLogMessage("Deserializing Game Livery file...", "G:Load", Log.LogLevel.DEBUG);
                Log.AddLogMessage($"File Path: {Config.GamePath}", "G:Load", Log.LogLevel.DEBUG);

                FileStream stream = File.Open(Config.GamePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                GameData = UESerializer.Read(stream);
                stream.Close();

                Log.AddLogMessage("Game Livery file fully deserialized", "G:Load", Log.LogLevel.DEBUG);
            }
            catch (FileNotFoundException e)
            {
                Config.GamePath = "";
                Log.AddLogMessage($"FileNotFoundException: {e.FileName}", "G:Load", Log.LogLevel.WARNING);
                return $"Game Livery file not found - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }
            catch (IOException e)
            {
                Config.GamePath = "";
                Log.AddLogMessage($"IOException: {e.Message}", "G:Load", Log.LogLevel.WARNING);
                return $"Exception while reading game liveries - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }

            Liveries.Clear();

            Log.AddLogMessage("Loading Liveries...", "G:Load", Log.LogLevel.DEBUG);

            try
            {
                if (!GameData.Properties.Any(p => p is UEArrayProperty))
                {
                    Log.AddLogMessage("No livery exists in the game...", "G:Load", Log.LogLevel.WARNING);
                    GvasReskinArray = new UEArrayProperty();
                    return null;
                }
                GvasReskinArray = (UEArrayProperty)GameData.Properties.First(p => p is UEArrayProperty && p.Name == "CompressedReskins");

                try
                {
                    GvasLegacyArray = (UEArrayProperty)GameData.Properties.First(p => p is UEArrayProperty && p.Name == "Reskins");
                }
                catch (Exception)
                {
                    GvasLegacyArray = new UEArrayProperty();
                    GameData.Properties.Add(GvasLegacyArray);
                    GvasLegacyArray.Name = "Reskins";
                    GvasLegacyArray.Items = new UEProperty[] { };
                }

                int i = 0;
                foreach (UEProperty LiveryBase in GvasReskinArray.Items)
                {
                    while (Liveries.ContainsKey(i)) i++;
                    Liveries.Add(i, new Livery((UEGenericStructProperty)LiveryBase));
                }
                foreach (UEProperty LiveryBase in GvasLegacyArray.Items)
                {
                    while (Liveries.ContainsKey(i)) i++;
                    Liveries.Add(i, new Livery((UEGenericStructProperty)LiveryBase, true));
                }

            }
            catch (Exception e)
            {
                Log.AddLogMessage($"Error loading Livery: {e.Message}", "G:Load", Log.LogLevel.ERROR);
                return $"Exception while loading liveries - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }

            Log.AddLogMessage("All liveries loaded successfully", "G:Load", Log.LogLevel.DEBUG);

            return null;
        }

        internal static void Save()
        {
            List<UEGenericStructProperty> tsw3 = Liveries.Values.Where(p => !p.IsLegacy).Select(p => p.GvasBaseProperty).ToList();
            List<UEGenericStructProperty> tsw2 = Liveries.Values.Where(p => p.IsLegacy).Select(p => p.GvasBaseProperty).ToList();

            GvasReskinArray.Count = tsw3.Count;
            GvasReskinArray.Items = tsw3.ToArray();

            if (tsw2.Count > 0)
            {
                GvasLegacyArray.Count = tsw2.Count;
                GvasLegacyArray.Items = tsw2.ToArray();
            }
            else
            {
                GameData.Properties.Remove(GvasLegacyArray);
            }

            byte[] Contents = File.ReadAllBytes(Config.GamePath);
            Directory.CreateDirectory($"{Config.LibraryPath}\\backup");
            string backupName = DateTime.Now.ToString("yyMMdd-HHmmss") + ".bak3";
            File.WriteAllBytes($"{Config.LibraryPath}\\backup\\{backupName}", Contents);

            FileStream stream = File.Open(Config.GamePath, FileMode.Create, FileAccess.Write);
            UESerializer.Write(stream, GameData);
            stream.Close();
        }

        internal static void Add(Livery livery)
        {
            int index = Enumerable.Range(0, Liveries.Count + 1).First(i => !Liveries.ContainsKey(i));
            Liveries[index] = livery;
        }

        internal class Livery
        {
            internal string ID
            {
                get { return ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty)).Value; }
                set { ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty)).Value = value; }
            }

            internal bool IsLegacy { get; set; }

            internal UEGenericStructProperty GvasBaseProperty;

            internal Livery(UEGenericStructProperty baseProp, bool isLegacy = false)
            {
                GvasBaseProperty = baseProp;
                IsLegacy = isLegacy;

                Log.AddLogMessage($"Livery {ID} loaded successfully", "G:Livery:<init>", Log.LogLevel.DEBUG);
            }


        }

    }
}
