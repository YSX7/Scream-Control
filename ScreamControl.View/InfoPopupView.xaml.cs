using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreamControl.View
{
    /// <summary>
    /// Interaction logic for InfoPopupView.xaml
    /// </summary>
    public partial class InfoPopupView : Window
    {
        public InfoPopupView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           Storyboard anim = ((Storyboard)FindResource("animate"));
            anim.Completed += AnimationCompleted;
            anim.Begin(this);
        }

        private void AnimationCompleted(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
