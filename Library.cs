using GvasConverter;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace TSW3LM
{
    internal static class Library
    {
        internal static Dictionary<int, Livery> Liveries = new Dictionary<int, Livery>();

        internal static void Load()
        {
            Liveries.Clear();

            Log.Message("Loading library...", "L:Load");
            DirectoryInfo Info = new DirectoryInfo(Config.LibraryPath);
            int i = 0;
            foreach (FileInfo file in Info.GetFiles("*.tsw3", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    Livery? livery = JsonConvert.DeserializeObject<Livery>(File.ReadAllText(file.FullName), new LibLivJsonConverter());
                    if (livery == null)
                        throw new FormatException("Library livery could not be deserialized");
                    livery.FileName = file.Name;

                    while (Liveries.ContainsKey(i)) i++;

                    Liveries.Add(i, livery);
                }
                catch (Exception e)
                {
                    Log.Exception($"Error while loading livery {file.Name}!", e, "L:Load", Log.LogLevel.WARNING);
                }
            }

            foreach (FileInfo file in Info.GetFiles("*.tsw3bin", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var bytes = File.ReadAllBytes(file.FullName);
                    var decompressed = Utils.ConvertTSW3(bytes, false);
                    string name = ((UETextProperty)decompressed.GvasBaseProperty.Properties.First(p => p is UETextProperty && p.Name == "DisplayName")).Value;
                    string model = ((UEStringProperty)decompressed.GvasBaseProperty.Properties.First(p => p is UEStringProperty && p.Name == "BaseDefinition")).Value.Split(".")[^1];
                    var idProperty = decompressed.GvasBaseProperty.Properties.Find(p => p.Name == "ID");
                    var compressed = CompressionHelper.CompressReskin(idProperty, bytes);
                    
                    var livery = new Livery(file.FullName, compressed, LiveryType.UNCOMPRESSED_TSW3, name: name, model: model);
                    if (livery == null)
                        throw new FormatException("Library livery could not be deserialized");
                    
                    while (Liveries.ContainsKey(i)) i++;

                    Liveries.Add(i, livery);
                }
                catch (Exception e)
                {
                    Log.Exception($"Error while loading livery {file.Name}!", e, "L:Load", Log.LogLevel.WARNING);
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

            Log.Message($"Library fully loaded", "L:Load", Log.LogLevel.DEBUG);
        }

        internal static void Save(Livery livery)
        {
            Log.Message($"Saving library livery {livery.Id}", "L:Save");

            //if (livery.Type == LiveryType.COMPRESSED_TSW3)
            //{
            //    try
            //    {
            //        var decompressed = CompressionHelper.DecompressReskin(livery.GvasBaseProperty);
            //        if (decompressed != null)
            //        {
            //            livery.GvasBaseProperty = decompressed.GvasBaseProperty;
            //            livery.Type = LiveryType.DESERIALIZED_TSW3;
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Exception("Could not decompress livery " + livery.Name, e);
            //    }
            //}

            if (livery.Type == LiveryType.UNCOMPRESSED_TSW3
                && CompressionHelper.DecompressReskin(livery.GvasBaseProperty) is Game.Tsw3UncompressedLivery uncompressedLivery)
            {
                livery.FileName = Path.ChangeExtension(livery.FileName, ".tsw3bin");

                var path = Path.Combine(Config.LibraryPath, livery.FileName);
                File.WriteAllBytes(path, uncompressedLivery.Bytes);
                return;
            }

            try
            {
                File.WriteAllText(Config.LibraryPath + "\\" + livery.FileName, JsonConvert.SerializeObject(livery, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Exception($"Error while saving livery {livery.FileName}!", e, "L:Save", Log.LogLevel.WARNING);
            }
        }

        internal static void Add(Livery livery)
        {
            int index = Enumerable.Range(0, Liveries.Count + 1).First(i => !Liveries.ContainsKey(i));
            Liveries[index] = livery;
        }

        internal class Livery
        {
            internal string FileName { get; set; }
            internal string Id
            {
                get { return ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty && p.Name == "ID")).Value; }
                set { ((UEStringProperty)GvasBaseProperty.Properties.First(p => p is UEStringProperty && p.Name == "ID")).Value = value; }
            }
            public string Name { get; set; }
            public string Model { get; set; }
            public string Type { get; set; }


            public UEGenericStructProperty GvasBaseProperty;

            public Livery(string fileName, UEGenericStructProperty property, string type, string name = "<unnamed>", string model = "<unknown>")
            {
                FileName = fileName;
                Name = name;
                Model = model;
                Type = type;
                GvasBaseProperty = property;
            }

            internal Livery()
            {
                FileName = string.Empty;
                GvasBaseProperty = new UEGenericStructProperty();
                Name = string.Empty;
                Model = string.Empty;
                Type = LiveryType.COMPRESSED_TSW3;
            }
        }

    }
}
