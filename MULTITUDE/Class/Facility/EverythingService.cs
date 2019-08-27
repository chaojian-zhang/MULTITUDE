using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    #region Helpder Constructs
    class FileSystemEntry
    {
        public enum EntryType
        {
            File,
            Folder
        }

        public FileSystemEntry(string path, EntryType type)
        {
            Path = path;
            Type = type;
        }

        public EntryType Type { get; set; }
        public string Path { get; set; }
    }
    class FileSystemUpdate
    {
        public enum UpdateType
        {
            AddNew,
            MoveLocationOrRename,
            Modified,
            Delete
        }

        public FileSystemEntry Entry { get; set; }
        public UpdateType Type { get; set; }

        // Additional Parameters
        public string OldPath { get; set; } // For MoveLocationOrRename UpdateType
    }

    #endregion

    class EverythingService
    {
        #region DLL Definition
        const int EVERYTHING_OK = 0;
        const int EVERYTHING_ERROR_MEMORY = 1;
        const int EVERYTHING_ERROR_IPC = 2;
        const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
        const int EVERYTHING_ERROR_CREATEWINDOW = 4;
        const int EVERYTHING_ERROR_CREATETHREAD = 5;
        const int EVERYTHING_ERROR_INVALIDINDEX = 6;
        const int EVERYTHING_ERROR_INVALIDCALL = 7;

        const string EverythingDLLName = "Everything32.dll";

        [DllImport(EverythingDLLName, CharSet = CharSet.Unicode)]
        private static extern int Everything_SetSearchW(string lpSearchString);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetMatchPath(bool bEnable);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetMatchCase(bool bEnable);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetMatchWholeWord(bool bEnable);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetRegex(bool bEnable);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetMax(int dwMax);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_SetOffset(int dwOffset);

        [DllImport(EverythingDLLName)]
        private static extern bool Everything_GetMatchPath();
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_GetMatchCase();
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_GetMatchWholeWord();
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_GetRegex();
        [DllImport(EverythingDLLName)]
        private static extern UInt32 Everything_GetMax();
        [DllImport(EverythingDLLName)]
        private static extern UInt32 Everything_GetOffset();
        [DllImport(EverythingDLLName)]
        private static extern string Everything_GetSearchW();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetLastError();

        [DllImport(EverythingDLLName)]
        private static extern bool Everything_QueryW(bool bWait);

        [DllImport(EverythingDLLName)]
        private static extern void Everything_SortResultsByPath();

        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetNumFileResults();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetNumFolderResults();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetNumResults();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetTotFileResults();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetTotFolderResults();
        [DllImport(EverythingDLLName)]
        private static extern int Everything_GetTotResults();
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_IsVolumeResult(int nIndex);
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_IsFolderResult(int nIndex);
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_IsFileResult(int nIndex);
        [DllImport(EverythingDLLName, CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathNameW(int nIndex, StringBuilder lpString, int nMaxCount);
        [DllImport(EverythingDLLName)]
        private static extern void Everything_Reset();
        [DllImport(EverythingDLLName)]
        private static extern void Everything_CleanUp();
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_Exit();  // Well it's BOOL in C, so should be int here
        [DllImport(EverythingDLLName)]
        private static extern bool Everything_IsAdmin();  // Well it's BOOL in C, so should be int here

        [DllImport(EverythingDLLName, CharSet = CharSet.Unicode)]
        private static extern IntPtr Everything_GetResultFileNameW(int nIndex);
        #endregion

        #region Constructor
        public EverythingService(DateTime? knowledgeTime, SystemEntryUpdateEventHandler handler)
        {
            ResetEverything();
            // Do a full update and don't fire 
            if (knowledgeTime == null)
            {
                PreviousUpdateTime = DateTime.Now;   // Effectively do nothing
            }
            // Update and fire events
            else UpdateSystemRecord();

            // Register events
            SystemEntryUpdateEvent += handler;
        }
        #endregion

        #region File System Change Track
        private DateTime PreviousUpdateTime;
        /// <summary>
        /// Internally update our clock of system database, to Everything we are just querying whether anything have changed since our lat update
        /// This function should be called when: 1. MULTITUDE window got focus; 2. Once in a while
        /// </summary>
        /// <returns></returns>
        private List<FileSystemEntry> UpdateSystemRecord()
        {
            // Get file system changes since a particular time

            // Wrap event parameters

            // Fire events

            // Update time
            PreviousUpdateTime = DateTime.Now;

            return new List<Facility.FileSystemEntry>();
        }
        private void ResetEverything()
        {
            // Reset
            Everything_Reset();
            Everything_CleanUp();

            // Set 

        }
        #endregion

        #region File System Change Callbacks
        public delegate void SystemEntryUpdateEventHandler(FileSystemUpdate update);
        public event SystemEntryUpdateEventHandler SystemEntryUpdateEvent;
        #endregion

        #region Everything Functions
        //// A mighty function for performing search tasks
        //public List<string> PerformQuery(....)
        //{

        //}

        //public List<string> SearchContainingKeywords(string[] keywords)
        //{
        //    return SearchingUsingRegularExpression(RegExpFromKeywords(keywords));
        //}
        //public List<string> SearchingUsingRegularExpression(string regExp)
        //{
        //    // set the search
        //    Everything_SetSearchW(regExp);

        //    // use our own custom scrollbar... 			
        //    // Everything_SetMax(listBox1.ClientRectangle.Height / listBox1.ItemHeight);
        //    // Everything_SetOffset(VerticalScrollBarPosition...);

        //    // execute the query
        //    Everything_QueryW(true);

        //    // sort by path
        //    // Everything_SortResultsByPath();

        //    // Fetch Results
        //    List<string> results = new List<string>();
        //    for (int i = 0; i < Everything_GetNumResults(); i++)
        //    {
        //        results.Add(Marshal.PtrToStringUni(Everything_GetResultFileNameW(i)));
        //    }
        //    return results;
        //}
        #endregion

        #region Helpers
        ///// <summary>
        ///// Generate a regular expression enabling searching using keywords
        ///// </summary>
        ///// <param name="keywords"></param>
        ///// <returns></returns>
        //private string RegExpFromKeywords(string[] keywords)
        //{

        //}
        #endregion

        #region State Queries
        public static bool IsEverythingRunning()
        {
            // If everything is running as admin, or if everything is running as a standard user, then everything is running (well it should be EVERYTHING_ERROR_SUCCESS but that constant doesn't exist); To double check we make sure 
            if (Everything_IsAdmin() != false || Everything_GetLastError() != EVERYTHING_ERROR_IPC) return true;
            else
            {
                if (Everything_GetLastError() == EVERYTHING_ERROR_IPC) return false;
                else throw new Exception("Unknown Everything state.");
            }
        }
        public void ExitEverythinig()
        {
            Everything_Exit();
        }
        #endregion
    }
}
