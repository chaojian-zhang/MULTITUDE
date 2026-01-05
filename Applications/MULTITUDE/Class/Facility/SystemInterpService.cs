using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    class SystemInterpService
    {
        #region Show File Properties
        // Ref - Open Folder/File properties window: http://stackoverflow.com/questions/1936682/how-do-i-display-a-files-properties-dialog-from-c
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        public static bool ShowFileProperties(string Filename)  // Filename can be a file or a folder
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }
        #endregion

        #region Reistration for Custom file type
        // Usage: if (!IsAssociated(".ext")) Associate(".ext", "ClassID.ProgID", "ext File", "YourIcon.ico", "YourApplication.exe");
        // Usage: string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData; then if it's not null and length is bigger than 0, use Uri uri = new Uri(activationData[0]); path = uri.LocalPath;
        // Usage: We can save it like this - (App)this.Properties["Login"] = (string)fname; for later retrival
        public static void RegisterCustomFileType()
        {
            // <Debug> Method 1
            // if (!IsAssociated(".home")) Associate(".home", "MULTITUDE.MULTITUDEHOME", "home File", @"C:\Users\szinu\Desktop\TestIcon.ico", System.Reflection.Assembly.GetExecutingAssembly().Location);

            // <Debug> Method 2
            // FileRegistrationHelper.SetFileAssociation(".home", "MULTITUDE.MULTITUDE home file");
        }
        // Path REf:https://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-a-net-console-application; Notice GetExecutingAssembly().CodeBase doesn't work https://stackoverflow.com/questions/4107625/how-can-i-convert-assembly-codebase-into-a-filesystem-path-in-c

        // Associate file extension with progID, description, icon and application
        public static void Associate(string extension,
               string progID, string description, string icon, string application)
        {
            Registry.ClassesRoot.CreateSubKey(extension).SetValue("", progID);
            if (progID != null && progID.Length > 0)
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progID))
                {
                    if (description != null)
                        key.SetValue("", description);
                    if (icon != null)
                        key.CreateSubKey("DefaultIcon").SetValue("", ToShortPathName(icon));
                    if (application != null)
                        key.CreateSubKey(@"Shell\Open\Command").SetValue("",
                                    ToShortPathName(application) + " \"%1\"");
                }
        }

        // Return true if extension already associated in registry
        public static bool IsAssociated(string extension)
        {
            return (Registry.ClassesRoot.OpenSubKey(extension, false) != null);
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetShortPathName(string lpszLongPath,
            [Out] StringBuilder lpszShortPath, uint cchBuffer);

        // Return short path format of a file name
        private static string ToShortPathName(string longName)
        {
            StringBuilder s = new StringBuilder(1000);
            uint iSize = (uint)s.Capacity;
            uint iRet = GetShortPathName(longName, s, iSize);
            return s.ToString();
        }
        #endregion

        #region Registration for Custom file Type
        public class FileRegistrationHelper
        {
            public static void SetFileAssociation(string extension, string progID)
            {
                // Create extension subkey
                SetValue(Registry.ClassesRoot, extension, progID);

                // Create progid subkey
                string assemblyFullPath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("/", @"\");
                StringBuilder sbShellEntry = new StringBuilder();
                sbShellEntry.AppendFormat("\"{0}\" \"%1\"", assemblyFullPath);
                SetValue(Registry.ClassesRoot, progID + @"\shell\open\command", sbShellEntry.ToString());
                StringBuilder sbDefaultIconEntry = new StringBuilder();
                sbDefaultIconEntry.AppendFormat("\"{0}\",0", assemblyFullPath);
                SetValue(Registry.ClassesRoot, progID + @"\DefaultIcon", sbDefaultIconEntry.ToString());

                // Create application subkey
                SetValue(Registry.ClassesRoot, @"Applications\" + System.IO.Path.GetFileName(assemblyFullPath), "", "NoOpenWith");
            }

            private static void SetValue(RegistryKey root, string subKey, object keyValue)
            {
                SetValue(root, subKey, keyValue, null);
            }
            private static void SetValue(RegistryKey root, string subKey, object keyValue, string valueName)
            {
                bool hasSubKey = ((subKey != null) && (subKey.Length > 0));
                RegistryKey key = root;

                try
                {
                    if (hasSubKey) key = root.CreateSubKey(subKey);
                    key.SetValue(valueName, keyValue);
                }
                finally
                {
                    if (hasSubKey && (key != null)) key.Close();
                }
            }
        }
        #endregion
    }
}
