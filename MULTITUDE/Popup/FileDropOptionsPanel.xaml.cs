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
    /// Interaction logic for FileDropOptionsPanel.xaml
    /// Lots of parameters here used are very specialized and artistically tweaked
    /// </summary>
    public partial class FileDropOptionsPanel : Window
    {
        // UI Configuration Properties
        public bool bFile { get; set; }
        public bool bFolder { get; set; }
        // Dialog Return Properties
        public ImportMode Mode { get; set; }
        public ImportAction Action { get; set; }

        // Constructor
        public FileDropOptionsPanel(bool bTargetFolder, Window owner)
        {
            bFile = !bTargetFolder;
            bFolder = bTargetFolder;
            Owner = owner;

            InitializeComponent();
        }

        // Automatic positining handling
        private void DropOptionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Point position = Mouse.GetPosition(Owner);
            this.Left = position.X - Width / 2;
            this.Top = position.Y - Height / 2;
        }

        // Window close handling: ESC and automatic
        private void DropOptionWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                DialogResult = false;
                this.Close();
            }
        }

        #region Button Handling
        private void Folder_Cut_Clue_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateClues;
            Action = ImportAction.Cut;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Cut_VA_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateVirtualArchive;
            Action = ImportAction.Cut;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Cut_A_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.UseAsArchive;
            Action = ImportAction.Cut;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Cut_C_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.NoClassification;
            Action = ImportAction.Cut;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Clone_Clue_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateClues;
            Action = ImportAction.Clone;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Clone_VA_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateVirtualArchive;
            Action = ImportAction.Clone;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Clone_A_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.UseAsArchive;
            Action = ImportAction.Clone;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Clone_C_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.NoClassification;
            Action = ImportAction.Clone;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Refer_Clue_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateClues;
            Action = ImportAction.Refer;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Refer_VA_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.GenerateVirtualArchive;
            Action = ImportAction.Refer;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Refer_A_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.UseAsArchive;
            Action = ImportAction.Refer;
            DialogResult = true;
            this.Close();
        }

        private void Folder_Refer_C_Option_Click(object sender, RoutedEventArgs e)
        {
            Mode = ImportMode.NoClassification;
            Action = ImportAction.Refer;
            DialogResult = true;
            this.Close();
        }

        private void File_Cut_Option_Click(object sender, RoutedEventArgs e)
        {
            Action = ImportAction.Cut;
            Mode = ImportMode.NoClassification;
            DialogResult = true;
            this.Close();
        }

        private void File_Clone_Option_Click(object sender, RoutedEventArgs e)
        {
            Action = ImportAction.Clone;
            Mode = ImportMode.NoClassification;
            DialogResult = true;
            this.Close();
        }

        private void File_Refer_Option_Click(object sender, RoutedEventArgs e)
        {
            Action = ImportAction.Refer;
            Mode = ImportMode.NoClassification;
            DialogResult = true;
            this.Close();
        }
        #endregion

        private void ActionCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
