using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

// Regarding serialization: Aim at using serialization
// Otherwise, output categorization systems fist, from root to children, one by one; Associated for each OU is output in pure string form(though can be binary) so takes a bit extra space but is more condense
// If we use text format then final generated output should ideally be encrypted and but definitely compressed
namespace MULTITUDE.Class
{
    // <Test> Pending checkout: Whether Lists got serialized
    class StorageManager
    {
        #region Data
        // Information about organization units
        public List<string> HomeLocations { get; set; }  // A duplicate of Application, serialized (each string goes with each List<OrganizationUnit>)
        // List of all OUs reside in out application, seperated per User Home Location
        public List<List<OrganizationUnit>> OUs { get; set; } // The first dimension corresponds to user home locations; The second dimension is actual OU table, or GUID table since index matches
        // Lists of specialized objects, those are just references from above
        public List<OrganizationUnit> TempUniverse { get; set; }     // Objects that are not by no means organized to a prominent classification structure (i.e. Collections)
        public List<OrganizationUnit> ForgottenUniverse { get; set; }   // Objects that are completely dereferenced yet not deleted from file system yet; Its links are still preserved on that OU, but no longer available from organization systems themselves
        // Collections of structures, all below holds root of organizatio systems
        public List<OrganizationUnit> Collections { get; set; } 
        public List<OrganizationUnit> IndexPages { get; set; } 
        public List<OrganizationUnit> Graphs { get; set; } 
        public List<VirtualWorkspace> VirtualWorkspaces { get; set; }     // Notice a virtual workspace itself isn't a referencable organization unit; But can be root of a PATH
        public List<OrganizationUnit> Documents { get; set; }       // A useful abstraction for QuickMatch, for we normally want to edit/open something
        #endregion Data

        #region Storing and Retriving
        // Load function
        public void LoadData()
        {
            throw new NotImplementedException();
        }

        // Store function: Each user home is stored as a seperate file
        public void SaveData()
        {
            throw new NotImplementedException();
        }
        
        // Cache handling
        
        // Text Document handling

        #endregion Storing and Retriving

        #region Acess Interface
        /// <summary>
        /// Export the OU, if its a document then export its content, if it's a file or folder then just copy it, if it's a structure then export its children structure in a text form
        /// </summary>
        public void Export(OrganizationUnit item, string outputPath)
        {
            throw new NotImplementedException();
        }
        #endregion Acess Interface
    }
}
