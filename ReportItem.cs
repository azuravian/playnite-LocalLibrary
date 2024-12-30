using System;

namespace LocalLibrary
{
    public class ReportItem
    {
        private string _name = String.Empty; // Name of game
        private bool _new = false; // New game
        private bool _updates = false; // Updates available
        private int _updatesCount = 0; // Number of updates available

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value), "Name cannot be null or empty.");
                }
                _name = value;
            }
        }

        public bool New
        {
            get => _new;
            set
            {
                _new = value;
            }
        }

        public bool Updates
        {
            get => _updates;
            set
            {
                _updates = value;
            }
        }

        public int UpdatesCount
        {
            get => _updatesCount;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Updates count cannot be negative.");
                }
                _updatesCount = value;
            }
        }

        public ReportItem(string name, bool isNew, bool hasUpdates, int updatesCount)
        {
            Name = name;
            New = isNew;
            Updates = hasUpdates;
            UpdatesCount = updatesCount;
        }
    }
}
