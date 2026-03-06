using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatDemo.Wpf.ViewModels;

namespace ChatDemo.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll to new messages
        viewModel.Messages.CollectionChanged += (_, _) =>
            Dispatcher.InvokeAsync(() =>
            {
                if (MessagesScroll is not null)
                    MessagesScroll.ScrollToBottom();
            });
    }

    private void ContactItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm &&
            sender is ListBoxItem { DataContext: Models.Contact contact })
        {
            vm.SelectContactCommand.Execute(contact);
        }
    }

    private void MessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift)
                               && !Keyboard.IsKeyDown(Key.RightShift))
        {
            if (DataContext is MainViewModel vm)
                vm.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Cleanup();
        base.OnClosed(e);
    }
}
