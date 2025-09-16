using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalLibrary.Models
{
    public class ReplaceRule : ObservableObject
    {
        private string _type;
        public string Type
        {
            get => _type;
            set => SetValue(ref _type, value);
        }

        private string _pattern;
        public string Pattern
        {
            get => _pattern;
            set => SetValue(ref _pattern, value);
        }

        private string _replacement;
        public string Replacement
        {
            get => _replacement;
            set => SetValue(ref _replacement, value);
        }
    }

}
