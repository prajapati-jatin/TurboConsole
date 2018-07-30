using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole
{
    public static class Extensions
    {
        public static Boolean IsNull(this object obj)
        {
            return obj == null;
        }

        public static void Edit(this Item item, Action<ItemEditArgs> action)
        {
            var args = new ItemEditArgs();
            try
            {
                var wasEditing = item.Editing.IsEditing;
                if (!wasEditing)
                {
                    item.Editing.BeginEdit();
                }
                action(args);
                if (!wasEditing)
                {
                    if (args.Save)
                    {
                        item.Editing.EndEdit(args.UpdateStatistics, args.Silent);
                    }
                    else
                    {
                        item.Editing.CancelEdit();
                    }
                }
            }
            catch
            {
                if (args.SaveOnError)
                {
                    item.Editing.EndEdit(args.UpdateStatistics, args.Silent);
                }
                else
                {
                    item.Editing.CancelEdit();
                }
                throw;
            }
        }
    }

    public static class StringExtensions
    {
        public static String IfNullOrEmpty(this String value, String useIfEmpty) => String.IsNullOrEmpty(value) ? useIfEmpty : value;
    }

    public class ItemEditArgs
    {
        public bool UpdateStatistics { get; set; } = true;

        public bool Silent { get; set; }

        public bool Save { get; set; } = true;

        public bool SaveOnError { get; set; }
    }
}