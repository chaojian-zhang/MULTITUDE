using MULTITUDE.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Dialog
{
    /// <summary>
    /// A dialog window that enables user to create select a list of locations as user home locations for management;
    /// And configure login information for notes
    /// Data bound, automatically update
    /// </summary>
    public partial class UserConfigurationDialog : Window
    {
        public UserConfigurationDialog(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        // https://stackoverflow.com/questions/6794274/setting-label-text-in-xaml-to-string-constant
        // https://stackoverflow.com/questions/17025601/the-name-viewmodel-does-not-exist-in-the-namespace-clr-namespaceproject-viewmo -- Public and public parameterless constructor
        public static readonly string HomeDefaultText = "Enter location here";
        public static readonly string AppellationDefaultText = "Enter name here";
        public static readonly string UserIDDefaultText = "Enter user ID here";
        public static readonly string PasswordDefaultText = "Enter password here";
        public static readonly string WallpaperDefaultText = MULTITUDE.Class.VirtualWorkspace.DefaultBackgroundImageClue;
        public static readonly string RhythmDefaultText = MULTITUDE.Class.VirtualWorkspace.DefaultBackgroundMelodyClue;

        #region Textbox UI Interaction
        private void HomeLocationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("TransparentText");
            if (HomeLocationTextBox.Text == "")
            {
                HomeLocationTextBox.Text = HomeDefaultText;
                HomeLocationTextBox.Foreground = color;
            }
        }

        private void UserIDTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("TransparentText");

            if (UserIDTextBox.Text == "")
            {
                UserIDTextBox.Text = UserIDDefaultText;
                UserIDTextBox.Foreground = color;
            }
        }

        private void PasswordTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("TransparentText");
            if (PasswordTextBox.Password == "")
            {
                PasswordTextBox.Password = PasswordDefaultText;
                PasswordTextBox.Foreground = color;
            }
        }

        private void AppellationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("TransparentText");
            if (AppellationTextBox.Text == "")
            {
                AppellationTextBox.Text = AppellationDefaultText;
                AppellationTextBox.Foreground = color;
            }
        }

        private void AppellationTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("ForegroundText");
            if (AppellationTextBox.Text == AppellationDefaultText)
            {
                AppellationTextBox.Text = "";
                AppellationTextBox.Foreground = color;
            }
        }

        private void HomeLocationTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("ForegroundText");
            if (HomeLocationTextBox.Text == HomeDefaultText)
            {
                HomeLocationTextBox.Text = "";
                HomeLocationTextBox.Foreground = color;
            }
        }

        private void UserIDTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("ForegroundText");
            if (UserIDTextBox.Text == UserIDDefaultText)
            {
                UserIDTextBox.Text = "";
                UserIDTextBox.Foreground = color;
            }
        }

        private void PasswordTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush color = (SolidColorBrush)this.FindResource("ForegroundText");
            if (PasswordTextBox.Password == PasswordDefaultText)
            {
                PasswordTextBox.Password = "";
                PasswordTextBox.Foreground = color;
            }
        }
        
        private string PrevWallpaperText = WallpaperDefaultText;
        private void WallpaperTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WallpaperTextbox.Text.IndexOf(WallpaperDefaultText) == -1)
            {
                WallpaperTextbox.Text = PrevWallpaperText;
                WallpaperTextbox.CaretIndex = PrevWallpaperText.Length;
            }
            else
                PrevWallpaperText = WallpaperTextbox.Text;
        }

        private string PrevRhythmText = RhythmDefaultText;
        private void RhythmTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RhythmTextbox.Text.IndexOf(RhythmDefaultText) == -1)
            {
                RhythmTextbox.Text = RhythmDefaultText;
                RhythmTextbox.CaretIndex = RhythmDefaultText.Length;
            }
            else
                PrevRhythmText = RhythmTextbox.Text;
        }
        #endregion

        // textChanged: // Validate inputs: any one of the user home locations should be empty or non-existing folders; Otherwise check whether other MULTITUDE profiles exist there and ask User whether we should manage them as well

        // Major event handler

        #region Overall UI
        // Initialize according to state
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set home
            MULTITUDE.Class.Home home = (App.Current as App).CurrentHome;
            HomeLocationTextBox.Text = MULTITUDE.Class.Home.Location;
            if(home.Appellation != null) AppellationTextBox.Text = home.Appellation;
            if (home.Username != null) UserIDTextBox.Text = home.Username;
            if(home.Password != null) PasswordTextBox.Password = home.Password.Content;

            // Set VW
            MULTITUDE.Class.VirtualWorkspace vw = (Owner as VirtualWorkspaceWindow).CurrentState;
            WallpaperTextbox.Text = vw.BackgroundImageClue;
            RhythmTextbox.Text = vw.BackgroundMelodyClue;

            // ... Other settings loading..
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            (Owner as MULTITUDE.Canvas.VirtualWorkspaceWindow).PrevOpenedConfigDialog = null;
            this.Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Begin dragging the window
            this.DragMove();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Open an aura first
            ModalDialogAura modalAura = new ModalDialogAura(this);
            modalAura.Show();

            string CurrentFolderPath = null;
            if (HomeLocationTextBox.Text != HomeDefaultText) CurrentFolderPath = HomeLocationTextBox.Text;
            OpenFolderDialog dialog = new OpenFolderDialog(this, CurrentFolderPath);
            if (dialog.ShowDialog() == true)
            {
                HomeLocationTextBox.Text = dialog.ChosenDirectoryPath;

                // Disable all other controls
                // ...
            }
            else
            {
                // Nothing happens
            }

            modalAura.Close();
        }

        // Notice we need to do all those here because we are not a modal dialog but a popup window
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Check non-empty
            if (System.IO.Directory.GetFileSystemEntries(HomeLocationTextBox.Text).Length != 0 && System.IO.File.Exists(HomeLocationTextBox.Text + MULTITUDE.Class.Home.HomeDataFileName) == false)
            {
                // Warn
                HomeLocationTextBox.BorderBrush = Brushes.Red;
                HomeLocationTextBox.Text = "New home folder must be empty.";
            }
            else
	        {
                App app = (App.Current as App);

                // Save user configured information
                app.CurrentHome.ChangeConfigurations(AppellationTextBox.Text == AppellationDefaultText ? null : AppellationTextBox.Text,
                    UserIDTextBox.Text == UserIDDefaultText ? null : UserIDTextBox.Text,
                    PasswordTextBox.Password == PasswordDefaultText ? null : PasswordTextBox.Password);

                // Respond to VW Settings
                (app.MainWindow as MULTITUDE.Canvas.VirtualWorkspaceWindow).ChangeConfigurations(WallpaperTextbox.Text, RhythmTextbox.Text, new List<MULTITUDE.Class.GadgetType>());

                // if current home has changed, reload it
                if (HomeLocationTextBox.Text != MULTITUDE.Class.Home.Location)
                {
                    // Reload home
                    app.ReloadHomedata(HomeLocationTextBox.Text);
                }

            // Respond to Windows Settings
            // ....

            (Owner as MULTITUDE.Canvas.VirtualWorkspaceWindow).PrevOpenedConfigDialog = null;
                this.Close();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Open an aura first
            ModalDialogAura modalAura = new ModalDialogAura(this);
            modalAura.Show();

            // Show desktop folder path
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            OpenFolderDialog dialog = new OpenFolderDialog(this, path);
            if (dialog.ShowDialog() == true)
            {
                path = dialog.ChosenDirectoryPath;

                // Disable all other controls
                MULTITUDE.Class.Home.Current.PackAndExport(path);
            }
            else
            {
                // Nothing happens
            }

            modalAura.Close();
        }
        #endregion
    }
}
