using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MULTITUDE.Class.DocumentTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MULTITUDE.Class.Facility;
using System.Collections;
using MULTITUDE.Class.Facility.ClueManagement;

namespace MULTITUDE.Class
{
    [Serializable]
    class EnctryptedString
    {
        public EnctryptedString(string unExcryptedContent)
        {
            _EncryptedContent = unExcryptedContent; // Obviously we are not doing anything here
        }

        private string _EncryptedContent;
        public string Content { get { return _EncryptedContent; } set { if (value != _EncryptedContent) _EncryptedContent = value; } } // Decrypted Content
    }

    // Represents a home
    [Serializable]
    class Home
    {
        #region Home Initialization
        public Home(string location)
        {
            Location = location;
            DocumentGUIDCounter = 0;
            _bLocked = false;

            _Documents = new List<Document>();
            _VirtualWorkspaces = new List<VirtualWorkspace>();
            _ForgottenUniverse = new List<Document>();
            _VoidUniverse = new List<Document>();
            _LinkUniverse = new List<Document>();

            _ClueManager = new ClueManager();
            _ActiveVW = new VirtualWorkspace(new Coordinate(0,0));
            _VirtualWorkspaces.Add(_ActiveVW);
        }
        #endregion

        #region Home Data
        private static string _Location;
        public static string Location { get { return _Location; } set { _Location = (value.Last() == '\\' ? value : value + "\\"); } }    // Home location; All documents have their location + home location to get their actual file location
        public DateTime? KnowledgeTime = null;  // Knowledge time of everything, indicates when is the last time Home get notified of file system changes; Null indicated never yet
        // Contents, seralized
        // private string HomeID { get; set; }  // An universally unique identifier of current home consisting of: MAC + Date (day to second) + a random number (000-500)
        private int DocumentGUIDCounter;
        private List<Document> _Documents;  // A list of all documents in current home // <Development> Can be implemented as a directory indexed using GUID for quicker access
        private List<VirtualWorkspace> _VirtualWorkspaces;
        // Lists of specialized objects, those are just references (indexes) from above
        private List<Document> _ForgottenUniverse;     // Objects that are not classfied or placed on VW or linked to (i.e. not in link universe)
        private List<Document> _VoidUniverse;   // Objects that are removed yet not deleted from file system
        // Configuration
        public string Appellation { get; set; }
        public string Username { get; set; }
        public EnctryptedString Password { get; set; }
        // State Management
        private bool _bLocked;
        public bool IsLocked { get { return _bLocked; } }
        // Below are book keeping information that can be serialized or not (and generated during loading) if we want to save space
        private List<Document> _LinkUniverse;    // Collection of document with links; Used for validation purpose only when a document is removed; Links form a link universe
        private ClueManager _ClueManager;
        private VirtualWorkspace _ActiveVW;
        // Property wrappers
        public List<Document> Documents { get { return _Documents; } }  // User shouldn't call Add() or otherwise modify this property directly
        public List<VirtualWorkspace> VirtualWorkspaces { get { return _VirtualWorkspaces; } }
        public List<Document> ForgottenUniverse { get { return _ForgottenUniverse; } }
        public List<Document> VoidUniverse { get { return _VoidUniverse; } }
        // public List<Document> LinkUniverse { get { return _LinkUniverse; } }
        public ClueManager ClueManager { get { return _ClueManager; } }
        public VirtualWorkspace ActiveVW { get { return _ActiveVW; } }

        // Extra Accessors
        public bool IsUserSet
        {
            get
            {
                return string.IsNullOrWhiteSpace(Username) == false && string.IsNullOrWhiteSpace(Password.Content) == false;
            }
        }
        #endregion Home Data

        #region Document Management

