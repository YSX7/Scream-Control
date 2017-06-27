using Octokit;
using ScreamControl;
using ScreamControl.View;
using ScreamControl.Client.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.IO.Compression;

namespace ScreamControl.Client
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
      //  #if !DEBUG
            GetUpdates();
    //    #endif
            m_Languages.Clear();
            m_Languages.Add(new CultureInfo("en-US"));
            m_Languages.Add(new CultureInfo("ru-RU"));

           App.LanguageChanged += App_LanguageChanged;
        }

        private async void GetUpdates()
        {
            if (_isUpdateUpdater)
            {
                ZipArchive za = ZipFile.OpenRead("temp.zip");
                var updaterFiles = za.Entries.Where(element => element.Name.ToLower().Contains("updater"));
                foreach (ZipArchiveEntry file in updaterFiles)
                {
                    if (file.Name == "")
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(file.FullName));
                        continue;
                    }
                    file.ExtractToFile(file.FullName, true);
                }
                za.Dispose();
            }
            try
            {
                Trace.TraceInformation("Updates check");
                var silentArgument = ScreamControl.Client.Properties.Settings.Default.IsStealthMode ? " " + "s" : "";
                var client = new GitHubClient(new ProductHeaderValue("Scream-Control"));
                var latest = await client.Repository.Release.GetLatest("YSXrus", "Scream-Control");
                var version = new ExtendedVersion(latest.TagName);
                bool updateAvailable = version > App.Version;
                Trace.TraceInformation("Updates available: {0}", updateAvailable.ToString());
                string appType = ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title.Split(' ')[1];
                string updateUrl = latest.Assets.First(element => element.Name.ToLower().Contains(appType.ToLower())).BrowserDownloadUrl;
                if (updateAvailable && File.Exists("Updater.exe"))
                {
                    Trace.TraceInformation("Go for updates");
                    System.Diagnostics.Process.Start("Updater.exe", updateUrl + " " + System.AppDomain.CurrentDomain.FriendlyName + silentArgument + " " + _isUpdateUpdater);
                    this.Shutdown();
                }
            }
            catch (Octokit.NotFoundException ex)
            {
                Trace.TraceWarning("No updates found: {0}", ex.Message);
            }
            catch(InvalidOperationException ex)
            {
                Trace.TraceWarning("No updates found: {0}", ex);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Something happend when checking: {0}", ex);
                //no updates
            }
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
                ResourceDictionary oldDict = (from d in System.Windows.Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.Contains("Language/lang.")
                                              select d).FirstOrDefault();
                if (oldDict != null)
                {
                    int ind = System.Windows.Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    System.Windows.Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                //4. Вызываем евент для оповещения всех окон.
                if(LanguageChanged!=null)
                     LanguageChanged(System.Windows.Application.Current, new EventArgs());
            }
            
        }

        private void App_LanguageChanged(Object sender, EventArgs e)
        {
            ScreamControl.Client.Properties.Settings.Default.CurrentLanguage = Language;
            ScreamControl.Client.Properties.Settings.Default.Save();
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Language = ScreamControl.Client.Properties.Settings.Default.CurrentLanguage;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
                _isUpdateUpdater = Convert.ToBoolean(e.Args[0]);

            Language = ScreamControl.Client.Properties.Settings.Default.CurrentLanguage;
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
