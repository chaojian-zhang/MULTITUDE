using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace MULTITUDE.Class.Facility
{
    class SystemHelper
    {
        #region General Data
        public static string CurrentTimeFileNameFriendly
        {
            get
            {
                return System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss");
            }
        }
        public static string CurrentTimeReaderFriendly
        {
            get
            {
                return System.DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss");
            }
        }
        #endregion

        #region File System
        public static bool? IsFolder(string path)
        {
            if (Directory.Exists(path)) return true;
            else if (File.Exists(path)) return false;
            else return null;
        }
        #endregion

        //#region System Volume Control
        //private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        //private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        //private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        //private const int WM_APPCOMMAND = 0x319;

        //IntPtr handle = new WindowInteropHelper(this).Handle;

        //[DllImport("user32.dll")]
        //public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
        //    IntPtr wParam, IntPtr lParam);

        //private void Mute()
        //{
        //    SendMessageW(handle, WM_APPCOMMAND, handle,
        //        (IntPtr)APPCOMMAND_VOLUME_MUTE);
        //}

        //private void VolDown()
        //{
        //    SendMessageW(handle, WM_APPCOMMAND, handle,
        //        (IntPtr)APPCOMMAND_VOLUME_DOWN);
        //}

        //private void VolUp()
        //{
        //    SendMessageW(handle, WM_APPCOMMAND, handle,
        //        (IntPtr)APPCOMMAND_VOLUME_UP);
        //}
        //#endregion
        //// Reference: https://stackoverflow.com/questions/13139181/how-to-programmatically-set-the-system-volume; Also checkout nuget CoreAudio APi

        #region Keyboard Interaction
        // A more elaborated way to get char from key: https://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c, https://stackoverflow.com/questions/318777/c-sharp-how-to-translate-virtual-keycode-to-char
        // Supports only english characters and numbers, with upper/lower case
        public static string GetCharFromKey_SimulatedKeyboard(Key key)
        {
            string c = string.Empty;
            switch (key)
            {
                case Key.A:
                case Key.B:
                case Key.C:
                case Key.D:
                case Key.E:
                case Key.F:
                case Key.G:
                case Key.H:
                case Key.I:
                case Key.J:
                case Key.K:
                case Key.L:
                case Key.M:
                case Key.N:
                case Key.O:
                case Key.P:
                case Key.Q:
                case Key.R:
                case Key.S:
                case Key.T:
                case Key.U:
                case Key.V:
                case Key.W:
                case Key.X:
                case Key.Y:
                case Key.Z:
                    c = key.ToString().ToLower();
                    break;
                case Key.D0:
                case Key.NumPad0:
                    c = "0";
                    break;
                case Key.NumPad1:
                case Key.D1:
                    c = "1";
                    break;
                case Key.NumPad2:
                case Key.D2:
                    c = "2";
                    break;
                case Key.NumPad3:
                case Key.D3:
                    c = "3";
                    break;
                case Key.NumPad4:
                case Key.D4:
                    c = "4";
                    break;
                case Key.NumPad5:
                case Key.D5:
                    c = "5";
                    break;
                case Key.NumPad6:
                case Key.D6:
                    c = "6";
                    break;
                case Key.NumPad7:
                case Key.D7:
                    c = "7";
                    break;
                case Key.NumPad8:
                case Key.D8:
                    c = "8";
                    break;
                case Key.NumPad9:
                case Key.D9:
                    c = "9";
                    break;
            }

            if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                c = c.ToUpper();
            }

            return c;
        }
        // To find out status about lock keys: https://stackoverflow.com/questions/577411/how-can-i-find-the-state-of-numlock-capslock-and-scrolllock-in-net
        #endregion
    }
}
