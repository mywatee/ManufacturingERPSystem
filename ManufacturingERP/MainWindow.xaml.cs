using System.Windows;
using System.Windows.Input;
using ManufacturingERP.ViewModels;

namespace ManufacturingERP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChatIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.AIChat != null)
                vm.AIChat.IsOpen = !vm.AIChat.IsOpen;
        }
    }
}