using System;
using ScreamControl.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        protected void RaisePropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        //public MainModel mainModel;

        #region Constructor
        public MainViewModel()
        {
            LoadedCommand = new Command(arg => LoadedMethod());

            //  this.mainModel = new MainModel();

            CurrentLanguage = App.Language;
        }
        #endregion


        public CultureInfo CurrentLanguage { get; private set; }
        public Command LoadedCommand { get; private set; }

        private void LoadedMethod()
        {
            throw new NotImplementedException();
        }
    }
}
