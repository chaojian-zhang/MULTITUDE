using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    enum UrgencyLevel
    {
        Notice,
        Warning,
        Exception,
        Error
    }

    /// <summary>
    /// Debugging facility
    /// </summary>
    class LogHelper
    {
        public LogHelper(string folderLocation)
        {
            LogFileFolder = folderLocation;

            // If the folder doesn't exist, create one
            if(System.IO.Directory.Exists(LogFileFolder) == false) System.IO.Directory.CreateDirectory(LogFileFolder);
        }

        public string LogFileFolder { get; set; }

        public void Log(UrgencyLevel level, string content, object counterToken = null)
        {
            string logFileLocation = System.IO.Path.Combine(LogFileFolder, System.DateTime.Now.ToString("MMMM dd, yyyy") + ".txt");
            System.IO.File.AppendAllText(logFileLocation, string.Format("[{0}][{1}] {2}\n", SystemHelper.CurrentTimeFileNameFriendly, level, content));

            if (counterToken == CounterToken) Count++;
            else { CounterToken = counterToken; Count = 1; }
        }

        // Counter: Caller pass in an object as identifier and we can count some statistics along with logging
        object CounterToken;
        int Count = 0;
        public int GetLastOperationStatisticCount()
        {
            return Count;
        }
    }
}
