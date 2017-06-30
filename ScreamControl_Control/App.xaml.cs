using ScreamControl.Controller.ViewModel;
using ScreamControl.View;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ScreamControl.Controller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static List<CultureInfo> m_Languages = new List<CultureInfo>();
        private static bool _isUpdateUpdater = false;

        internal static ExtendedVersion Version
        {
            get
            {
                Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return new ExtendedVersion(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
            }
        }

        public static List<CultureInfo> Languages
        {
            get
            {
                return m_Languages;
            }
        }

        public App()
        {
            ChangeLogFile();
            Trace.TraceInformation("Scream Control started");
#if !DEBUG
            var checkUpdates = CheckUpdates.Check(App.Version, _isUpdateUpdater, ScreamControl.Controller.Properties.Settings.Default.IsStealthMode);
            checkUpdates.Wait();
            if (!checkUpdates.Result)
                this.Shutdown();
#endif
            m_Languages.Clear();
            m_Languages.Add(new CultureInfo("en-US"));
            m_Languages.Add(new CultureInfo("ru-RU"));

            App.LanguageChanged += App_LanguageChanged;
        }
        public static event EventHandler LanguageChanged;

        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

                //1. Меняем язык приложения:
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                //2. Создаём ResourceDictionary для новой культуры
                ResourceDictionary dict = new ResourceDictionary();

                dict.Source = new Uri(String.Format("pack://application:,,,/ScreamControl.View;component/Language/lang.{0}.xaml", value.Name), UriKind.RelativeOrAbsolute);

                //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                ResourceDictionary oldDict = (from d in App.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.StartsWith("Language/lang.")
                                              select d).FirstOrDefault();

                dict.Source = new Uri(String.Format("pack://application:,,,/ScreamControl.View;component/Language/lang.{0}.xaml", value.Name), UriKind.RelativeOrAbsolute);

                //4. Вызываем евент для оповещения всех окон.
                if (LanguageChanged!=null)
                     LanguageChanged(App.Current, new EventArgs());
            }
        }

        private void App_LanguageChanged(Object sender, EventArgs e)
        {
            ScreamControl.Controller.Properties.Settings.Default.CurrentLanguage = Language;
            ScreamControl.Controller.Properties.Settings.Default.Save();
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Language = ScreamControl.Controller.Properties.Settings.Default.CurrentLanguage;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(e.Args.Length > 0)
                _isUpdateUpdater = Convert.ToBoolean(e.Args[0]);

            Language = ScreamControl.Controller.Properties.Settings.Default.CurrentLanguage;
            MainWindow window = new MainWindow();
            window.DataContext = new MainViewModel();
            window.Show();
        }

        private void ChangeLogFile()
        {
            if (!File.Exists("log.txt"))
                File.Create("log.txt").Close();
            if (File.Exists("log-prev.txt"))
                File.Delete("log-prev.txt");
            File.Move("log.txt", "log-prev.txt");
            Trace.TraceInformation("Created at {0}", DateTime.Now.ToString());
        }

    }
}
