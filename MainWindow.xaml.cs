#nullable disable warnings
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.Net;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Runtime.CompilerServices;

namespace TSW3LM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string VERSION = "0.0.1";

        private Thread InfoCollectorThread = new Thread(GameLiveryInfo.Collector);

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        [DllImport("Kernel32.dll")]
        public static extern bool FreeConsole();

        public MainWindow()
        {

            AttachConsole(-1);

            Config.Init("TSW3LM.json");
            GameLiveryInfo.Init("LiveryInfo.json");

            Log.AddLogFile("TSW2LM.log", Log.LogLevel.INFO);
            if (Environment.GetCommandLineArgs().Contains("-debug"))
            {
                Log.AddLogFile("TSW2LM_debug.log", Log.LogLevel.DEBUG);
                Log.ConsoleLevel = Log.LogLevel.DEBUG;
            }
            Log.AddLogMessage($"Command line: {Environment.GetCommandLineArgs()}", "MW:<init>", Log.LogLevel.DEBUG);

            string[] args = Environment.GetCommandLineArgs();
            Config.SkipAutosave = true;
            for (int i = 1; i < args.Length; i++)
            {
                try
                {
                    switch (args[i])
                    {
                        case "-maxGameLiveries":
                            if (!int.TryParse(args[i + 1], out int count)) PrintHelp();
                            Config.MaxGameLiveries = count > 300 ? count : 300;
                            break;
                        case "-noUpdate":
                            if (!(args[i + 1] == "true" || args[i + 1] == "false")) PrintHelp();
                            Config.NoUpdate = args[i + 1] == "true";
                            break;
                        case "-devUpdates":
                            if (!(args[i + 1] == "true" || args[i + 1] == "false")) PrintHelp();
                            Config.DevUpdates = args[i + 1] == "true";
                            break;
                        case "-reset":
                            Config.ApplyDefaults();
                            break;
                        case "-noInfoCollect":
                            if (!(args[i + 1] == "true" || args[i + 1] == "false")) PrintHelp();
                            Config.CollectLiveryData = args[i + 1] == "false";
                            break;
                        case "-help":
                        case "-?":
                            PrintHelp();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.AddLogMessage($"Failed to parse command line argument '{args[i]}': {e.Message}", "MW:<init>", Log.LogLevel.WARNING);
                }
            }
            Config.SkipAutosave = false;

            if (!Config.NoUpdate)
            {
                try
                {
                    Log.AddLogMessage("Checking for updates...", "MW:<init>");
                    string? newVersion = Utils.checkUpdate(VERSION);
                    if (newVersion != null)
                        new UpdateNotifier(VERSION, newVersion, $"https://github.com/RagingLightning/TSW3-LM/releases/tag/v{newVersion}").ShowDialog();
                }
                catch (WebException e)
                {
                    Log.AddLogMessage($"Unable to check for updates: {e.Message}", "MW:<init>", Log.LogLevel.WARNING);
                }

            }

            if (Config.DevUpdates)
            {
                try
                {
                    Log.AddLogMessage("Checking for dev updates...", "MW::<init>");
                    string? newVersion = Utils.checkUpdate(VERSION);
                    if (newVersion != null)
                        new UpdateNotifier(VERSION, newVersion, $"https://github.com/RagingLightning/TSW3-LM/releases/tag/v{newVersion}").ShowDialog();
                }
                catch (WebException e)
                {
                    Log.AddLogMessage($"Unable to check for dev updates: {e.Message}", "MW:<init>", Log.LogLevel.WARNING);
                }
            }

            InitializeComponent();
            DataContext = new Data();

            if (Config.GamePath != "")
            {
                Log.AddLogMessage("Loading GamePath Data...", "MW::<init>");
                if (File.Exists(Config.GamePath))
                {
                    txtGameDir.Text = Config.GamePath;
                    string GameStatus = Game.Load();
                    if (GameStatus != null)
                    {
                        lblMessage.Content = $"ERROR WHILE LOADING GAME LIVERIES:\n{GameStatus}";
                    }
                    else
                    {
                        ((Data)DataContext).Useable = true;
                    }
                }
                else
                {
                    lblMessage.Content = $"ERROR WHILE LOADING GAME LIVERIES, please ensure you:\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or create an issue on github";
                }
            }
            UpdateGameGui();

            if (Config.LibraryPath != "")
            {
                txtLibDir.Text = Config.LibraryPath;
                Library.Load();
            }

            UpdateLibraryGui();

            InfoCollectorThread.Start();

        }
        
        private void Close(object sender, CancelEventArgs e)
        {
            InfoCollectorThread.Abort();
        }
        private void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  Train Sim World 3 Livery Manager                  ║");
            Console.WriteLine("╟──────────────────────── by RagingLightning ────────────────────────╢");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Command Line Arguments:");
            Console.WriteLine(" -help / -? :");
            Console.WriteLine("    Show this help page");
            Console.WriteLine();
            Console.WriteLine(" -reset :");
            Console.WriteLine("    Resets all config options back to default");
            Console.WriteLine();
            Console.WriteLine(" -noInfoCollect <true|false> :");
            Console.WriteLine("    Toggle TSW3 Livery Info collection");
            Console.WriteLine();
            Console.WriteLine(" -noUpdate <true|false> :");
            Console.WriteLine("    Toggle automatic update check at startup");
            Console.WriteLine();
            Console.WriteLine(" -devUpdate <true|false> :");
            Console.WriteLine("    Toggle automatic dev-update check on startup");
            Console.WriteLine();
            Console.WriteLine(" -maxGameLiveries <count> :");
            Console.WriteLine("    Change the number of in-game liveries !!EXPERIMENTAL!!");
            Console.WriteLine("    any number less than 300 reverts back to the default setting of 300");
            Console.WriteLine();
            FreeConsole();
            Application.Current.Shutdown();
        }

        private void UpdateGameGui()
        {
            Log.AddLogMessage("Updating game liveries in GUI...", "MW:UpdateGameGui");
            lstGameLiveries.Items.Clear();
            for (int i = 1; i <= Config.MaxGameLiveries; i++)
            {
                string Text;
                if (Game.Liveries.Count <= i)
                    Text = $"({i}) <empty>";
                else
                {
                    string Id = Game.Liveries[i].ID;
                    GameLiveryInfo.Info Info = GameLiveryInfo.Get(Id);
                    Text = $"({i}) {Info.Name} for {Info.Model}";
                }
                lstGameLiveries.Items.Add(Text);

                Log.AddLogMessage($"Added game livery {Text}", "MW:UpdateGameGui", Log.LogLevel.DEBUG);
            }
            Log.AddLogMessage("Game liveries in GUI updated", "MW:UpdateGameGui", Log.LogLevel.DEBUG);
        }

        private void UpdateLibraryGui()
        {
            Log.AddLogMessage("Updating library liveries in GUI...", "MW:UpdateLibraryGui");
            lstLibraryLiveries.Items.Clear();
            foreach (int i in Library.Liveries.Keys)
            {
                Library.Livery livery = Library.Liveries[i];
                string Text = $"{livery.Name} for {livery.Model} <{livery.FileName}>";
                lstLibraryLiveries.Items.Add(Text);
                Log.AddLogMessage($"Added library livery {Text}", "MW:UpdateLibraryGui", Log.LogLevel.DEBUG);
            }

            Log.AddLogMessage("Library liveries in GUI updated", "MW:UpdateGameGui", Log.LogLevel.DEBUG);
        }
        private void ImportLivery(Library.Livery ll)
        {
            Log.AddLogMessage($"Importing livery from file {ll.FileName}", "MW:ImportLivery");
            string registeredId = GameLiveryInfo.SetInfo(ll.Id, ll.Name, ll.Model);
            if (registeredId != ll.Id)
            {
                Log.AddLogMessage($"ID {ll.Id} already in use; new ID is {registeredId}", "MW:ImportLivery", Log.LogLevel.DEBUG);
                ll.Id = registeredId;
                Library.Save(ll);
            }

            Game.Add(new Game.Livery(ll.GvasBaseProperty));
            Log.AddLogMessage($"Livery successfully imported (ID: {ll.Id}", "MW:ImportLivery", Log.LogLevel.DEBUG);
        }

        private void ExportLivery(Game.Livery gl)
        {
            Log.AddLogMessage($"Exporting livery {gl.ID}", "MW:ExportLivery");
            GameLiveryInfo.Info Info = GameLiveryInfo.Get(gl.ID);
            string fileName = Utils.SanitizeFileName($"{Info.Name} for {Info.Model}.tsw3");
            if (Info.Name == "<unnamed>" && Info.Model == "<unknown>")
            {
                Log.AddLogMessage($"Livery Info not set, asking for file name", "MW:ExportLivery", Log.LogLevel.DEBUG);
                SaveFileDialog Dialog = new SaveFileDialog();
                Dialog.InitialDirectory = Config.LibraryPath;
                Dialog.Filter = "TSW3 Livery File (*.tsw3)|*.tsw3";
                Dialog.DefaultExt = "*.tsw3";
                if (Dialog.ShowDialog() == true)
                    fileName = Utils.SanitizeFileName(Dialog.SafeFileName);
            }

            Library.Livery ll = new Library.Livery(fileName, gl.GvasBaseProperty, Info.Name, Info.Model);
            Library.Add(ll);
            Library.Save(ll);

            Log.AddLogMessage($"Livery successfully exported (ID: {ll.Id}", "MW:ImportLivery", Log.LogLevel.DEBUG);
        }

        private string DetermineWindowsStoreSaveFile()
        {
            string saveFilePath = Environment.GetEnvironmentVariable("LocalAppData") + "\\Packages";
            string containerFilePath;
            string pattern = "<n/a>";
            try
            {
                pattern = "DovetailGames.TrainSimWorld2021_*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                Log.AddLogMessage($"Found TrainSimWorld2021 package at '{saveFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.DEBUG);
                saveFilePath += "\\SystemAppData\\wgs";
                pattern = "*_*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                pattern = "*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                pattern = "container.*";
                containerFilePath = Directory.EnumerateFiles(saveFilePath, pattern).First();
            }
            catch (Exception)
            {
                Log.AddLogMessage($"couldn't find pattern '{pattern}' in '{saveFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.WARNING);
                return "";
            }
            Log.AddLogMessage($"container idx file is at '{containerFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.DEBUG);
            try
            {
                byte[] containerFile = File.ReadAllBytes(containerFilePath);

                byte[] key = new byte[] { 0x55, 0, 0x47, 00, 0x43, 00, 0x4c, 00, 0x69, 00, 0x76, 00, 0x65, 00, 0x72, 00, 0x69, 00, 0x65, 00, 0x73, 00, 0x5f, 00, 0x30, 00 };
                int start = Utils.LocateInByteArray(containerFile, key);
                int idx = start + key.Length;

                while (containerFile[idx] == 0) idx++;

                string fileBuilder = BitConverter.ToString(new byte[] { containerFile[idx + 3], containerFile[idx + 2], containerFile[idx + 1], containerFile[idx] });
                idx += 4;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx + 1], containerFile[idx] });
                idx += 2;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx + 1], containerFile[idx] });
                idx += 2;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++] });
                fileBuilder = fileBuilder.ToUpper().Replace("-", "");
                saveFilePath += "\\" + fileBuilder;
            }
            catch (Exception)
            {
                Log.AddLogMessage($"Unable to parse container idx file", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.WARNING);
                return "";
            }
            Log.AddLogMessage($"Liveries file is at '{saveFilePath}'");
            return saveFilePath;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1)
            {
                lblMessage.Content = "Before exporting, please ensure you:\n - Have a Game Livery selected";
                return;
            }

            lblMessage.Content = "";
            ExportLivery(Game.Liveries[lstGameLiveries.SelectedIndex]);
            UpdateLibraryGui();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibraryLiveries.SelectedItem == null || lstLibraryLiveries.SelectedIndex == -1)
            {
                lblMessage.Content = "Before importing, please ensure you:\n - Have an empty Game Livery slot selected\n - Have a Library Livery selected\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                return;
            }

            lblMessage.Content = "";
            ImportLivery(Library.Liveries[lstLibraryLiveries.SelectedIndex]);
            UpdateGameGui();
        }

        private void btnJExport_Click(object sender, RoutedEventArgs e)     //Reuse for TSW2 import maybe?
        {
            throw new NotImplementedException();
        }

        private void btnJImport_Click(object sender, RoutedEventArgs e)     //Reuse for TSW2 import maybe?
        {
            throw new NotImplementedException();
        }

        private void lstLibrary_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnLibDir_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
            Dialog.Description = "Select a folder for all your liveries to be exported to";
            if (Dialog.ShowDialog() == true)
            {
                Log.AddLogMessage("Changing library path...", "MW:LibDirClick", Log.LogLevel.DEBUG);
                Config.LibraryPath = Dialog.SelectedPath;
                txtLibDir.Text = Dialog.SelectedPath;
                Log.AddLogMessage($"Changed library path to {Config.LibraryPath}", "MW:LibDirClick");
            }
            Library.Load();
            UpdateLibraryGui();

            if (Config.LibraryPath != "" && Config.GamePath != "")
                ((Data)DataContext).Useable = true;
        }

        private void lstGame_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnGameDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblMessage.Content = "";
                VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
                Dialog.Description = "Select the TSW3 game folder";
                if (Dialog.ShowDialog() == true)
                {
                    Log.AddLogMessage("Changing game path...", "MW:GameDirClick", Log.LogLevel.DEBUG);
                    if (Dialog.SelectedPath.Contains("TrainSimWorld3WGDK"))
                    {
                        Log.AddLogMessage("Detected Windows store version", "MW:GameDirClick", Log.LogLevel.DEBUG);
                        Config.GamePath = DetermineWindowsStoreSaveFile();
                    }
                    else
                    {
                        Log.AddLogMessage("Detected Steam or epic store version", "MW:GameDirClick", Log.LogLevel.DEBUG);
                        Config.GamePath = $@"{Dialog.SelectedPath}\Saved\SaveGames\UGCLiveries_0.sav";
                    }
                    txtGameDir.Text = Dialog.SelectedPath;
                    Log.AddLogMessage($"Changed game path to {Config.GamePath}", "MW::GameDirClick");
                }
                Game.Load();
                UpdateGameGui();
            }
            catch (Exception ex)
            {
                Log.AddLogMessage($"Error while changing game folder: {ex.Message}", "MW:GameDirClick", Log.LogLevel.ERROR);
            }

            if (Config.LibraryPath != "" && Config.GamePath != "")
                ((Data)DataContext).Useable = true;
        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            SaveFileDialog Dialog = new SaveFileDialog();
            Dialog.InitialDirectory = Config.LibraryPath;
            Dialog.Filter = "TSW3 Livery Backup (*.bak3)|*.bak3";
            Dialog.DefaultExt = "*.bak3";
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Config.GamePath);
                File.WriteAllBytes(Dialog.FileName, Contents);
                Log.AddLogMessage($"Created backup: {Dialog.FileName}", "MW:BackupClick");
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "TSW2 Livery Backup (*.bak3)|*.bak3";
            Dialog.DefaultExt = "*.bak3";
            Dialog.InitialDirectory = Config.LibraryPath;
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Dialog.FileName);
                File.WriteAllBytes(Config.GamePath, Contents);
                Log.AddLogMessage($"Restored from backup: {Dialog.FileName}", "MW:RestoreClick");
            }
            Game.Load();
            UpdateGameGui();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Log.AddLogMessage("Saving local game liveries to disk...", "MW:SaveClick");
            lblMessage.Content = "";
            Game.Save();

            Game.Load();
            UpdateGameGui();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            Library.Load();
            Game.Load();
            UpdateLibraryGui();
            UpdateGameGui();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1)
            {
                Log.AddLogMessage($"Deleting game livery {lstGameLiveries.SelectedItem}...", "MW:DeleteClick");
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have a Game Livery selected\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                return;
            }
            Game.Liveries.Remove(lstGameLiveries.SelectedIndex);
            UpdateGameGui();
        }
    }

    public class Data : INotifyPropertyChanged
    {
        private bool _useable = false;
        public bool Useable
        {
            get { return _useable; }
            set { _useable = value; OnPropertyChanged("Useable"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
