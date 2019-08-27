using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    /// <summary>
    /// Encapsulates a flie
    /// </summary>
    class File : OrganizationUnit
    {

    }

    /// <summary>
    /// Encapsulates a folder
    /// </summary>
    class Folder : OrganizationUnit
    {
        // This is not null only for root folders; subfolders use their name to constitute their physical path
        public string FolderPath { get; set; }
    }
}
