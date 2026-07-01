using System.Collections.Specialized;
using System.Windows;

namespace ChatApp.Client.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (MessagesListBox.ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += (_, _) =>
            {
                if (MessagesListBox.Items.Count > 0)
                    MessagesListBox.ScrollIntoView(
                        MessagesListBox.Items[^1]);
            };
        }
    }
}
