using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    /// <summary>
    /// This defines all addressable endpoints
    /// </summary>
    enum OrganizationUnitType
    {
        // Physical
        RealFile,
        RealFolder,
        // Primary
        DataCollection,
        FlowDocument,   // Notice a Flow Document is an internal document; External text files are edited and viewd using Flow Document Canvas as well but they are not considered "flow document" per se
        WebContent,     // A robust internal HTML viewr is scheduled
        // Structure
        // Linker, // Used for hyperlink elements inside a flow document; Notice linkers are not OUs
        Category,
        Tag,
        TagGroup,
        Graph
    }

    /// <summary>
    /// The fundamental unit of item that is addressable in MULTITUDE, and those constitude our application data
    /// ALL OU types has a class
    /// </summary>
    [Serializable]
    class OrganizationUnit
    {
        // Identification
        /// <summary>
        /// The global identification of the OU
        /// </summary>
        public long GUID { get; set; }
        /// <summary>
        /// Dynamic PATH to this OU depending on where it's referenced from; This is not unique, and each OU might have different PATH in differetn circumstances
        /// </summary>
        public string PATH { get; }

        // Special identification
        public static List<string> HomeLocations { get; set; }  // A duplicate of Application
        [field: NonSerialized()]   // Filled in after deserialization
        int _HomeIndex;
        /// <summary>
        /// Which home index this OU belongs to; Notice each Home Location has its own OU list, but to the application all structures and documetns are also kept in aggregate lists for reference purpose.
        /// This is also used for application internal relative locating its files and folders; For external file/folder the path will be absolute (with no waste, since for a folder only its node name needs to be recorded, its fs path can be constructed from parent)
        /// </summary>
        public int HomeIndex { get { return _HomeIndex; } }

        // Metadata
        public string Name { get; set; }
        public string Comment { get; set; }
        public List<KeyValuePair<string, string>> Metadata { get; set; }
        public string CreationDate { get; set; }
        public string AccessDate { get; set; }  // Might be useful for a "recent" list, though with VW I doubt we will ever need such a facility
        public string EditDate { get; set; }    // Only useful for applicaton-editable contents, i.e. Text documents

        // General links from this OU to others, this is populated whenever this OU is referenced
        // Structure nodes do not form links to their parents
        public List<Link> Links { get; set; }
    }

    // A link describes a double connection without order
    class Link
    {
        public OrganizationUnit Linker { get; set; }
        public OrganizationUnit Linkee { get; set; }
    }
}
