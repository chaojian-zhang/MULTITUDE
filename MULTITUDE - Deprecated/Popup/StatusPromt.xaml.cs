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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Popup
{
    /// <summary>
    /// A Paragon like promt appearing in corner of application showing updates and optionally disappear after some time
    /// The promt will be always on top
    /// We support three kinds of updates: List view, Simple string with image, delegate actions, progress circle with string (a form of string + animation but not generalized for other animations)
    /// Currently used for: Moving files status, loading user home status, loading VW status, automatic saving status
    /// </summary>
    public partial class StatusPromt : Window
    {
        public StatusPromt()
        {
            InitializeComponent();
        }
    }
}
