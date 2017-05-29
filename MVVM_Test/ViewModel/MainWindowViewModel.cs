using MVVM_Test.Model;
using System.Windows;
using System.Windows.Input;

namespace MVVM_Test.ViewModel
{
    class MainWindowViewModel
    {
        #region Constructor
        public MainWindowViewModel()
        {
            ClickCommand = new Command(arg => ClickMethod());
            People = new PeopleModel
            {
                FirstName = "First name",
                LastName = "Last name"
            };
        }
        #endregion

        #region Properties

        /// <summary>
        /// Get or set people.
        /// </summary>
        public PeopleModel People { get; set; }

        #endregion

        #region Commands
        /// <summary>
        /// Gets or sets ClickCommand
        /// </summary>
        public ICommand ClickCommand { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Click method
        /// </summary>
        private void ClickMethod()
        {
            MessageBox.Show("Hello mister fuzkoff");
        }
        #endregion
    }
}
