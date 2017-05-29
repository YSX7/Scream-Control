using MVVM_Test.ViewModel;
using MVVM_Test.View;
using System.Windows;

namespace MVVM_Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var mw = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
             mw.Show();
        }

    }
}