        #region Importing
        /// <summary>
        /// Add a single target to Home, return the document(s) generated during the process that are not classified (by a clue or VA).
        /// If no Exception generated then the operation succeeds.
        /// Documetns are returned immediately, but actual file operations happen in a seperate thread, a callback needs to be provided to get notification.
        /// </summary>
        /// Description (Stage 1): Load all documents specified by sources and return as soon as document object instances are generated - while actual file movement happens in the background
        /// Call FinishImport to do file operation and get a chance to notify user
        internal /*async Task<List<Document>>*/ List<Document> ImportTargets(string[] sources, ImportAction action, ImportMode mode, out int documentCount)
        {
            // Set up state
            bImportFinished = false;

            // A list of newly generated documents
            List<Document> virtualArchives = new List<Document>();
            newlyImportedUnorganizedDocuments = new List<Document>();
            lastImportAction = action;

            // Stage 1: Generate documents (Async)
            // Foreach source generate documents for it
            foreach (string path in sources)
            {
                switch (SystemHelper.IsFolder(path))
                {
                    // Handle as a folder
                    case true:
                        Document virtualArchive = null;
                        newlyImportedUnorganizedDocuments.AddRange(ImportFolder(path, mode, out virtualArchive));
                        if (virtualArchive != null) virtualArchives.Add(virtualArchive);
                        break;
                    // Handle as a file
                    case false:
                        newlyImportedUnorganizedDocuments.Add(ImportFile(new FileInfo(path)/*, new DirectoryInfo(Path.GetDirectoryName(path))*/));
                        break;
                    // Error
                    case null:
                        throw new InvalidOperationException("Path doesn't represent a file/folder.");
                }
            }

            // Return appropriately about current status
            documentCount = newlyImportedUnorganizedDocuments.Count;
            if (sources.Length == 1 && SystemHelper.IsFolder(sources[0]) == false) return newlyImportedUnorganizedDocuments;     // If it's a single file, return the document
            else // Depending on whether the documents are added in a classified manner
            {
                switch (mode)
                {
                    case ImportMode.GenerateClues:
                        newlyImportedUnorganizedDocuments.Clear(); // If it's classified, then return an empty list
                        break;
                    case ImportMode.GenerateVirtualArchive:
                        return virtualArchives;
                }
            }

            return newlyImportedUnorganizedDocuments;
        }
        // Import a folder into Home, return all Documents created
        // virtualArchive is assigned only when we are importing a VR
        private List<Document> ImportFolder(string path, ImportMode mode, out Document virtualArchive)
        {
            List<Document> list = new List<Document>();
            DirectoryInfo source;
            virtualArchive = null;
            switch (mode)
            {
                case ImportMode.GenerateClues:
                    source = new DirectoryInfo(path);
                    RecursiveGenerateDocument(list, source, source, true);
                    return list;
                case ImportMode.GenerateVirtualArchive:
                    // Create a virtual archive
                    DirectoryInfo vardir = new DirectoryInfo(path);
                    Archive var = new Archive(false, null, vardir.Name, vardir.CreationTime.ToString("MMMM dd, yyyy HHmmss"));  // Notice the VA doesn't have a path assigned
                    source = new DirectoryInfo(path);
                    RecursiveGenerateDocument(list, source, source, false, var.Roots[0]);
                    // Materialize: unlike other three methods, import as VA will involve creating a new document that has its unique storage (unlike real archive which just refers to a real folder)
                    var.Materialize();
                    // Add to home state
                    RegisterDocument(var);
                    // Send as output
                    virtualArchive = var;
                    list.Add(var);
                    return list;
                case ImportMode.NoClassification:
                    source = new DirectoryInfo(path);
                    RecursiveGenerateDocument(list, source, source);
                    return list;
                case ImportMode.UseAsArchive:
                    DirectoryInfo ardir = new DirectoryInfo(path);
                    Document newDocument;
                    newDocument = new Archive(true, path, ardir.Name, ardir.CreationTime.ToString("MMMM dd, yyyy HHmmss"));
                    // Add to home state
                    RegisterDocument(newDocument);
                    list.Add(newDocument);
                    return list;
            }
            return null;
        }
        // Resursively import files and subfolders in a folder
        private void RecursiveGenerateDocument(List<Document> list, DirectoryInfo sourceDir, DirectoryInfo dir, bool bWithClues = false, ArchiveNode root = null)
        {
            // Get all files in current folder and generate documents for them.
            FileInfo[] fis = dir.GetFiles();
            foreach (FileInfo fi in fis)
            {
                if (fi.Attributes == FileAttributes.Normal || fi.Attributes == FileAttributes.Archive ||
                        ((fi.Attributes & FileAttributes.Archive) == FileAttributes.Hidden && (fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))  // https://msdn.microsoft.com/en-us/library/system.io.fileattributes(v=vs.110).aspx
                {
                    try
                    {
                        string trial = fi.FullName; // Try access fullName to handle exceptions

                        Document doc = ImportFile(fi, bWithClues, sourceDir);
                        if (root != null) root.AddNode(doc.GetMeta("name"), doc);
                        list.Add(doc);
                    }
                    catch (PathTooLongException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with file \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, fi.Name, fi.Attributes.ToString(), fi.DirectoryName)); }
                    catch (System.Security.SecurityException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with file \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, fi.Name, fi.Attributes.ToString(), fi.DirectoryName)); }
                }
                else (App.Current as App).Log(UrgencyLevel.Notice, string.Format("File \"{0}\" skipped due to access level: \"{1}\"; File location: {2}", fi.Name, fi.Attributes.ToString(), fi.DirectoryName));
            }
            // Iterate subdirectory
            DirectoryInfo[] dis = dir.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden &&
                    (di.Attributes & FileAttributes.System) != FileAttributes.System)
                    try {
                        string trial = di.FullName;  // Try access fullName to handle exceptions

                        ArchiveNode sub = root == null ? null : root.AddNode(di.Name);
                        RecursiveGenerateDocument(list, sourceDir, di, bWithClues, sub);
                    }
                    catch (PathTooLongException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with folder \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, di.Name, di.Attributes.ToString(), di.Parent.FullName)); }
                    catch (System.Security.SecurityException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with folder \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, di.Name, di.Attributes.ToString(), di.Parent.FullName)); }
                else (App.Current as App).Log(UrgencyLevel.Notice, string.Format("Folder \"{0}\" skipped due to access level: \"{1}\"; Folder location: {2}", di.Name, di.Attributes.ToString(), di.FullName));
            }
        }

        /// <summary>
        ///  This is a one-shot reference-only with Clue import for easy interfacing with an existing computer
        /// </summary>
        internal /*async Task or call as a task*/ void ImportFolderSelective(TreeFolderInfo folder)
        {
            // Prepare holders
            List<Document> list = new List<Document>();

            // Recusrive process
            RecursiveGenerateDocumentSelective(list, folder);
        }
        // Resursived import files and subfolders in a folder
        private void RecursiveGenerateDocumentSelective(List<Document> list, TreeFolderInfo dir)
        {
            if (dir.bSelected == false && dir.IsTemp) return;

            // Prepare for next stage
            if (dir.IsTemp)
            {
                // Clear existing content
                dir.Folders.Clear();

                // Add new content
                MULTITUDE.Class.Facility.TreeFolderInfo.FolderGenerator(dir);
            }

            if (dir.bSelected)
            {
                // Get all files in current folder and generate documents for them.
                System.Collections.ObjectModel.ObservableCollection<TreeFileInfo> fis = dir.Files;
                foreach (TreeFileInfo fi in fis)
                {
                    if (fi.bSelected == true &&
                        (fi.Info.Attributes == FileAttributes.Normal || fi.Info.Attributes == FileAttributes.Archive ||
                        ((fi.Info.Attributes & FileAttributes.Archive) == FileAttributes.Hidden && (fi.Info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))) // https://msdn.microsoft.com/en-us/library/system.io.fileattributes(v=vs.110).aspx
                    {
                        try {
                            string trial = fi.Info.FullName; // Try access fullName to handle exceptions

                            // Truncate a clue with up-to three fragments
                            string pathWithoutDrive = fi.Info.DirectoryName.Substring(Path.GetPathRoot(fi.Info.DirectoryName).Length);    // http://stackoverflow.com/questions/7772520/removing-drive-or-network-name-from-path-in-c-sharp
                            int folderCount = pathWithoutDrive.Count(f => f == '\\') + 1;
                            if (folderCount > 3) pathWithoutDrive = pathWithoutDrive.Substring(GetLastNthIndex(pathWithoutDrive, '\\', 3) + 1);
                            string clueString = NeutralizePath(pathWithoutDrive);
                            Document doc = ImportFile(fi.Info, clueString);
                            list.Add(doc);
                        } 
                        catch (PathTooLongException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with file \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, fi.Info.Name, fi.Info.Attributes.ToString(), fi.Info.DirectoryName)); }
                        catch (System.Security.SecurityException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with file \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, fi.Info.Name, fi.Info.Attributes.ToString(), fi.Info.DirectoryName)); }
                    }
                    else (App.Current as App).Log(UrgencyLevel.Notice, string.Format("File \"{0}\" skipped due to access level: \"{1}\"; File location: {2}", fi.Info.Name, fi.Info.Attributes.ToString(), fi.Info.DirectoryName));
                }
            }

            // Iterate subdirectory not matter they are selected or not: If a folder is not selected its children might still be 
            foreach (TreeFolderInfo sub in dir.Folders)
            {
                if ((sub.Info.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden &&
                    (sub.Info.Attributes & FileAttributes.System) != FileAttributes.System)
                    try { string trial = sub.Info.FullName; RecursiveGenerateDocumentSelective(list, sub); } // Try access fullName to handle exceptions
                    catch (PathTooLongException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with folder \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, sub.Info.Name, sub.Info.Attributes.ToString(), sub.Info.Parent.FullName)); }
                    catch (System.Security.SecurityException e) { (App.Current as App).Log(UrgencyLevel.Exception, string.Format("Exception \"{0}\" with folder \"{1}\" with access level: \"{2}\"; Parent folder location: {3}.", e, sub.Info.Name, sub.Info.Attributes.ToString(), sub.Info.Parent.FullName)); }  
                else (App.Current as App).Log(UrgencyLevel.Notice, string.Format("Folder \"{0}\" skipped due to access level: \"{1}\"; Folder location: {2}", sub.Info.Name, sub.Info.Attributes.ToString(), sub.Info.FullName));
            }
        }
        private int GetLastNthIndex(string s, char t, int n)
        {
            int count = 0;
            for (int i = s.Length -1; i >= 0; i--)
            {
                if (s[i] == t)
                {
                    count++;
                    if (count == n)
                    {
                        return i;
                    }
                }
            }
            return -1;
            // Ref: https://stackoverflow.com/questions/2571716/find-nth-occurrence-of-a-character-in-a-string
        }
        private string NeutralizePath(string original)
        {
            // Also might want to eliminate redundancy in case, sometimes, e.g. me, use redundant paths like handbook-handbook-something
            return original.Replace('-', '_').Replace('\\', '-').Replace('[', '(').Replace(']', ')').Replace("+", "and");  // Windows
        }
        // Import a file into Home, return the document created
        private Document ImportFile(FileInfo file, bool bWithClues, DirectoryInfo sourceDir) // SourceDir indicate where drop action was initiated
        {
            if (bWithClues && sourceDir != null)
            {
                // http://stackoverflow.com/questions/7772520/removing-drive-or-network-name-from-path-in-c-sharp
                string pathWithoutSource = file.DirectoryName.Substring(sourceDir.Parent.FullName.Length);
                int folderCount = pathWithoutSource.Count(f => f == '\\') + 1;
                if (folderCount > 3) pathWithoutSource = pathWithoutSource.Substring(GetLastNthIndex(pathWithoutSource, '\\', 3) + 1);
                string pathInClueForm = NeutralizePath(pathWithoutSource);

                return ImportFile(file, pathInClueForm);
            }
            else return ImportFile(file, null);
        }

        private Document ImportFile(FileInfo file, string clueString = null)
        {
            // Check file type depending on known suffix
            string extension = file.Extension;
            DocumentType type;
            // No extension
            if (extension == "")
            {
                type = DocumentType.Unkown;
            }
            // Text types
            else if (extension == PlainText.FileSuffix || StringHelper.ExtensionContains(PlainText.Extensions, extension) == true)
            {
                type = DocumentType.PlainText;
            }
            // Mardown plus type
            else if (extension == MarkdownPlus.FileSuffix)   // MULTITUDE Markdown plus
            {
                type = DocumentType.MarkdownPlus;
            }
            // Virtual archive type
            else if (extension == Archive.FileSuffix)   // MULTITUDE Virtual Archive
            {
                type = DocumentType.VirtualArchive;
            }
            // Data collection type
            else if (extension == DataCollection.FileSuffix)   // MULTITUDE data collection
            {
                type = DocumentType.DataCollection;
            }
            // Graph type
            else if (extension == Graph.FileSuffix)   // MULTITUDE graph
            {
                type = DocumentType.Graph;
            }
            // Command type
            else if (StringHelper.ExtensionContains(Command.Extensions,extension) == true || extension == Command.FileSuffix)    // Common executables
            {
                type = DocumentType.Command;
            }
            // Web type
            else if (StringHelper.ExtensionContains(Web.Extensions,extension) || extension == Web.FileSuffix)    // Web
            {
                type = DocumentType.Web;
            }
            // Playlist type
            else if (extension == Playlist.FileSuffix)    // MULTITUDE playlist
            {
                type = DocumentType.PlayList;
            }
            // (Supported) Image types
            else if (StringHelper.ExtensionContains(ImagePlus.Extensions,extension) == true || extension == ImagePlus.FileSuffix)    // Image + MULTITUDE Image Plus
            {
                type = DocumentType.ImagePlus;
            }
            // (Supported) Sound types
            else if (StringHelper.ExtensionContains(Playlist.SoundExtensions,extension) == true)
            {
                type = DocumentType.Sound;
            }
            // (Supported)  Video types
            else if (StringHelper.ExtensionContains(Playlist.VideoExtensions,extension) == true)
            {
                type = DocumentType.Video;
            }
            else if (extension == VirtualWorkspace.FileSuffix)
            {
                type = DocumentType.VirtualWorkspace;
            }
            // Other types
            else
            {
                type = DocumentType.Others;
            }

            // Generate a file depending on file type
            Document newDocument = null;
            switch (type)
            {
                case DocumentType.PlainText:
                    newDocument = PlainText.Import(file);
                    break;
                case DocumentType.MarkdownPlus:
                    newDocument = MarkdownPlus.Import(file);
                    break;
                case DocumentType.Archive:
                    throw new InvalidOperationException("Archive isn't a file and cannot be imported as a file.");
                case DocumentType.VirtualArchive:
                    newDocument = Archive.Import(file);
                    break;
                case DocumentType.DataCollection:
                    newDocument = DataCollection.Import(file);
                    break;
                case DocumentType.Graph:
                    newDocument = Graph.Import(file);
                    break;
                case DocumentType.Command:
                    newDocument = Command.Import(file);
                    break;
                case DocumentType.Web:
                    newDocument = Web.Import(file);
                    break;
                case DocumentType.PlayList:
                    newDocument = Playlist.Import(file);
                    break;
                case DocumentType.ImagePlus:
                    newDocument = ImagePlus.Import(file);
                    break;
                case DocumentType.VirtualWorkspace:
                    newDocument = VirtualWorkspace.Import(file);
                    break;
                case DocumentType.Sound:
                case DocumentType.Video:
                case DocumentType.Others:
                case DocumentType.Unkown:
                default:
                    newDocument = new GenericDocument(type, file.FullName, Path.GetFileNameWithoutExtension(file.Name), file.LastWriteTime.ToString("MMMM dd, yyyy HHmmss"));
                    break;
            }
            // Add extra available information
            newDocument.AddMeta("extension", extension);

            // Generate Clues and Add to home
            if (string.IsNullOrWhiteSpace(clueString) == false)
            {
                newDocument.AddClue(new Clue(NeutralizeClue(clueString)));
                RegisterDocument(newDocument);
            }
            else
            {
                RegisterDocument(newDocument); 
            }

            return newDocument;
        }

        private string NeutralizeClue(string originalClueString)
        {
            // Ref: https://stackoverflow.com/questions/25154701/how-to-replace-any-of-these-characters-in-a-strin
            // Ref: https://stackoverflow.com/questions/7265315/replace-multiple-characters-in-a-string
            var pattern = Clue.InvalidClueCharacters;
            return new string(originalClueString.Where(ch => !pattern.Contains(ch)).ToArray());
        }
        // Register an imported/created document by: 1. Adding it to list 2. Giving it a GUID
        private void RegisterDocument(Document document)  // Add a document reference
        {
            // Add document to list
            if (document.Clues.Count == 0 && string.IsNullOrWhiteSpace(document.Name)) ForgottenUniverse.Add(document);
            else Documents.Add(document); 
            
            // <Development> Notice Documents.Count is not a reliable way of defininig ID since Documents might be removed and ID will be incorrect; We should instead keep a global integer value and keep incrementing only without going back.
            document.GUID = DocumentGUIDCounter;
            DocumentGUIDCounter++;

            if (DocumentGUIDCounter >= int.MaxValue) throw new IndexOutOfRangeException("Congratulations! You have reached the upper document count limit of this software, contact developer for extra help of consider creating a new Home to manage extra documents.");
        }
        internal void Relocate(Document document)
        {
            if (ForgottenUniverse.Contains(document))
            {
                if (document.Clues.Count == 0 && string.IsNullOrWhiteSpace(document.Name)) return;
                else
                {
                    ForgottenUniverse.Remove(document);
                    Documents.Add(document);
                }
            }
            else
            {
                if (document.Clues.Count == 0 && string.IsNullOrWhiteSpace(document.Name))
                {
                    Documents.Remove(document);
                    ForgottenUniverse.Add(document);
                }
                else return;
            }
        }
        internal void Delete(Document document)
        {
            // Remove from documents or forgotten universe to Void space
            int index = Documents.IndexOf(document);
            if (index != -1) { Documents.RemoveAt(index); VoidUniverse.Add(document); UnClassifyDocument(document); return; }
            else
            {
                index = ForgottenUniverse.IndexOf(document);
                if (index != -1) { ForgottenUniverse.RemoveAt(index); VoidUniverse.Add(document); UnClassifyDocument(document); return; }
            }
            throw new IndexOutOfRangeException("Specified documents doesn't exist in current home.");
        }

        private void UnClassifyDocument(Document doc)
        {
            // Remove document's clues from Cluemanager
            ClueManager.Manager.RemoveAllClues(doc);
        }

        // Physically delete the document from Home data
        internal void Eliminate(Document doc)
        {
            int index = VoidUniverse.IndexOf(doc);
            if (index == -1) throw new IndexOutOfRangeException("Specified document doesn't exist.");
            // Remove
            VoidUniverse.RemoveAt(index);
            // Disintegrate
            doc.Disintegrate();
        }
        internal void Recover(Document doc)
        {
            int index = VoidUniverse.IndexOf(doc);
            if (index == -1) throw new IndexOutOfRangeException("Specified document doesn't exist.");
            // Move from void universe back to forgotten or document
            VoidUniverse.RemoveAt(index);
            if (doc.HasAnyClassification) Documents.Add(doc);
            else ForgottenUniverse.Add(doc);
            // Register with clue manager
            ClueManager.Manager.AddAllClues(doc);
        }
        /// <summary>
        /// Mode 2: Given a home location, copy all its content into current home (Used for backup or doing a fork/expansion; Delete original home by hand if user wants)
        /// Mode 2: Given a home location, externally reference it, i.e. use that home's documents all as external
        /// Mode 3: Thinking of combining that with Multiple User home locations to enable centralized search (but of course cross reference is still not allowed)
        /// </summary>
        /// We might provide two means of doing that: one is to make an indepdent copy of an existing home; Another is to make a mapping using secondary home address to create a bunch of documents refering to documents in another home (only HOME ID and documetn GUID is recorded, the mapping from home ID to home location is of course specified by user). Using second way there might be many lavel of indirections (home documents refer to home documents refer to home documents), thus we might also provide some means to resolve existing references that point to the same physical location: be it online or on local disk) - those are all implementation details that's kept inside a home and shiled away from other classes
        /// <param name="homeLocation"></param>
        /// <returns></returns>
        internal List<Document> ImportHome(string homeLocation)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region File System Interaction Layer
        // Temp State Variables: Last Import Action States
        private List<Document> newlyImportedUnorganizedDocuments;
        private ImportAction lastImportAction;
        private bool? bImportFinished = null;
        /// <summary>
        /// Description (Stage 2): Called after ImportTargets()
        /// Raise file exceptions if any
        /// </summary>
        internal/* async Task*/void FinishImport()
        {
            // Stage 2: Do actual movement if any (in a seperate thread, not through async mechanism)
            foreach (Document doc in newlyImportedUnorganizedDocuments)
            {
                try
                {
                    doc.Internalize(lastImportAction);
                }
                catch (UnauthorizedAccessException)
                {
                    // <Development> Try resolve this from source (i.e. during indexing) so we don't mess up with other things -- to some extent we can just check for files beginning with ~ on Windows
                    // Remove all records of that document
                    newlyImportedUnorganizedDocuments.Remove(doc);
                    // Remove from documents
                    Documents.Remove(doc);
                    // Remove from forgotten universer 
                    ForgottenUniverse.Remove(doc);
                    // Remove clue records
                    ClueManager.Manager.RemoveAllClues(doc);
                    // Also remove from .....
                }
            }
            bImportFinished = true;

            // Stage 3: Do an auto saving (Async)
            Save();
            bImportFinished = null;
        }
        #endregion

        // Cache handling
        // Text Document handling
        #endregion

        #region Link Search Helper Constructs
        #endregion

        #region Access Interface
        internal VirtualWorkspace TryGetActiveVW(Coordinate location)
        {
            foreach (VirtualWorkspace vw in VirtualWorkspaces)
            {
                if (vw.VWCoordinate == location)
                    return vw;
            }
            return null;
        }
        // Get VW from Index, if not currently exist, then create one
        internal VirtualWorkspace GetActiveVW(Coordinate location)
        {
            foreach (VirtualWorkspace vw in VirtualWorkspaces)
            {
                if (vw.VWCoordinate == location)
                return vw;
            }

            VirtualWorkspace newVW = new VirtualWorkspace(location);
            VirtualWorkspaces.Add(newVW);
            return newVW;
        }
        internal void SetActiveVW(VirtualWorkspace active)
        {
            if (VirtualWorkspaces.Contains(active) == false || active.VWCoordinate == null) throw new IndexOutOfRangeException("Specified VW isn't part of VW space");

            _ActiveVW = active;
        }
        // Return new one; Caller decide what to do with beingReplaced
        internal VirtualWorkspace RemoveVWAndGetANewEmptyOne(VirtualWorkspace beingReplaced)
        {
            if (VirtualWorkspaces.Contains(beingReplaced) == false) throw new IndexOutOfRangeException("Specified VW isn't part of VW space");

            // Generate a new substitute at current location
            VirtualWorkspace newVW = new VirtualWorkspace(beingReplaced.VWCoordinate);
            beingReplaced.VWCoordinate = null;
            VirtualWorkspaces.Remove(beingReplaced);
            VirtualWorkspaces.Add(newVW);

            // Save();  // <Development> Pending deciding when and where to perform this
            return newVW;
        }
        internal VirtualWorkspace PackVWAndGetANewEmptyOne(VirtualWorkspace beingReplaced)
        {
            if (VirtualWorkspaces.Contains(beingReplaced) == false) throw new IndexOutOfRangeException("Specified VW isn't part of VW space");

            // Generate a new substitute at current location
            VirtualWorkspace newVW = new VirtualWorkspace(beingReplaced.VWCoordinate);
            beingReplaced.VWCoordinate = null;
            VirtualWorkspaces.Remove(beingReplaced);
            VirtualWorkspaces.Add(newVW);
            // Registration
            RegisterDocument(beingReplaced);
            beingReplaced.Materialize();
            Save();  // <Development> Pending deciding when and where to perform this
            return newVW;
        }
        // Get document from ID: Notice ID has nothing to do with index
        // For a more efficient implementation we might use a table (from GUID to document index in list, that is, a int-int 8 byte per entry table; Approximately 2^22 entries will be enough for normoal use, which is 32MB, fair size for such a table; For at that time (i.e. 2^22 documents), the storage of our _PhysicalLocation will take around 1GB, assuming 128 char each doc on average with 2 byte per char; Letting alone auxiliary information like clues and meta storage, also not mentioning actual file-behind)
        public Document GetDocument(int ID)
        {
            foreach (Document doc in Documents)
            {
                if (doc.GUID == ID) return doc;
            }
            foreach (Document doc in ForgottenUniverse)
            {
                if (doc.GUID == ID) return doc;
            }
            return null;
        }

        internal bool IsDocumentOfType(int linkGUID, DocumentType compare)
        {
            return GetDocument(linkGUID).Type == compare;
        }
        // Create a new document
        public Document CreateDocument(DocumentType type, string name = null)
        {
            // Create a document instance
            Document document = null;
            switch (type)
            {
                case DocumentType.PlainText:
                    document = new PlainText(null, name);
                    break;
                case DocumentType.MarkdownPlus:
                    document = new MarkdownPlus(null, name);
                    break;
                case DocumentType.Archive:
                    throw new NotImplementedException();
                    break;
                case DocumentType.VirtualArchive:
                    document = new Archive(false, null, name);
                    break;
                case DocumentType.DataCollection:
                    document = new DataCollection(null, name);
                    break;
                case DocumentType.Graph:
                    document = new Graph(null, name);
                    break;
                case DocumentType.Command:
                    throw new NotImplementedException();
                    break;
                case DocumentType.Web:
                    throw new NotImplementedException();
                    break;
                case DocumentType.PlayList:
                    throw new NotImplementedException();
                    break;
                case DocumentType.ImagePlus:
                case DocumentType.Sound:
                case DocumentType.Video:
                case DocumentType.VirtualWorkspace:
                case DocumentType.Others:
                case DocumentType.Unkown:
                default:
                    throw new InvalidOperationException("The specified document isn't supported for creation from scratch yet.");
            }

            // Register
            RegisterDocument(document);

            // Create a physical representation of the document
            document.Materialize();

            // Also save home
            Save();

            // Return
            return document;
        }

        // Create a playlist from image, sound or video documents
        public Playlist CreatePlaylistFromMedia(Document mediaDocRef)
        {
            Playlist newPlaylist = null;
            if (mediaDocRef.Type == DocumentType.Sound || mediaDocRef.Type == DocumentType.ImagePlus || mediaDocRef.Type == DocumentType.Video)
            {
                newPlaylist = new Playlist(mediaDocRef);
            }
            else throw new InvalidOperationException("Invalid media document type.");

            // Register
            RegisterDocument(newPlaylist);

            // Create a physical representation of the document
            newPlaylist.Materialize();

            // Also save home
            Save();

            // Return
            return newPlaylist;
        }

        // Return coverted VA
        public Archive ConvertVWToVAAndRegister(VirtualWorkspace vw)
        {
            Archive archive = new Archive(vw);

            // Register
            RegisterDocument(archive);

            // Create a physical representation of the document
            archive.Materialize();

            // Also save home
            Save();

            // Return
            return archive;
        }

        // Internalize all documents in current home
        public void InternalizeAll(ImportAction action)
        {
            foreach (Document doc in Documents)
            {
                // If a documetn doesn't have a physical form, materialize it (which is not likely since we materialize it immediately when we create it)
                // ...
                // Otherwise internalize it
                doc.Internalize(action);
            }
        }
        #endregion Access Interface

        #region Data Management
        // response to notification from EverythingService
        internal void OnFileSystemEntryUpdate(FileSystemUpdate update)
        {
            throw new NotImplementedException();
        }
        public void Lock()
        {
            _bLocked = true;
            // <Development> Might also want to prevent certain data access?
        }
        public void UnLock()
        {
            _bLocked = false;
        }
        public static readonly string HomeDataFileName = @"Data.home";
        public static Home Current
        {
            get
            {
                return (App.Current as App).CurrentHome;
            }
        }
        // Home Level Configurations, e.g. user name and password information; Notice VW configurations are done directly on VW itself
        public void ChangeConfigurations(string name, string userID, string password)
        {
            Appellation = name;
            Username = userID;
            Password = new EnctryptedString(password);
        }
        /// <summary>
        /// Load home from location or generate one if that's an empty folder/non-existing folder; If a folder has content yet doesn't have a home file then it's invalid
        /// </summary>
        /// <param name="homeLocation">homeLocation as a directory should be guaranteed to be accisieble (yet not necessarily exist) by caller</param>
        /// <returns></returns>
        public static Home Load(string homeLocation)
        {
            if (string.IsNullOrWhiteSpace(homeLocation)) throw new InvalidOperationException("Home location must be a valid path.");

            // Set global data
            Home.Location = homeLocation;

            // Also load configurations if we plan to support them (i.e. as a seperate .conf file)
            // ... <Development> Currently no configurations are implemented

            // Read home data
            string homeFile = System.IO.Path.Combine(homeLocation, HomeDataFileName);
            // If folder and home file exists, Load existing home
            if (System.IO.Directory.Exists(homeLocation) && System.IO.File.Exists(homeFile))
            {
                // Load serialized data
                Stream fileStream = File.OpenRead(homeFile);
                BinaryFormatter deserializer = new BinaryFormatter();
                Home home = (Home)deserializer.Deserialize(fileStream);
                // b = (Home)deserializer.Deserialize(fileStream);
                // c = (List<TestClass>)deserializer.Deserialize(fileStream);
                fileStream.Close();

                // Generate extra application data
                // ...

                // Also setup some static members
                ClueManager.Manager = home.ClueManager;

                return home;
            }

            // If the folder doesn't exist, create one
            if (System.IO.Directory.Exists(homeLocation) == false)    System.IO.Directory.CreateDirectory(homeLocation);// Create a folder
            
            // Generate a blank home there if the folder isn't occupied
            if (System.IO.Directory.GetFileSystemEntries(homeLocation).Length == 0)
            {
                // Create a home
                Home newHome = new Home(homeLocation);
                // Save home
                newHome.Save();
                // Return home
                return newHome;
            }
            // Non-multitude folder with unrelated contents are not allowed
            else
            {
                throw new InvalidOperationException("Home location must be empty.");
            }
        }
        /// <summary>
        /// Pack all contents in current home in a self-contained manner and compress the result
        /// </summary>
        /// <param name="path"></param>
        internal void PackAndExport(string path)
        {
            if (path == Home.Location) path = System.IO.Directory.GetParent(path).FullName;

            throw new NotImplementedException();
        }

        internal void ImportPackedHome()
        {
            // Uncompress and read everthing; Add documents and internalize them
            // ...
            throw new NotImplementedException();
        }

        // Serialize: Save current home data into a home data file -- normally Home decides when to save itself
        private void Save()
        {
            Stream fileStream = File.Create(System.IO.Path.Combine(Location, HomeDataFileName));
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(fileStream, this);
            // serializer.Serialize(TestFileStream, b);
            // serializer.Serialize(TestFileStream, c);
            fileStream.Close();
        }
        public void RequestSave()
        {
            if (bImportFinished == null || bImportFinished == true)
            {
                Save();
            }
            else
            {
                throw new InvalidOperationException("Home import not finished before attemping saving home data.");
            }
        }
        #endregion Data Management
    }
}
