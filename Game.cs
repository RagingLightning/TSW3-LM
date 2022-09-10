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

                GameData = UESerializer.Read(File.Open(Config.GamePath, FileMode.Open, FileAccess.Read, FileShare.Read));
               
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
                GvasReskinArray = (UEArrayProperty)GameData.Properties.First(p => p is UEArrayProperty);
                int i = 0;
                foreach (UEProperty LiveryBase in GvasReskinArray.Items)
                {
                    while (Liveries.ContainsKey(i)) i++;
                    Liveries.Add(i, new Livery((UEGenericStructProperty)LiveryBase));
                }
            } catch (Exception e)
            {
                Log.AddLogMessage($"Error loading Livery: {e.Message}", "G:Load", Log.LogLevel.WARNING);
                return $"Exception while loading liveries - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }

            Log.AddLogMessage("All liveries loaded successfully", "G:Load", Log.LogLevel.DEBUG);

            return null;
        }

        internal static void Save()
        {
            GvasReskinArray.Count = Liveries.Count;
            List<UEProperty> properties = new List<UEProperty>();
            foreach (Livery livery in Liveries.Values)
            {
                properties.Add(livery.GvasBaseProperty);
            }
            GvasReskinArray.Items = properties.ToArray();

            byte[] Contents = File.ReadAllBytes(Config.GamePath);
            Directory.CreateDirectory($"{Config.LibraryPath}\\backup");
            string backupName = DateTime.Now.ToString("yyMMdd-HHmmss") + ".bak3";
            File.WriteAllBytes($"{Config.LibraryPath}\\backup\\{backupName}", Contents);

            UESerializer.Write(File.Open(Config.GamePath, FileMode.Create, FileAccess.Write), GameData);
        }

        internal static void Add(Livery livery)
        {
            int index = Enumerable.Range(0, Liveries.Count+1).First(i => !Liveries.ContainsKey(i));
            Liveries[index] = livery;
        }

        internal class Livery
        {
            internal string ID {
                get { return ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty)).Value; }
                set { ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty)).Value = value; } 
            }

            internal UEGenericStructProperty GvasBaseProperty;

            internal Livery(UEGenericStructProperty baseProp)
            {
                GvasBaseProperty = baseProp;

                Log.AddLogMessage($"Livery {ID} loaded successfully", "G:Livery:<init>", Log.LogLevel.DEBUG);

            }


        }

    }
}
