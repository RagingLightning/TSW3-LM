using GvasFormat;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Text;

namespace TSW3LM
{
    internal static class Game
    {
#pragma warning disable CS8618
        private static Gvas GameData;
        private static UEArrayProperty GvasZipArray;
        private static UEArrayProperty GvasRawArray;
#pragma warning restore CS8618

        internal static Dictionary<int, Livery> Liveries = new Dictionary<int, Livery>();

        internal static string? Load()
        {
            Liveries.Clear();

            Log.Message("Loading game liveries...", "G:Load");

            if (Config.GamePath == string.Empty)
            {
                return "Configuration error - Please make sure, you selected a valid game data folder.";
            }
            try
            {
                Log.Message("Deserializing Game Livery file...", "G:Load", Log.LogLevel.DEBUG);
                Log.Message($"File Path: {Config.GamePath}", "G:Load", Log.LogLevel.DEBUG);

                FileStream stream = File.Open(Config.GamePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                GameData = UESerializer.Read(stream);
                stream.Close();

                Log.Message("Game Livery file fully deserialized", "G:Load", Log.LogLevel.DEBUG);
            }
            catch (Exception e)
            {
                Config.GamePath = "";
                Log.Exception("Error while loading Game Liveries!", e, "G:Load", Log.LogLevel.WARNING);
                return $"[ERR] Exception while loading liveries - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }

            Liveries.Clear();

            Log.Message("Loading Liveries...", "G:Load", Log.LogLevel.DEBUG);

            try
            {
                if (!GameData.Properties.Any(p => p is UEArrayProperty))
                {
                    Log.Message("No livery exists in the game...", "G:Load", Log.LogLevel.WARNING);
                }

                try
                {
                    GvasZipArray = (UEArrayProperty)GameData.Properties.First(p => p is UEArrayProperty && p.Name == "CompressedReskins");
                }
                catch (Exception)
                {
                    GvasZipArray = new UEArrayProperty
                    {
                        Name = "CompressedReskins",
                        Type = "ArrayProperty",
                        ItemType = "StructProperty",
                        Items = new UEProperty[] { }
                    };
                    GameData.Properties.Add(GvasRawArray);
                }

                try
                {
                    GvasRawArray = (UEArrayProperty)GameData.Properties.First(p => p is UEArrayProperty && p.Name == "Reskins");
                }
                catch (Exception)
                {
                    GvasRawArray = new UEArrayProperty
                    {
                        Name = "Reskins",
                        Type = "ArrayProperty",
                        ItemType = "StructProperty",
                        Items = new UEProperty[] { }
                    };
                    GameData.Properties.Add(GvasRawArray);
                }

                int i = 0;
                foreach (UEProperty LiveryBase in GvasZipArray.Items)
                {
                    while (Liveries.ContainsKey(i)) i++;
                    var structProperty = (UEGenericStructProperty)LiveryBase;

                    try
                    {
                        var decompressedLivery = CompressionHelper.DecompressReskin(structProperty);

                        if (decompressedLivery != null)
                        {
                            string name = ((UETextProperty)decompressedLivery.GvasBaseProperty.Properties.First(p => p is UETextProperty && p.Name == "DisplayName")).Value;
                            string model = ((UEStringProperty)decompressedLivery.GvasBaseProperty.Properties.First(p => p is UEStringProperty && p.Name == "BaseDefinition")).Value.Split(".")[^1];
                            string newId = GameLiveryInfo.SetInfo(decompressedLivery.ID, name, model);

                            decompressedLivery.ID = newId;
                        }

                    }
                    catch (Exception e)
                    {
                        Log.Exception("Could not decompress livery " + i, e);
                    }

                    Liveries.Add(i, new Livery(structProperty, true));
                }
                foreach (UEProperty LiveryBase in GvasRawArray.Items)
                {
                    while (Liveries.ContainsKey(i)) i++;
                    Liveries.Add(i, new Livery((UEGenericStructProperty)LiveryBase, false));
                }

            }
            catch (Exception e)
            {
                Log.Exception("Error loading game livery!", e, "G:Load");
                return $"[ERR] Exception while loading liveries - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }

            Log.Message("All liveries loaded successfully", "G:Load", Log.LogLevel.DEBUG);

            return null;
        }

        internal static void Save()
        {
            List<UEGenericStructProperty> zip = Liveries.Values.Where(p => p.Compressed).Select(p => p.GvasBaseProperty).ToList();
            List<UEGenericStructProperty> raw = Liveries.Values.Where(p => !p.Compressed).Select(p => p.GvasBaseProperty).ToList();

            GameData.Properties.Clear();

            if (zip.Count > 0)
            {
                // CompressedReskins
                GvasZipArray.Count = zip.Count;
                GvasZipArray.Items = zip.ToArray();
                GvasZipArray.ValueLength = Utils.DetermineValueLength(GvasZipArray, r =>
                {
                    r.ReadUEString(); //name
                    r.ReadUEString(); //type
                    r.ReadInt64(); //valueLength
                    r.ReadUEString(); //itemType
                    r.ReadByte(); //terminator
                    return r.BaseStream.Length - r.BaseStream.Position;
                });

                GameData.Properties.Add(GvasZipArray);
            }

            if (raw.Count > 0)
            {
                // Reskins
                GvasRawArray.Count = raw.Count;
                GvasRawArray.Items = raw.ToArray();
                GvasRawArray.ValueLength = Utils.DetermineValueLength(GvasRawArray, r =>
                {
                    r.ReadUEString(); //name
                    r.ReadUEString(); //type
                    r.ReadInt64(); //valueLength
                    r.ReadUEString(); //itemType
                    r.ReadByte(); //terminator
                    return r.BaseStream.Length - r.BaseStream.Position;
                });

                GameData.Properties.Add(GvasRawArray);
            }

            // None
            GameData.Properties.Add(new UENoneProperty());


            File.WriteAllText($"{Config.LibraryPath}\\gvas.json", JsonConvert.SerializeObject(GameData));

            byte[] Contents = File.ReadAllBytes(Config.GamePath);
            Directory.CreateDirectory($"{Config.LibraryPath}\\backup");
            string backupName = DateTime.Now.ToString("yyMMdd-HHmmss") + ".bak3";
            File.WriteAllBytes($"{Config.LibraryPath}\\backup\\{backupName}", Contents);

            FileStream stream = File.Open(Config.GamePath, FileMode.Create, FileAccess.Write);
            UESerializer.Write(stream, GameData);
            stream.Close();
        }

        internal static void Add(Livery? livery)
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

            internal bool Compressed { get; set; }

            internal UEGenericStructProperty GvasBaseProperty;

            internal Livery(UEGenericStructProperty baseProp, bool compressed)
            {
                GvasBaseProperty = baseProp;
                Compressed = compressed;

                Log.Message($"Livery {ID} loaded successfully", "G:Livery:<init>", Log.LogLevel.DEBUG);
            }


        }
    }
}
