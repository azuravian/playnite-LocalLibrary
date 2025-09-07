using LocalLibrary.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LocalLibrary.Helpers
{
    internal class Migration
    {
        public static ObservableCollection<ReplaceRule> ConvertOldLists(
            IEnumerable<string> regexList,
            IEnumerable<string> stringList)
        {
            var rules = new ObservableCollection<ReplaceRule>();

            if (stringList != null)
            {
                foreach (var s in stringList)
                {
                    rules.Add(new ReplaceRule
                    {
                        Type = "String",
                        Pattern = s,
                        Replacement = "" // stripping by default
                    });
                }
            }

            if (regexList != null)
            {
                foreach (var r in regexList)
                {
                    rules.Add(new ReplaceRule
                    {
                        Type = "Regex",
                        Pattern = r,
                        Replacement = "" // stripping by default
                    });
                }
            }

            return rules;
        }
    }
}