using MVVM_Test.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScreamControl_Client.ViewModel
{
    class MainViewModel
    {
        #region Constructor
        public MainViewModel()
        {
            #region Commands
            LoadedCommand = new Command(arg => LoadedMethod());
            #endregion

            CurrentLanguage = App.Language;
            Languages = new ObservableCollection<CultureInfo>(App.Languages);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set Stealth Mode bool
        /// </summary>
        public bool IsStealthMode
        {
            get
            {
                return Properties.Settings.Default.StealthMode;
            }
            set
            {
                Properties.Settings.Default.StealthMode = value;
            }
        }

        /// <summary>
        /// Get or set window visibility, based on IsStealthMode property
        /// </summary>
        public Visibility WindowVisibilityState
        {
            get
            {
                if (IsStealthMode)
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;

            }

            set { WindowVisibilityState = value; }
        }

        /// <summary>
        /// Get or set available languages
        /// </summary>
        public ObservableCollection<CultureInfo> Languages { get; set; }

        /// <summary>
        /// Get or set selected language
        /// </summary>
        public CultureInfo CurrentLanguage
        { get { return App.Language; }
          set { App.Language = value; } }
        #endregion

        #region Commands
        public ICommand LoadedCommand { get; set; }
        #endregion

        #region Methods
        private void LoadedMethod()
        {
            //MessageBox.Show("Hi");
        }
        #endregion

        #region Helper methods

        #endregion
    }
}
