using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    static class ArchiveHelper
    {
        // Ref: https://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx. http://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            CopyFilesRecursively(new DirectoryInfo(sourcePath), new DirectoryInfo(destinationPath));
        }


        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (target.Exists == false) target.Create();
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        // Auxliary functions
        // JSON
        //void ExportJSON(string folder, bool bSeperate = false);   // Export as a whole or as individual files
        //static FileManager ImportJSON(string folder);
    }
}
