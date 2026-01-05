using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    /// <summary>
    /// A class describing a self-containted package of MULTITUDE documents, including basic document information and its derived type specific data
    /// Used for transporting complete data between different homes
    /// Importing home must assign new GUIDs for each document
    /// </summary>
    [Serializable]
    class Package
    {
        public List<Document> Documents;

        public void Import(string filePath)
        {
            // Unzip
            // ...
            // Call handler of each specific  type
            // foreach ImportFromPackage();
        }

        public void Export(string filePath)
        {
            // Call handler of each specific  type
            // foreach ImportFromPackage();

            // Zip
            // foreach ExportToPackage();
        }
    }
}
