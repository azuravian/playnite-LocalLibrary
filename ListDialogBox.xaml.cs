using System.Collections.ObjectModel;
using System.Windows;

namespace LocalLibrary
{
    public partial class SelectionDialog : Window
    {
        public INamedItem SelectedItem { get; private set; }
        public bool IsCancelled { get; private set; } = true;

        public SelectionDialog(ObservableCollection<INamedItem> items)
        {
            InitializeComponent();
            OptionsListBox.ItemsSource = items;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = OptionsListBox.SelectedItem as INamedItem;
            IsCancelled = SelectedItem == null;
            DialogResult = true;
            Close();
        }

        private void NoneButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = null;
            IsCancelled = true;
            DialogResult = true;
            Close();
        }
    }
}