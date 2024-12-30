using System;
using System.ComponentModel;

namespace LocalLibrary
{
    public class GameSourceOption : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        private bool _isPrimary;
        public bool IsPrimary
        {
            get => _isPrimary;
            set
            {
                _isPrimary = value;
                OnPropertyChanged(nameof(IsPrimary));
            }
        }

        public override string ToString() => Name;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? throw new ArgumentNullException(nameof(propertyName))));
        }

        public GameSourceOption(Guid id, string name, bool isPrimary)
        {
            Id = id;
            Name = name;
            IsPrimary = isPrimary;
        }
    }
}
