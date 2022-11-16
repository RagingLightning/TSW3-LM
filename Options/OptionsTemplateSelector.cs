using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TSW3LM.Options
{
    internal class OptionsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is Option) {

                Option optionItem = item as Option;

                switch (optionItem.Type)
                {
                    case Option.OptionType.BOOL: return element.FindResource("booleanOptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.TEXT: return element.FindResource("textOptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.NUMBER: return element.FindResource("numberOptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.FILE: return element.FindResource("fileOptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.FOLDER: return element.FindResource("folderOptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.GROUP0: return element.FindResource("group0OptionTemplate") as HierarchicalDataTemplate;
                    case Option.OptionType.GROUP1: return element.FindResource("group1OptionTemplate") as HierarchicalDataTemplate;
                }
            }
            return null;
        }
    }

    internal abstract class Option
    {
        public string OptionsID { get; set; } = string.Empty;
        public OptionType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Default { get; set; } = string.Empty;

        public enum OptionType
        {
            BOOL,
            TEXT,
            NUMBER,
            FILE,
            FOLDER,
            GROUP0,
            GROUP1
        }
    }

    internal class BooleanOption : Option
    {
        public bool Value { get; set; } = false;
        public bool NotValue { get => !Value; set => Value = !value; }
        internal BooleanOption() { Type = OptionType.BOOL; }
    }

    internal class TextOption : Option
    {
        public string Value { get; set; } = string.Empty;
        internal TextOption() { Type = OptionType.TEXT; }
    }

    internal class NumberOption: Option
    {
        public int Value { get; set; } = 0;
        internal NumberOption() { Type = OptionType.NUMBER; }
    }

    internal class FileOption : Option
    {
        public string Value { get; set; } = string.Empty;
        internal FileOption() { Type = OptionType.FILE; }
    }

    internal class FolderOption : Option
    {
        public string Value { get; set; } = string.Empty;
        internal FolderOption() { Type = OptionType.FOLDER; }
    }

    internal class GroupOption : Option
    {
        public string Desc { get; set; } = string.Empty;
        public List<Option> SubItems { get; set; } = new List<Option>();
        internal GroupOption() { }
    }
}
