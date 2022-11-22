﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace TSW3LM
{
    static class Config
    {
#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        internal static Entries entries;
        private static string Path;
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.

        internal static void Init(string path)
        {
            Path = path;

            if (File.Exists(Path))
            {
                Log.Message("Loading Config...", "Config:<init>", Log.LogLevel.DEBUG);
                entries = JsonConvert.DeserializeObject<Entries>(File.ReadAllText(path));
                Log.Message("Config loaded", "Config:<init>", Log.LogLevel.DEBUG);
            }
            else
            {
                Log.Message("No Config found, initializing empty config", "Config:<init>", Log.LogLevel.DEBUG);
                entries = new Entries();
            }
        }

        public static bool SkipAutosave
        {
            set { entries.SkipAutosave = value; if (!value) Save(); }
        }

        public static string GamePath
        {
            get { return entries.GamePath; }
            set { entries.GamePath = value; if (!entries.SkipAutosave) Save(); }
        }

        public static string LibraryPath
        {
            get { return entries.LibraryPath; }
            set { entries.LibraryPath = value; if (!entries.SkipAutosave) Save(); }
        }

        [JsonIgnore]
        public static bool NoUpdate { set => AutoUpdate = !value; }
        public static bool AutoUpdate
        {
            get { return entries.AutoUpdate; }
            set { entries.AutoUpdate = value; if (!entries.SkipAutosave) Save(); }
        }

        public static bool DevUpdates
        {
            get { return entries.DevUpdates; }
            set { entries.DevUpdates = value; if (!entries.SkipAutosave) Save(); }
        }

        public static int MaxGameLiveries
        {
            get { return entries.MaxGameLiveries; }
            set { entries.MaxGameLiveries = value; if (!entries.SkipAutosave) Save(); }
        }

        [JsonIgnore]
        public static bool CollectLiveryData
        {
            get { return entries.CollectLiveryData; }
            set { entries.CollectLiveryData = value; if (!entries.SkipAutosave) Save(); }
        }

        internal static void Save()
        {
            Log.Message("Saving Config...", "Config:Save", Log.LogLevel.DEBUG);
            File.WriteAllText(Path, JsonConvert.SerializeObject(entries, new JsonSerializerSettings { Formatting = Formatting.Indented }));
            Log.Message("Config saved", "Config:Save", Log.LogLevel.DEBUG);
        }

        internal static void ApplyDefaults()
        {
            entries.ApplyDefaults();
            Save();
        }

        internal class Entries
        {
            public bool SkipAutosave = false;
            public string GamePath = "";
            public string LibraryPath = "";
            public bool AutoUpdate = true;
            public bool DevUpdates = false;
            public int MaxGameLiveries = 300;
            public bool CollectLiveryData = true;

            public static readonly Dictionary<string, string> DEFAULTS = new()
            {
                { "SkipAutosave", "false" },
                {"GamePath", "\"\"" },
                {"LibraryPath","\"\"" },
                {"AutoUpdate","true" },
                {"DevUpdates","false" },
                {"MaxGameLiveries","300" }
            };

            internal void ApplyDefaults()
            {
                SkipAutosave = false;
                GamePath = "";
                LibraryPath = "";
                AutoUpdate = true;
                DevUpdates = false;
                MaxGameLiveries = 300;
                CollectLiveryData = true;
            }
        }

    }

}
