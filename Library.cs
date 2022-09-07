using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace TSW3LM
{
    internal static class Library
    {
        internal static Dictionary<int, Livery> Liveries = new Dictionary<int, Livery>();

        internal static void Load()
        {
            Liveries.Clear();

            Log.AddLogMessage("Loading library...", "L:Load");
            DirectoryInfo Info = new DirectoryInfo(Config.LibraryPath);
            int i = 0;
            foreach (FileInfo file in Info.GetFiles("*.tsw3", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    Livery livery = JsonConvert.DeserializeObject<Livery>(File.ReadAllText(file.FullName));
                    livery.Path = file.FullName;
                    while (Liveries.ContainsKey(i)) i++;
                    Liveries.Add(i, livery);
                }
                catch (Exception e)
                {
                    Log.AddLogMessage($"Error while loading livery {file.Name}: {e.Message}", "L:Load", Log.LogLevel.WARNING);
                }
            }

            /*Log.AddLogMessage($"Loading TSW2LM-Liveries...", "L:Load", Log.LogLevel.DEBUG);
            foreach (FileInfo file in Info.GetFiles("*.tsw2liv"))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(file.FullName);
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i + 1 == data.Length)
                            data[i] = 0;
                        else
                            data[i] = data[i+1];
                    }
                    File.WriteAllBytes(file.FullName + ".tmp", data);

                    List<UEProperty> properties = new List<UEProperty>();

                    BinaryReader reader = new BinaryReader(File.Open(file.FullName + ".tmp", FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.ASCII, true);
                    while (UEProperty.Read(reader) is UEProperty prop) properties.Add(prop);

                    Livery livery = new Livery(file.FullName, properties);

                }
                catch (Exception e)
                {
                    Log.AddLogMessage($"Error while loading livery {file.Name}: {e.Message}", "L:Load", Log.LogLevel.WARNING);
                }
            }*/

            Log.AddLogMessage($"Library fully loaded", "L:Load", Log.LogLevel.DEBUG);
        }

        internal class Livery
        {
            [JsonIgnore]
            internal string Path { get; set; }
            internal string Name { get; set; }
            internal string Model { get; set; }

            private UEGenericStructProperty GvasBaseProperty;

            internal Livery(string path, List<UEProperty> properties)
            {
                Path = path;
                throw new NotImplementedException();
            }
        }

    }
}
