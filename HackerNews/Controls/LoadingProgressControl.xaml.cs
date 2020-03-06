using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace HackerNews
{
    public partial class LoadingProgressControl : UserControl
    {
        public LoadingProgressControl()
        {
            InitializeComponent();
        }

        internal Popup ChildWindowPopup
        {
            get;
            private set;
        }

        public void Show()
        {
            if (this.ChildWindowPopup == null)
            {
                this.ChildWindowPopup = new Popup();

                try
                {
                    this.ChildWindowPopup.Child = this;
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException("The control is already shown.");
                }
            }

            if (this.ChildWindowPopup != null && Application.Current.RootVisual != null)
            {
                this.ChildWindowPopup.IsOpen = true;
            }
        }

        public void Hide()
        {
            this.ChildWindowPopup.IsOpen = false;
        }
    }
}
