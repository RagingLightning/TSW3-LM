using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TSW3LM.Options;

namespace TSW3LM
{
    /// <summary>
    /// Interaktionslogik für OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        private Config.Entries localEntries;

        private readonly List<string> dirtyEntries = new();
        public OptionsWindow()
        {
            Owner = Application.Current.MainWindow;

            localEntries = Config.entries;

            MakeDataContext();

            InitializeComponent();

        }

        private void Discard(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (dirtyEntries.Contains("GamePath")) {
                if (localEntries.GamePath.Contains("TrainSimWorld3WGDK"))
                {
                    Log.Message("Detected Windows store version", "OW:Save", Log.LogLevel.DEBUG);
                    localEntries.GamePath = Utils.DetermineWindowsStoreSaveFile();
                }
                else
                {
                    Log.Message("Detected Steam or epic store version", "OW:Save", Log.LogLevel.DEBUG);
                    localEntries.GamePath = $@"{localEntries.GamePath}\Saved\SaveGames\UGCLiveries_0.sav";
                }
            }
            Config.entries = localEntries;
            Config.Save();
        }

        private void SetDefault(object sender, RoutedEventArgs e)
        {
            localEntries.ApplyDefaults();
            InvalidateVisual();
        }

        private void BooleanOptionChange(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton btn) return;
            var key = btn.GroupName;
#pragma warning disable CS8629
            var value = (btn.Content.ToString() == "true" && (bool)btn.IsChecked) || (btn.Content.ToString() == "false" && (bool)!btn.IsChecked);
#pragma warning restore CS8629

#pragma warning disable CS8602
            typeof(Config.Entries).GetField(key).SetValue(localEntries, value);
#pragma warning restore CS8602
            dirtyEntries.Add(key);
        }

        private void TextOptionChange(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.FindName("key") is not TextBlock keyBlock) return;
            string key = keyBlock.Text;

#pragma warning disable CS8602
            typeof(Config.Entries).GetField(key).SetValue(localEntries, box.Text);
#pragma warning restore CS8602
            dirtyEntries.Add(key);
        }

        private void NumberOptionChange(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.FindName("key") is not TextBlock keyBlock) return;
            string key = keyBlock.Text;

#pragma warning disable CS8602
            typeof(Config.Entries).GetField(key).SetValue(localEntries, int.Parse(box.Text));
#pragma warning restore CS8602
            dirtyEntries.Add(key);

        }

        private void FolderOptionChange(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.FindName("key") is not TextBlock keyBlock) return;
            string key = keyBlock.Text;
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
#pragma warning disable CS8602
                typeof(Config.Entries).GetField(key).SetValue(localEntries, dialog.SelectedPath);
#pragma warning restore CS8602
            }

            MakeDataContext();
            ((TextBlock)btn.FindName("path")).InvalidateProperty(TextBlock.TextProperty);
            dirtyEntries.Add(key);
        }

        private void FileOptionChange(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.FindName("key") is not TextBlock keyBlock) return;
            string key = keyBlock.Text;
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
#pragma warning disable CS8602
                typeof(Config.Entries).GetField(key).SetValue(localEntries, dialog.SelectedPath);
#pragma warning restore CS8602
            }

            MakeDataContext();
            ((TextBlock)btn.FindName("path")).InvalidateProperty(TextBlock.TextProperty);
            dirtyEntries.Add(key);
        }

        private void MakeDataContext()
        {
            DataContext = new GroupOption
            {
                SubItems =
                {
                    new GroupOption
                    {
                        Type = Option.OptionType.GROUP0,
                        Name = "Files & Folders",
                        SubItems =
                        {
                            new FolderOption
                            {
                                OptionsID = "GamePath",
                                Name = "Game Folder",
                                Value = localEntries.GamePath,
                                Default = Config.Entries.DEFAULTS["GamePath"]
                            },
                            new FolderOption
                            {
                                OptionsID = "LibraryPath",
                                Name = "Library Folder",
                                Value = localEntries.LibraryPath,
                                Default = Config.Entries.DEFAULTS["LibraryPath"]
                            }
                        }
                    },
                    new GroupOption
                    {
                        Type = Option.OptionType.GROUP0,
                        Name = "Functionality",
                        SubItems =
                        {
                            new NumberOption
                            {
                                OptionsID = "MaxGameLiveries",
                                Name = "Maximum Number of liveries",
                                Value = localEntries.MaxGameLiveries,
                                Default = Config.Entries.DEFAULTS["MaxGameLiveries"]
                            }
                        }
                    },
                    new GroupOption
                    {
                        Type = Option.OptionType.GROUP0,
                        Name = "Updates",
                        SubItems =
                        {
                            new BooleanOption
                            {
                                OptionsID = "AutoUpdate",
                                Name = "Check for Updates",
                                Value = localEntries.AutoUpdate,
                                Default = Config.Entries.DEFAULTS["AutoUpdate"]
                            },
                            new BooleanOption
                            {
                                OptionsID = "DevUpdates",
                                Name = "Include pre-release Versions",
                                Value = localEntries.DevUpdates,
                                Default = Config.Entries.DEFAULTS["DevUpdates"]
                            }
                        }
                    }
                }

            };
        }
    }

}
