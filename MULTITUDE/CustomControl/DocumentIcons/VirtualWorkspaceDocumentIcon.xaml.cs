﻿using MULTITUDE.Class.DocumentTypes;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for VirtualWorkspaceDocumentIcon.xaml
    /// </summary>
    public partial class VirtualWorkspaceDocumentIcon : UserControl
    {
        internal VirtualWorkspaceDocumentIcon(Document doc)
        {
            InitializeComponent();
        }
    }
}
