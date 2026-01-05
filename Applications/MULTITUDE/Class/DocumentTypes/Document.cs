using MULTITUDE.Class.Facility;
using MULTITUDE.Class.Facility.ClueManagement;
using MULTITUDE.CustomControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MULTITUDE.Class.DocumentTypes
{
    /// <summary>
    /// Each DocumentType has a specific icon
    /// </summary>
    [Serializable]
    enum DocumentType
    {
        PlainText,
        MarkdownPlus,
        Archive,
        VirtualArchive,
        DataCollection,
        Graph,
        Command,
        Web,
        PlayList,
        ImagePlus,
        Sound, 
        Video, 
        VirtualWorkspace, 
        Others,     // Those we do not recognize but has a suffix, can be redirected to OS to handle it
        Unkown  // Those do not have a suffix, Default treat as plain text
    }

    /// <summary>
    /// An implementation of StringValuePair that are modifiable
    /// </summary>
    [Serializable]
    class StringValuePair
    {
        public StringValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents a document, A document refers to a file
    /// <Development> If a document support CEA (Content Element Addressing), then the corresponding editor must keep a state of all other documetns that currently referencing the CE of the current document, so when a CE is changed and the change might affect its CEA, the document can be notified
    /// Only base is serializable, children documents don't serialize their data, but save directly to the file (Real archive, command, and web are handled specially, the last one saved in memory)
    /// </summary>
    [Serializable]
    internal abstract class Document : ISerializable, INotifyPropertyChanged   // Need to implement this interface so children can implement their own; For this reason children must implement ISerializable if they want to add their own data
    {
        // Identification
        /// <summary>
        /// The global identification of the document; Notice a GUID is only valid in its current Home
        /// Notice both clues and name do not distinguish upper/lower cases
        /// </summary>
        public int GUID { get; set; }   // I believe it is enough for ordinary power users; Set by Home
        public DocumentType Type { get; set; }  // Determined during importing from suffix
        private string _PhysicalLocationURI; // An Uri using either linux, web, windows, or Home protocol- The file location corresponding to this document; Notice there can be a bit waste when Documents are inside folders in Home (which will record all sub folder paths)for we don't have a representation for folders thus path cannot be constructed; All paths except those beginning with "home:" indicates a relative to home address - and who said we will not support those documents that actually reside in cloud (either in Totalimagine server, or redirected by server from another PC)?; Notice file path can have nothing to do with file meta name
        public string Path { get { if (_PhysicalLocationURI != null && _PhysicalLocationURI.IndexOf(HomeRootPathProtocol) == 0) { return Home.Location + _PhysicalLocationURI.Substring(HomeRootPathProtocol.Length); } else return _PhysicalLocationURI; } }    // A path that is directly physically accessible, i.e. when from inside a home it's already appended with Home location
        public List<Clue> Clues { get; set; }  // A clue is a chained list of unordered fragments, each is either a phrase, or a single word without any special symbol; All content elements, if they are addressable, are implied to share all its residual documents' clues; Now the somewhat confusing part is that each clue is termed a tag - not each clue fragment (to avoid confusion we don't use "tag"); A document is thus categorized under different clues, each of which indepedent of each other
        // Links describe addressing of content elements, either into the document or out from it; Specific document types handle links specially in their subclass.
        public List<int> RegisteredDocuments { get; set; } // ID of all documents that refer to this; When we are deleted we shall notify user of those documents that refers to this

        // Metadata
        public List<StringValuePair> Metadata { get; set; }    // Two fields are specially handled if provided: name and comment (all lower case)
        public string CreationDate { get; set; }

        // Search hepler
        [NonSerialized]
        public int KeywordOccurences = 0;
        [NonSerialized]
        public int KeywordMisses = 0;

        #region Data Binding Support
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void NotifyDescriptivePropertiesChanged()
        {
            NotifyPropertyChanged("Description");
            NotifyPropertyChanged("ShortDescription");
            NotifyPropertyChanged("Details");
            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Comment");
        }

        // Meta Accessor: Used for quick access and data binding
        public string Description
        {
            get { return string.Format("[{0}] {1}: {2} ({3})", Type.ToString(), Name, Comment, Path); }
        }
        public string Details
        {
            get { return this.GetDetails(); }
        }
        public string ShortDescription
        {
            get { return string.Format("[{0}] {1}", Type.ToString(), string.IsNullOrEmpty(Name) == false ? Name : Comment); }
        }
        public string Name
        {
            get { return GetMeta("name"); }
            set { AddMeta("name", value); Home.Current.Relocate(this); NotifyDescriptivePropertiesChanged(); }
        }
        public string Comment
        {
            get { return GetMeta("comment"); }
            set { AddMeta("comment", value); NotifyDescriptivePropertiesChanged(); }
        }
        #endregion

        // Extra Accessor
        public string References    // Links that point to current document
        {
            get
            {
                //List<string> links = (App.Current as App).CurrentHome.GetLinksToDocument(this);
                //if (links != null)
                //{
                //    return string.Join("\n", links);
                //}
                //else
                //{
                //    return "null";
                //}
                throw new NotImplementedException();
            }
        }
        public BitmapImage VirtualIcon  // Used by Collection Creator
        {
            get
            {
                switch (Type)
                {
                    case DocumentType.PlainText:
                    case DocumentType.MarkdownPlus:
                        return IconBase.TextVirtualIcon;
                    case DocumentType.Archive:
                    case DocumentType.VirtualArchive:
                    case DocumentType.VirtualWorkspace:
                        return IconBase.ArchiveVirtualIcon;
                    case DocumentType.PlayList:
                    case DocumentType.ImagePlus:
                    case DocumentType.Sound:
                    case DocumentType.Video:
                        return IconBase.MediaVirtualIcon;
                    case DocumentType.DataCollection:
                    case DocumentType.Graph:
                    case DocumentType.Command:
                    case DocumentType.Web:
                    case DocumentType.Others:
                    case DocumentType.Unkown:
                    default:
                        return IconBase.GeneralVirtualIcon;
                }
            }
        }
        public BitmapImage SmallIcon    // Used by MemoirSearchBar in data binding
        {
            get
            {
                switch (Type)
                {
                    case DocumentType.PlainText:
                        return IconBase.PlainTextSmallIcon;
                    case DocumentType.MarkdownPlus:
                        return IconBase.MarkdownPlusSmallIcon;                        
                    case DocumentType.Archive:
                        return IconBase.ArchiveSmallIcon;
                    case DocumentType.VirtualArchive:
                        return IconBase.VirtualArchiveSmallIcon;
                    case DocumentType.DataCollection:
                        return IconBase.DataCollectionSmallIcon;
                    case DocumentType.Graph:
                        return IconBase.GraphSmallIcon;
                    case DocumentType.Command:
                        return IconBase.CommandSmallIcon;
                    case DocumentType.Web:
                        return IconBase.WebSmallIcon;
                    case DocumentType.PlayList:
                        return IconBase.PlayListSmallIcon;
                    case DocumentType.ImagePlus:
                        return IconBase.ImagePlusSmallIcon;
                    case DocumentType.Sound:
                        return IconBase.SoundSmallIcon;
                    case DocumentType.Video:
                        return IconBase.VideoSmallIcon;
                    case DocumentType.Others:
                        return IconBase.OthersSmallIcon;
                    case DocumentType.Unkown:
                        return IconBase.UnkownSmallIcon;
                }
                return IconBase.UnkownSmallIcon;
            }
        }
        public bool HasAnyClassification
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name) == false || Clues.Count > 0;
            }
        }

        // Constructors: notice PATH can be null for created-on-the-fly documents
        protected Document(DocumentType type, string path, string metaname, string date)
        {
            Clues = new List<Clue>();
            Metadata = new List<StringValuePair>();

            Type = type;
            _PhysicalLocationURI = path;
            Metadata.Add(new StringValuePair("name", metaname != null? metaname : ""));

            // CreationDate = System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss");    // https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
            CreationDate = date;

            bDirty = true;
        }

        // Interface
        public void RegisterUser(Document doc)
        {
            RegisteredDocuments.Add(doc.GUID);
        }
        public string[] GetMatchingClueFragments(string fragment)    // Given a fragment, find matching clue, and return all other fragments in that clue, including the given one
        {
            fragment = fragment.ToLower();
            foreach (Clue clue in Clues)
            {
                if (clue.Contains(fragment))
                    return clue.Fragments; // Use only the first match, because for a document its clues shouldn't have fragment overlaps otherwise it indicates a bad design
            }
            return null;
        }
        public void AddClue(Clue clue)
        {
            Clues.Add(clue);

            // Add to home as well; Notice a documetn can add whatever many clues it want but not add to home
            Home home = (App.Current as MULTITUDE.App).CurrentHome;
            ClueManager.Manager.AddClue(clue, this);

            // Also remove current document from forgotten universe if we are adding the first ever clue
            if (Clues.Count == 1) home.Relocate(this);

            // Now for the purpose of encapsulation it might seems legit for Home to actively manage its document rather than get informed when its documents get changed; but require documetns to notify their homes isn't a bad design since it can be considered as some sort of event model, with Home being the central repository and all documents actively register/deregister themselves.
        }
        public void ChangeClue(Clue oldClue, Clue newClue)
        {
            for (int i = 0; i < Clues.Count; i++)
            {
                if (Clues[i].Equals(oldClue)) { Clues[i] = newClue; return; }
            }
            
            throw new ArgumentException("Old clue doesn't exist in current document!");
        }
        public void ChangePrimaryClueFragment(string oldFragment, string newFragment)
        {
            for (int i = 0; i < Clues.Count; i++)
            {
                if (Clues[i].Fragments[0] == oldFragment) { Clues[i].Fragments[0] = newFragment; return; }
            }

            throw new ArgumentException("Old clue doesn't exist in current document!");
        }
        public void RemoveClue(Clue oldClue)
        {
            // Remove from document tags record
            Clues.Remove(oldClue);

            // Remove from home as well
            Home home = (App.Current as MULTITUDE.App).CurrentHome;
            ClueManager.Manager.RemoveClue(oldClue, this);

            // Also add current document to forgotten universe if we have no clues left and we don't have a name
            home.Relocate(this);
        }
        // Add or change existing meta
        public void AddMeta(string field, string value)
        {
            field = field.ToLower(); // Metakey always in lower
            // Replace an old one if found
            for (int i = 0; i < Metadata.Count; i++)
            {
                if(Metadata[i].Key == field)
                {
                    Metadata[i].Value = value;
                    return;
                }
            }

            // Add a new one
            Metadata.Add(new StringValuePair(field, value));
        }
        public void ChangeMeta(string oldKey, string newKey)
        {
            newKey = newKey.ToLower(); // Metakey always in lower
            // Replace an old one if found
            for (int i = 0; i < Metadata.Count; i++)
            {
                if (Metadata[i].Key == oldKey)
                {
                    Metadata[i].Key = newKey;
                    return;
                }
            }

            throw new InvalidOperationException("Field doesn't exist.");
        }
        // Returns whether the given partialKey in any means matches any metaname or metavalue, processed in lower case
        public bool IsPartialMetaNameOrValue(string partialKey)
        {
            partialKey = partialKey.ToLower();
            for (int i = 0; i < Metadata.Count; i++)
            {
                if (Metadata[i].Key.Contains(partialKey) || Metadata[i].Value.ToLower().Contains(partialKey))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsMetanamePresent(IEnumerable<string> keys)
        {
            foreach (string key in keys)
            {
                if (IsMetanamePresent(key) == true) return true;
            }
            return false;
        }
        public bool IsMetanamePresent(string key)
        {
            int seperatorIndex = key.IndexOf('=');
            if (seperatorIndex < 0)
            {
                key = key.ToLower();
                for (int i = 0; i < Metadata.Count; i++)
                {
                    if (Metadata[i].Key == key)
                    {
                        return true;
                    }
                }
                return false;
            }
            else return IsMetapairPresent(key.Substring(0, seperatorIndex), key.Substring(seperatorIndex + 1));
        }
        public bool IsMetapairPresent(string key, string partialValue)
        {
            key = key.ToLower();
            for (int i = 0; i < Metadata.Count; i++)
            {
                if (Metadata[i].Key == key && Metadata[i].Value.Contains(partialValue))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsPartialMetaValue(IEnumerable<string> partialValues)
        {
            foreach (string value in partialValues)
            {
                if (IsPartialMetaValue(value) == true) return true;
            }
            return false;
        }
        public bool IsPartialMetaValue(string partialValue)
        {
            partialValue = partialValue.ToLower();
            for (int i = 0; i < Metadata.Count; i++)
            {
                if (Metadata[i].Value.ToLower().Contains(partialValue))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsDocumentType(string searchValue)
        {
            return Type.ToString().ToLower().IndexOf(searchValue.ToLower()) == 0;
        }
        public bool IsValueAnywherePresent(string searchValue)
        {
            return IsPartialMetaNameOrValue(searchValue) || IsPartialCLue(searchValue) || IsPartialContent(searchValue) || IsDocumentType(searchValue);
        }
        public bool IsValueAnywherePresent(IEnumerable<string> searchValues)
        {
            foreach (string value in searchValues)
            {
                if (IsPartialMetaNameOrValue(value) || IsPartialCLue(value) || IsPartialContent(value) || IsDocumentType(value)) return true;
            }
            return false;
        }
        // Should be implemented by children
        public virtual bool IsPartialContent(string partialString)
        {
            return false;
        }
        // Returns whether the given string in any means matches any clue, processed in lower case
        public bool IsPartialCLue(string partialString)
        {
            Clue compareClue = new Clue(partialString);
            foreach (Clue clue in Clues)
            {
                if (clue.Contains(compareClue)) return true;
            }
            return false;
        }
        public string GetMeta(string field)
        {
            foreach (StringValuePair meta in Metadata)
            {
                if (meta.Key == field)
                    return meta.Value;
            }
            return null;
        }
        public void ConvertType(DocumentType TargetType)
        {
            // Convert type if current document is empty
            // Throw exception DocumentNonEmpty() if not
            throw new NotImplementedException();
        }

        // Return a legal destinate physical file path; Also return the relativePath with extension (if given) for that path
        protected string GetLegalPhysicalLocation(out string relativePath, string suffix)
        {
            // State summary: before this function is called, a document is either: externalized (with an esxternal location), just created (with null location), or already internalized (with Home relative location)

            // If it's already internalized, which by any means this function shouldn't have been called at first place, just return its current status
            if (_PhysicalLocationURI != null && _PhysicalLocationURI.IndexOf(HomeRootPathProtocol) == 0) { relativePath = _PhysicalLocationURI.Substring(HomeRootPathProtocol.Length); return Path; }

            // Otherwise generate a relative path for it using current available information
            // <Development> Notice below implementation hasn't considered document division by folders yet, but so far just all put under one folder

            // Generate a file system legal name
            relativePath = GetMeta("name");
            if (relativePath == "" || relativePath == null) relativePath = GetMeta("comment");
            if (relativePath == "" || relativePath == null) relativePath = System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss");
            else
            {
                foreach (char c in invalidCharacters)   // <performance> Cautious performance
                {
                    relativePath = relativePath.Replace(c.ToString(), " ");
                }
            }
            // Generate a path
            string testPath = Home.Location + relativePath;    // CurrentHome.Location end with '\'
            if (suffix == null) suffix = "";

            // Try path validity depending on document type
            if (Type == DocumentType.Archive)
            {
                if (System.IO.Directory.Exists(testPath + suffix)) relativePath += System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss"); // Guanranteed not to exist as long as time doesn't flow back 
            }
            else
            {
                if (System.IO.File.Exists(testPath + suffix)) relativePath += System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss"); // Guanranteed not to exist as long as time doesn't flow back
            }

            relativePath = relativePath + suffix;
            return Home.Location + relativePath;
        }
        // If current document isn't internalized, then do it
        static string HomeRootPathProtocol = "home:";   // No "\\" etc.
        public static readonly string DragDropFormatString = "MULTITUDE Document";
        static string invalidCharacters = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
        public void Internalize(ImportAction action)
        {
            // Check for internalization
            if (_PhysicalLocationURI.IndexOf(HomeRootPathProtocol) != 0)   // If we are not using an internal address
            {
                // Check to see if already in home, so we condense the path a bit
                if (action == ImportAction.Refer)
                {
                    if (Path.IndexOf(Home.Location) == 0)
                    {
                        _PhysicalLocationURI = HomeRootPathProtocol + _PhysicalLocationURI.Substring(Home.Location.Length);
                    }
                    else return;
                }
                // Replace path
                else
                {
                    string extension = System.IO.Path.GetExtension(Path);
                    string relativePath;
                    string newPath = GetLegalPhysicalLocation(out relativePath, extension);

                    // Take appropriate file action using new path
                    switch (action)
                    {
                        case ImportAction.Cut:
                            FileHelper.MoveOrRenameDirectoryOrFile(Path, newPath);
                            break;
                        case ImportAction.Clone:
                            if (Type == DocumentType.Archive) { ArchiveHelper.CopyDirectory(Path, newPath); }
                            else { System.IO.File.Copy(Path, newPath); }
                            break;
                        case ImportAction.Refer:
                            throw new InvalidOperationException("Should have been handled.");
                        default:
                            break;
                    }

                    // Change file path
                    _PhysicalLocationURI = HomeRootPathProtocol + relativePath;
                    // Notice original file's meta-name remains unchanged: meta names will tend to diverge from actual file name
                }
            }
        }
        // Save document data in a solid file
        // For a non-material document (i.e. created completely in memory), give it a physical form (throw an exception if it already has one) by generating a physical file-back file for the document depenidng on it type
        // Used only for those documents that are created and managed by VW and actually needs a physical representation
        public void Materialize()
        {
            if (Path != null)
                throw new InvalidOperationException("Only non-existing documents can be materialized");

            // Find suffix
            string extension = "";
            switch (Type)
            {
                case DocumentType.PlainText:
                    extension = PlainText.FileSuffix;
                    break;
                case DocumentType.MarkdownPlus:
                    extension = MarkdownPlus.FileSuffix;
                    break;
                case DocumentType.Archive:
                    // Notice we don't support creating directories what-so-ever in our environment, we just support opening them
                    throw new InvalidOperationException("Real archives cannot be materialized, it's purely in memory");
                case DocumentType.VirtualArchive:
                    extension = Archive.FileSuffix;
                    break;
                case DocumentType.DataCollection:
                    extension = DataCollection.FileSuffix;
                    break;
                case DocumentType.Graph:
                    extension = Graph.FileSuffix;
                    break;
                case DocumentType.Command:
                    extension = Command.FileSuffix;
                    break;
                case DocumentType.Web:
                    extension = Web.FileSuffix;
                    break;
                case DocumentType.PlayList:
                    extension = Playlist.FileSuffix;
                    break;
                case DocumentType.ImagePlus:
                    extension = ImagePlus.FileSuffix;
                    break;
                case DocumentType.VirtualWorkspace:
                    extension = VirtualWorkspace.FileSuffix;
                    break;
                case DocumentType.Sound:
                case DocumentType.Video:
                case DocumentType.Others:
                case DocumentType.Unkown:
                default:
                    throw new InvalidOperationException("Unsupported document type for materialization.");
            }

            // Get a suitable filepath
            string relativePath;
            string filePath = GetLegalPhysicalLocation(out relativePath, extension);
            // Creata a physical file
            using (System.IO.File.Create(filePath)) { };

            // Update
            _PhysicalLocationURI = HomeRootPathProtocol + relativePath;
            AddMeta("extension", extension);

            // Save document data (if any) to the created file
            if(bDirty){SaveDocument(); bDirty = false;}
        }
        public void Disintegrate()
        {
            if (Path == null)
                throw new InvalidOperationException("Only physically existing documents can be disintegrated.");

            // Physically delete the file/folder pointed by PATH if it's LOCAL - we don't delete a reference type pointing to external file/folder
            if(_PhysicalLocationURI.IndexOf(HomeRootPathProtocol) == 0)
            {
                // Archive Handling vs Normal file document
                if (System.IO.Directory.Exists(this.Path))
                    System.IO.Directory.Delete(this.Path, true);
                else System.IO.File.Delete(this.Path);
            }

            // At this time nowhere in our application should have a reference to this object
        }

        #region Serialization
        protected bool bDirty = false;  // It's crucial children make use of this during serialization to avoid unnecessary file saving during Home serializing (where document information needs to be saved anyway but files may not need to be updated.)
        public Document(SerializationInfo info, StreamingContext ctxt)
        {
            bDirty = false;
            // Load properties
            GUID = (int)info.GetValue("GUID", typeof(int));
            Clues = (List<Clue>)info.GetValue("Clues", typeof(List<Clue>));
            Type = (DocumentType)info.GetValue("Type", typeof(DocumentType));
            _PhysicalLocationURI = (string)info.GetValue("_PhysicalLocationURI", typeof(string));
            CreationDate = (string)info.GetValue("CreationData", typeof(string));
            Metadata = (List<StringValuePair>)info.GetValue("Metadata", typeof(List<StringValuePair>));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Save Properties
            info.AddValue("GUID", GUID);
            info.AddValue("Clues", Clues);
            info.AddValue("Type", Type);
            info.AddValue("_PhysicalLocationURI", _PhysicalLocationURI);
            info.AddValue("CreationData", CreationDate);
            info.AddValue("Metadata", Metadata);
        }
        #endregion Serialization

        #region Document Interface
        public void RequestSave()
        {
            SaveDocument();
        }
        protected virtual string GetDetails()
        {
            return string.Empty;
        }
        /// <summary>
        /// Call this function to save document data (defined by children) to physical file
        /// </summary>
        protected abstract void SaveDocument();
        /// <summary>
        /// Call this function to load document data (defined by children) from physical file
        /// </summary>
        protected abstract void LoadDocument();
        /// <summary>
        /// Given a corresponding document file, we import and create an instance of corresponding document
        /// </summary>
        /// <returns></returns>
        // public static Document Import(System.IO.FileInfo file);
        /// <summary>
        /// Export the given document into a file used for import, notice this is differetn from SaveDocument() and LoadDocument() which is used to keep length document data, not for sharing
        /// </summary>
        public abstract void Export();

        /// <summary>
        /// Transportation Support
        /// </summary>
        // protected abstract void ImportFromPackage(Stream stream);
        /// <summary>
        /// Transportation Support
        /// </summary>
        // protected abstract void ExportToPackage(Stream stream);
        #endregion
    }
}
