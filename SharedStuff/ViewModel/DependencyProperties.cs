using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace ScreamControl.ViewModel
{
    //public class MouseBehaviour : System.Windows.Interactivity.Behavior<Border>
    //{
    //    public static readonly DependencyProperty MouseYProperty = DependencyProperty.Register(
    //        "MouseY", typeof(double), typeof(MouseBehaviour), new PropertyMetadata(default(double)));

    //    public double MouseY
    //    {
    //        get { return (double)GetValue(MouseYProperty); }
    //        set { SetValue(MouseYProperty, value); }
    //    }

    //    public static readonly DependencyProperty MouseXProperty = DependencyProperty.Register(
    //        "MouseX", typeof(double), typeof(MouseBehaviour), new PropertyMetadata(default(double)));

    //    public double MouseX
    //    {
    //        get { return (double)GetValue(MouseXProperty); }
    //        set { SetValue(MouseXProperty, value); }
    //    }

    //    protected override void OnAttached()
    //    {
    //        AssociatedObject.MouseMove += AssociatedObjectOnMouseMove;
    //    }

    //    private void AssociatedObjectOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
    //    {
    //        var pos = mouseEventArgs.GetPosition(AssociatedObject);
    //        MouseX = pos.X;
    //        MouseY = pos.Y;
    //    }

    //    protected override void OnDetaching()
    //    {
    //        AssociatedObject.MouseMove -= AssociatedObjectOnMouseMove;
    //    }
    //}

    public class CloseWindowBehavior : Behavior<Window>
    {
        public bool CloseTrigger
        {
            get { return (bool)GetValue(CloseTriggerProperty); }
            set { SetValue(CloseTriggerProperty, value); }
        }

        public static readonly DependencyProperty CloseTriggerProperty =
            DependencyProperty.Register("CloseTrigger", typeof(bool), typeof(CloseWindowBehavior), new PropertyMetadata(false, OnCloseTriggerChanged));


        private static void OnCloseTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as CloseWindowBehavior;

            if (behavior != null)
            {
                behavior.OnCloseTriggerChanged();
            }
        }

        private void OnCloseTriggerChanged()
        {
            //TODO: test this
            if (this.CloseTrigger)
            {
                this.AssociatedObject.Close();
            }
        }
    }
}
