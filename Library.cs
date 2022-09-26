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

            Log.Message($"Library fully loaded", "L:Load", Log.LogLevel.DEBUG);
        }

        internal static void Save(Livery livery)
        {
            Log.Message($"Saving library livery {livery.Id}", "L:Save");
            try
            {
                File.WriteAllText(Config.LibraryPath + "\\" + livery.FileName, JsonConvert.SerializeObject(livery));
            }
            catch (Exception e)
            {
                Log.Exception($"Error while saving livery {livery.FileName}!", e, "L:Save", Log.LogLevel.WARNING);
            }
        }

        internal static void Add(Livery livery)
        {
            int index = Enumerable.Range(0, Liveries.Count+1).First(i => !Liveries.ContainsKey(i));
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
            public bool Compressed { get; set; }


            public UEGenericStructProperty GvasBaseProperty;

            public Livery(string fileName, UEGenericStructProperty property, string name = "<unnamed>", string model = "<unknown>", bool compressed = true)
            {
                FileName = fileName;
                GvasBaseProperty = property;
                Name = name;
                Model = model;
                Compressed = compressed;
            }

            internal Livery()
            {
                FileName = string.Empty;
                GvasBaseProperty = new UEGenericStructProperty();
                Name = string.Empty;
                Model = string.Empty;
                Compressed = true;
            }
        }

    }
}
