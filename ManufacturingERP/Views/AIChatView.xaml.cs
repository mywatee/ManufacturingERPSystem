using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ManufacturingERP.Views;

public partial class AIChatView : UserControl
{
    private bool _autoScroll = true;

    public AIChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.AIChatViewModel vm)
        {
            vm.Messages.CollectionChanged += (_, _) =>
            {
                Dispatcher.InvokeAsync(ScrollToBottom, DispatcherPriority.Background);
            };
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.IsOpen) && vm.IsOpen)
                    Dispatcher.InvokeAsync(ScrollToBottom, DispatcherPriority.Background);
            };
        }

        MessageScrollViewer.ScrollChanged += (s, args) =>
        {
            if (args.ExtentHeightChange == 0 && s is ScrollViewer sv)
                _autoScroll = sv.ScrollableHeight - sv.VerticalOffset < 30;
        };
    }

    private void ScrollToBottom()
    {
        if (_autoScroll && MessageScrollViewer != null)
            MessageScrollViewer.ScrollToBottom();
    }
}
