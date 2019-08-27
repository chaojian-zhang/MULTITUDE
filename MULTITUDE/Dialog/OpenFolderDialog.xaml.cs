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
using System.IO;

namespace MULTITUDE.Dialog
{
    /// <summary>
    /// 
    /// </summary>
    // Pending anim, async, autocompletion
    public partial class OpenFolderDialog : Window
    {
        public OpenFolderDialog(Window owner, string loadPath = null)
        {
            InitializeComponent();

            Owner = owner;

            // Check validity
            if (loadPath != null && System.IO.Directory.Exists(loadPath))
                CurrentDisplay = new DirectoryInfo(loadPath);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentDisplay != null)
            {
                LocationTextbox.Text = CurrentDisplay.FullName;
                UpdateFolderDisplay(new DirectoryInfo(LocationTextbox.Text));
            }
            else
            {
                // Use default display
                UpdateFolderDisplay();
            }
        }

        private DirectoryInfo CurrentDisplay { get; set; }
        // Window Return Valud
        public string ChosenDirectoryPath { get; set; }

        #region Core Logics
        // Pending async
        private void UpdateFolderDisplay(DirectoryInfo newDirectory = null)
        {
            PresentationDockPanel.Children.Clear();

            if(newDirectory != null)
            {
                LocationTextbox.Text = newDirectory.FullName;
                CurrentDisplay = newDirectory;
                // Show subdirectory
                DirectoryInfo[] folders = newDirectory.GetDirectories();
                foreach (DirectoryInfo folder in folders)
                {
                    string fullText = folder.Name;
                    string displayText = TruncateDisplayText(fullText);
                    GenerateAndAddButtons(displayText, fullText, folder);
                }
            }
            else
            {
                LocationTextbox.Text = "";
                // Show drives
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    string fullText = drive.Name + drive.VolumeLabel;
                    string displayText = TruncateDisplayText(fullText);
                    GenerateAndAddButtons(displayText, fullText, new DirectoryInfo(drive.Name));
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentDisplay != null)
            {
                UpdateFolderDisplay(CurrentDisplay.Parent);
            }
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateFolderDisplay((sender as Button).Tag as DirectoryInfo);
        }

        private void GenerateAndAddButtons(string displayText, string fullText, DirectoryInfo dir)
        {
            // Generate a new button
            Button newButton = new Button();
            newButton.Content = displayText;
            newButton.ToolTip = fullText;
            newButton.Style  = (Style)this.FindResource("CircularFolderButton");
            newButton.Tag = dir;
            // Attach Event Handler
            newButton.Click += FolderButton_Click;

            // Add to dock panel
            DockPanel.SetDock(newButton, Dock.Left);
            PresentationDockPanel.Children.Add(newButton);
        }

        private static readonly int DisplayLimit;
        private string TruncateDisplayText(string original)
        {
            if(original.Length >= 10)
            {
                return original.Substring(0, 10) + "...";
            }
            else
            {
                return original;
            }
        }

        private void LocationTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Autocomplete, pending
        }
        #endregion

        #region Window Interactions
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ChosenDirectoryPath = LocationTextbox.Text; 
            DialogResult = true;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Begin dragging the window
            this.DragMove();
        }
        #endregion

        #region Text UI Interaction
        private void LocationTextbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("ForegroundText");
            if (LocationTextbox.Text == App.DefaultTextboxText)
            {
                LocationTextbox.Text = "";
                LocationTextbox.Foreground = color;
            }
        }

        private void LocationTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateFromTextBox();
        }

        private void UpdateFromTextBox()
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("TransparentText");
            SolidColorBrush warningBorder = (SolidColorBrush)this.FindResource("ValidityWarningBorder");
            SolidColorBrush normalBorder = (SolidColorBrush)this.FindResource("DimTextBorder");
            if (LocationTextbox.Text == "")
            {
                LocationTextbox.Text = App.DefaultTextboxText;
                LocationTextbox.Foreground = color;
                LocationTextbox.BorderBrush = normalBorder;
            }
            else
            {
                // Verify directory leditity
                if (Directory.Exists(LocationTextbox.Text) == true)
                {
                    if (CurrentDisplay == null || LocationTextbox.Text != CurrentDisplay.FullName)
                    {
                        UpdateFolderDisplay(new DirectoryInfo(LocationTextbox.Text));
                    }
                }
                else
                {
                    LocationTextbox.ToolTip = "Invalid folder path.";
                    LocationTextbox.BorderBrush = warningBorder;
                }
            }
        }

        private void LocationTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Test for Enter key.
            if (e.Key == Key.Enter)
            {
                UpdateFromTextBox();
            }
        }
        #endregion
    }
}
