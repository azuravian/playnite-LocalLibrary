using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace LocalLibrary.Models
{
    public class MetadataExtractionRule : ObservableObject
    {
        private string _openDelimiter = "";
        public string OpenDelimiter 
        { 
            get => _openDelimiter; 
            set => SetValue(ref _openDelimiter, value); 
        }

        private string _closeDelimiter = "";
        public string CloseDelimiter 
        { 
            get => _closeDelimiter; 
            set => SetValue(ref _closeDelimiter, value); 
        }

        private string _metadataType = "";
        public string MetadataType 
        { 
            get => _metadataType; 
            set => SetValue(ref _metadataType, value); 
        }

        private string _pattern = "";
        public string Pattern 
        { 
            get => _pattern; 
            set => SetValue(ref _pattern, value);
        }

        private bool _append = false;
        public bool Append 
        { 
            get => _append; 
            set => SetValue(ref _append, value);
        }

        public MetadataExtractionRule()
        {
        }

        public MetadataExtractionRule(string openDelimiter, string closeDelimiter, string metadataType, string pattern, bool append)
        {
            OpenDelimiter = openDelimiter;
            CloseDelimiter = closeDelimiter;
            MetadataType = metadataType;
            Pattern = pattern;
            Append = append;
        }
    }
}
