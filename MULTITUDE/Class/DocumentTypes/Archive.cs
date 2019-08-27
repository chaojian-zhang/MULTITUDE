using MULTITUDE.Class.Facility.ClueManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    // CEA format: RootName/NodeName/....
    [Serializable]
    class Archive : Document, ISerializable
    {
        #region Constructors
        // Create VA from VW
        public Archive(VirtualWorkspace vw): base(DocumentType.VirtualArchive, null, vw.Name, vw.CreationDate)
        {
            IsReal = false;
            Roots = new List<ArchiveNode>();
            ArchiveNode rootNode = new ArchiveNode(vw.Name, this);
            Roots.Add(rootNode);

            // Populate contents
            Home home = (App.Current as App).CurrentHome;
            for (int i = 0; i < vw.Pages.Count; i++)
            {
                ArchiveNode child = rootNode.AddNode("Page " + i);
                foreach (DocumentIcon docIcon in vw.Pages[i].Documents)
                {
                    Document doc = home.GetDocument(docIcon.DocumentID);
                    child.AddNode(doc.Name, doc);
                }
            }

            // Request saving
            bDirty = true;
        }
        public Archive(bool real, string path, string metaname, string creationdate = null)
            : this(real ? DocumentType.Archive : DocumentType.VirtualArchive, path, metaname, creationdate, real) { }
        // A wrapper for underlying base
        public Archive(DocumentType type, string path, string metaname, string date, bool real = false)
            : base(type, path, metaname, date != null ? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {
            IsReal = real;
            Roots = new List<ArchiveNode>();
            Roots.Add(new ArchiveNode(metaname, this));    // Add a node represent self as a beginning point; An archive has at least one node in all times

            // Request saving
            bDirty = true;
        }
        #endregion

        #region Data
        public bool IsReal { get; set; } // Whether the archive is real: Denote all real folder/files, has only one root
        public List<ArchiveNode> Roots; // A seperate file is a root on its own; To support Content Element Addressing, at each level the Nodes must all have unique names (e.g. roots must unique, and first level of roots must unique etc..)
        public void MarkDirty() { bDirty = true; }
        #endregion
        public static readonly string FileSuffix = ".MULTITUDEvar";

        #region Serialization Handling
        // Serialization function: VAR and AR differs in behavior
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save common basic information
            info.AddValue("bReal", IsReal);

            // Real archives save contents in real folders and files during editing process
            if (IsReal)
            {
                // Nothing to load
                // Children will be loaded during editing upon request
            }
            // Virtual archives package itself in a file, rather than participate directly from serialization process
            else
            {
                SaveDocument();
            }
        }

        // Deserialization constructor
        public Archive(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Get common basic information and assign them to the appropriate properties
            IsReal = (bool)info.GetValue("bReal", typeof(bool));

            if(IsReal)
            {
                // Initialize empty root
                Roots = new List<ArchiveNode>();
                Roots.Add(new ArchiveNode(this.Name, this));    // Add a node represent self as a beginning point; An archive has at least one node in all times

                // Children will be loaded during editing so no need to load -- this is necessary so details of the folder remians hidden until inspection
            }
            else
            {
                LoadDocument(); 
            }
        }
        protected override void SaveDocument()
        {
            if(bDirty && IsReal == false)
            {
                // Save data into a file
                Stream fileStream = File.Create(Path);
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(fileStream, Roots);
                fileStream.Close();

                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            if(IsReal == false)
            {
                // Load from a file
                Stream fileStream = File.OpenRead(Path);
                BinaryFormatter deserializer = new BinaryFormatter();
                Roots = (List<ArchiveNode>)deserializer.Deserialize(fileStream);
                fileStream.Close();

                // Setup owner
                foreach (ArchiveNode node in Roots)
                {
                    node.SetOwner(this);
                }
            }
        }

        public static Archive Import(System.IO.FileInfo file)
        {
            throw new NotImplementedException();
        }

        public override void Export()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Interface Functions
        // Return null for a non-real archive (i.e. virtual archive)
        public ArchiveNode GetRealRoot()
        {
            return IsReal ? Roots[0] : null;
        }
        public ArchiveNode AddRootNode(string nodeName)
        {
            ArchiveNode newNode = new ArchiveNode(nodeName, this);
            Roots.Add(newNode);

            // Request saving
            bDirty = true;

            return newNode;
        }
        // Get Node using CotnentElement addressing
        public ArchiveNode GetNode(string contentElement)   // String inclue [] so strip it first
        {
            string address = contentElement.Substring(1, contentElement.Length - 2);    // Get actual address
            string[] nodeNames = address.Split(new char[] { '/' }); // Get name of each node
            // Iterate to find node
            List<ArchiveNode> children = Roots;
            ArchiveNode node = null;
            foreach (string name in nodeNames)
            {
                bool bFound = false;
                foreach (ArchiveNode child in children)
                {
                    if (child.Name == name) { node = child; bFound = true; break; }
                }
                if (bFound == true) { children = node.Children; }
                else { return null; }
            }
            return node;
        }

        public string GetNodeCEA(ArchiveNode node)
        {
            if (node.Parent != null)
                return GetNodeCEA(node.Parent) + '/';   // Notice CEA is in linux format
            else
                return node.Name;
        }
        #endregion Interface Functions
    }

    [Serializable]
    // <debug> Cautious how this handles Owner: will it automatically link to existing object or cause problem since Archive isn't automatically serialized
    class ArchiveNode: INotifyPropertyChanged
    {
        /// <summary>
        /// Generate a new ArchiveNode, optionally with a parent and a Document reference to establish a link
        /// An ArchiveNode can represent both a file and a folder, if it doesn't have any children then it's a file
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="docRef"></param>
        public ArchiveNode(string name, Archive owner, ArchiveNode parent = null)
        {
            Owner = owner;
            _Name = name;
            Parent = parent;
            if (owner.IsReal == false) _Children = new List<ArchiveNode>();
            else _Children = null;
            _Text = string.Empty;
        }
        public string GetPath(string replaceName = null) // Notice this is in Windows format
        {
            if(Parent != null)
                return System.IO.Path.Combine(Parent.GetPath(), replaceName == null ? Name : replaceName);  // Notice parents are still using their original name
            else
            {
                if (Owner.IsReal) return Owner.Path;
                else return System.IO.Path.Combine(Owner.Path, replaceName == null ? Name : replaceName);
            }
        }

        public const string DefaultArchiveNodeName = "New Node";

        #region Data
        [NonSerialized]
        public Archive Owner;
        public ArchiveNode Parent { get; set; }
        public string Link { get; set; }

        // <Development> Pending elaboration into a full scale MD+ document - maybe embeded rather than stand alone
        private string _Text;
        public string Text
        {
            get { return _Text; }
            set
            {
                if (Owner.IsReal == true)
                {
                    // Commit change to file/folder
                    throw new InvalidOperationException("This is not a valid operation; We might consider support text files loading in real time though.");
                }
                else if (value != _Text)
                {
                    _Text = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                if (Owner.IsReal == true)
                {
                    // Do some validation, replace invalid characters
                    if (Clue.ValidateString(value) == false) value = new string(value.Where(ch => !Clue.InvalidClueCharacters.Contains(ch)).ToArray());
                    // Commit change to file/folder
                    string path = GetPath();
                    if (System.IO.Directory.Exists(path)) System.IO.Directory.Move(path, GetPath(value));
                    else System.IO.File.Move(path, GetPath(value));
                }
                else if (value != _Name)
                {
                    // Update
                    _Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private List<ArchiveNode> _Children;    // For a real archive it has only one node and it has no children
        public List<ArchiveNode> Children
        {
            get
            {
                // If it's a real archive and the node is a directory, load contents at that folder dynamically
                if(_Children == null)
                {
                    System.IO.DirectoryInfo newDir = new DirectoryInfo(this.GetPath());
                    List<ArchiveNode> nodes = new List<ArchiveNode>();
                    if (newDir.Exists)
                    {
                        foreach (DirectoryInfo dir in newDir.EnumerateDirectories())
                        {
                            nodes.Add(new ArchiveNode(dir.Name, Owner, this));
                        }
                        foreach (FileInfo file in newDir.EnumerateFiles())
                        {
                            nodes.Add(new ArchiveNode(file.Name, Owner, this));
                        }
                    }
                    return nodes;
                }
                else return _Children;
            }
        }
        #endregion

        #region Data Binding
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Serialization Support
        internal void SetOwner(Archive archive)
        {
            this.Owner = archive;
            foreach (ArchiveNode node in Children)
            {
                node.SetOwner(archive);
            }
        }
        #endregion

        #region Interface
        public ArchiveNode AddNode(string name = DefaultArchiveNodeName, Document docRef = null)
        {
            ArchiveNode newNode = new ArchiveNode(name, Owner, this);
            // If it's real we add a new folder bearing the node's name, otherwise we just create a virtual node
            if (_Children == null)
            {
                System.IO.Directory.CreateDirectory(newNode.GetPath());
            }
            else
            {
                if (docRef != null)
                {
                    newNode.Link = "ID" + docRef.GUID;
                    // Owner.AddLink(this); // Record an external ref; Not used
                }

                Children.Add(newNode);
            }
            return newNode;
        }
        // Might be null
        public Document GetLinkedDocument()
        {
            if (Link == null) return null;
            return Home.Current.GetDocument(int.Parse(Link.Substring(2)));
        }
        #endregion
    }
}