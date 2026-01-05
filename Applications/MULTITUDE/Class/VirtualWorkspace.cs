using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MULTITUDE.Class.DocumentTypes;
using System.Runtime.Serialization;
using MULTITUDE.Class.Facility;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MULTITUDE.Class
{
    public class VWView : INotifyPropertyChanged
    {
        internal void SetVW()
        {
            _VW = VW;
        }
        private VirtualWorkspace _VW;
        internal VirtualWorkspace VW { get { return _VW; } }

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Name
        {
            get { return this._VW.Name; }
            set
            {
                if (value != this._VW.Name)
                {
                    this._VW.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<VWStackTraceView> StackTrace
        {
            get { return this._VW.GetStackTrace(); }
        }
        public int DocumentCount
        {
            get { return this._VW.GetDocumentCount(); }
        }
        #endregion
    }

    public class VWStackTraceView: INotifyPropertyChanged
    {
        public VWStackTraceView()
        {
            DisplayName = null;
        }
        public VWStackTraceView(string displayName)
        {
            DisplayName = displayName;
        }

        internal void SetVW(VirtualWorkspace vw)
        {
            _VW = vw;
        }
        private VirtualWorkspace _VW;
        internal VirtualWorkspace VW { get { return _VW; } }

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string DisplayName = null;
        public string Name
        {
            get { if (DisplayName != null) return DisplayName; else return this._VW.Name; }
        }
        #endregion
    }

    #region Gadget Related
    [Serializable]
    enum GadgetType
    {
        Gadget1,
        QuickNote,      // TotalImagine Cloud Note System
        CommandConsole, // A hybrid of custom commands, SIS controls, and linux/windows command emutations
        TeamChat,       // TotalImagine team chat
        MultiFunClock   // Alarm, Timer, Stopwatch
    }

    // Represetn type and location of VW Gadgets
    [Serializable]
    class VWGadget  // by design gadgets normally shouldn't have states; If it doesn't need to save some userdata, then create a subclass for that, and mostly, use some custom serialization logic to implement some state saving (because notice gadgets can be closed in real time during VW configuration changes)
    {
        public VWGadget(GadgetType type)
        {
            Type = type;
        }

        public CanvasRelativeLocation Location { get; set; }
        public GadgetType Type { get; set; }
    }
    #endregion

    #region Location Related

    [Serializable]
    enum RelativeLocation
    {
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }
    [Serializable]
    class CanvasRelativeLocation
    {
        public CanvasRelativeLocation()
        {

        }
        public CanvasRelativeLocation(RelativeLocation relativity, double hOff, double vOff)
        {
            Relativity = relativity;
            HorizontalDistance = hOff;
            VerticalDistance = vOff;
        }
        public RelativeLocation Relativity { get; set; }
        public double HorizontalDistance { get; set; }
        public double VerticalDistance { get; set; }
    }
    #endregion

    #region Documetn Icon Related
    // Represents a recrangular area; Indexing format not specified by this class; See specific instances for their convention
    [Serializable]
    class IconArea
    {
        public CanvasRelativeLocation Location { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public IconArea(CanvasRelativeLocation loc, double w, double h)
        {
            Location = loc;
            Width = w;
            Height = h;
        }
    }

    // Represents the documents that are placed on this VW
    [Serializable]
    class DocumentIcon
    {
        public DocumentIcon(int ID, IconArea occupation)
        {
            DocumentID = ID;
            Occupation = occupation;
            MDHideBackground = false;
        }

        // ID information
        public int DocumentID { get; set; }
        // Placement information
        public IconArea Occupation { get; set; }

        // Extra Information
        public bool MDHideBackground { get; set; }
    }

    [Serializable]
    class Page
    {
        public Page()
        {
            Documents = new List<DocumentIcon>();
        }

        public List<DocumentIcon> Documents { get; set; }

        public DocumentIcon AddDocument(Document doc, IconArea occupation)
        {
            DocumentIcon info = new DocumentIcon(doc.GUID, occupation);
            Documents.Add(info);
            return info;
        }

        public void RemoveDocument(DocumentIcon iconInfo)
        {
            if (Documents.Contains(iconInfo) == false) throw new IndexOutOfRangeException("Specified icon doesn't exist in current page.");
            else Documents.Remove(iconInfo);
        }
    }
    #endregion

    #region Tool Tray Related
    //[Serializable]
    //class ToolTray
    //{
    //    public List<Document> ToolTrayItems { get; set; }
    //}
    #endregion

    enum EdgeDirection
    {
        LeftEdge,
        RightEdge,
        TopEdge,
        BottomEdge,
        TL,
        TR,
        BL,
        BR,
        Center
    }

    [Serializable]
    public struct Coordinate
    {
        public static Coordinate Default = new Coordinate(0,0);

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int X;
        public readonly int Y;

        public override bool Equals(Object obj)
        {
            return obj is Coordinate && this == (Coordinate)obj;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
        public static bool operator ==(Coordinate x, Coordinate y)
        {
            return x.X == y.X && x.Y == y.Y;
        }
        public static bool operator !=(Coordinate x, Coordinate y)
        {
            return !(x == y);
        }
    }

    /// <summary>
    /// This is a pure data class
    /// </summary>
    [Serializable]
    class VirtualWorkspace: Document, ISerializable
    {
        #region User VW Configurations
        public string BackgroundImageClue { get; set; } // VW Background Image
        public string BackgroundMelodyClue { get; set; }    // VW Background Melody
        public static readonly string DefaultBackgroundImageClue = "background-";
        public static readonly string DefaultBackgroundMelodyClue = "rhythm-";
        #endregion
        #region Content and Layout Data
        public Coordinate? VWCoordinate { get; set; }   // VW Coordinate: x minus from Left to Right positive, Y minus from up to Down positive // VW Relative Location: Location of this VW in counter clockwise spiral coordinate; Begin at 1; -1 indicates a packed VW document that isn't managed under active VW spaces
        public int PageIndex { get; set; }   // Index begin from 0
        public List<VWGadget> Gadgets { get; set; } // VW Gadgets
        // public ToolTray ToolTray { get; set; }  // VW Tooltray
        public List<Page> Pages { get; set; } // VW Pages
        public List<VirtualWorkspace> OpenedVWTrace { get; set; }  // Stack of VWs that are opened under current VW
        #endregion
        public static string FileSuffix = ".MULTITUDEvw";

        #region Constructor and Serialization Handling
        public const string DefaultWorkspaceSubject = "My Workspace";   // https://stackoverflow.com/questions/55984/what-is-the-difference-between-const-and-readonly
        public VirtualWorkspace(Coordinate? location = null, string name = DefaultWorkspaceSubject, string path = null, string creationdate = null)
            :this(DocumentType.VirtualWorkspace, path, 
                 (name == DefaultWorkspaceSubject? VirtualWorkspace.GetRandomWorkspaceName() : name), 
                 creationdate == null ? SystemHelper.CurrentTimeFileNameFriendly : creationdate, location){}
        // A wrapper for underlying base
        public VirtualWorkspace(DocumentType type, string path, string metaname, string date, Coordinate? location = null)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {             
            VWCoordinate = location;
            PageIndex = 0;
            Gadgets = new List<VWGadget>();
            Pages = new List<Page>();   // A page list with 5 pages
            Pages.Add(new Page());
            Pages.Add(new Page());
            Pages.Add(new Page());
            Pages.Add(new Page());
            Pages.Add(new Page());
            // ToolTray = new ToolTray();
            OpenedVWTrace = new List<VirtualWorkspace>();

            BackgroundImageClue = DefaultBackgroundImageClue;
            BackgroundMelodyClue = DefaultBackgroundMelodyClue;
        }

        private static Random rnd = new Random();
        private static string[] PersudoWorkspaceNames = 
            { "House Work", "Documents", "Musics", "My Projects", "Weekend Arragements", "A Default Workspace", "Arts", "History" };
        private static string GetRandomWorkspaceName()
        {
            Home home = (App.Current as App).CurrentHome;
            if (home == null) return DefaultWorkspaceSubject;
            else
            {
                // return PersudoWorkspaceNames[rnd.Next(0, PersudoWorkspaceNames.Length)];  // 0 <= index < count
                return DefaultWorkspaceSubject + " " + (home.VirtualWorkspaces.Count + 1).ToString();
            }
        }

        // Deserialization constructor
        public VirtualWorkspace(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Load properties
            BackgroundImageClue = (string)info.GetValue("BackgroundImageClue", typeof(string));
            BackgroundMelodyClue = (string)info.GetValue("BackgroundMelodyClue", typeof(string));
            VWCoordinate = (Coordinate?)info.GetValue("VWCoordinate", typeof(Coordinate?));
            Gadgets = (List<VWGadget>)info.GetValue("Gadgets", typeof(List<VWGadget>));
            PageIndex = (int)info.GetValue("PageIndex", typeof(int));
            OpenedVWTrace = (List<VirtualWorkspace>)info.GetValue("OpenedVWTrace", typeof(List<VirtualWorkspace>));
            // Load other data during load time, from a file
            if (Path != null) LoadDocument();
            else Pages = (List<Page>)info.GetValue("Pages", typeof(List<Page>));
        }

        // Serialization function
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save Properties
            info.AddValue("BackgroundImageClue", BackgroundImageClue);
            info.AddValue("BackgroundMelodyClue", BackgroundMelodyClue);
            info.AddValue("VWCoordinate", VWCoordinate);
            info.AddValue("Gadgets", Gadgets);
            info.AddValue("OpenedVWTrace", OpenedVWTrace);
            info.AddValue("PageIndex", PageIndex);
            // Save other data to a file
            if(Path != null) SaveDocument();
            else info.AddValue("Pages", Pages);
        }

        protected override void SaveDocument()
        {
            // Save data into a file
            Stream fileStream = File.Create(Path);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(fileStream, Pages);
            fileStream.Close();
        }

        protected override void LoadDocument()
        {
            // Load from a file
            Stream fileStream = File.OpenRead(Path);
            BinaryFormatter deserializer = new BinaryFormatter();
            Pages = (List<Page>)deserializer.Deserialize(fileStream);
            fileStream.Close();
        }
        #endregion

        #region VW workspace helpers
        internal VirtualWorkspace GetCurrentOpenVW()
        {
            if (OpenedVWTrace.Count != 0) return OpenedVWTrace.Last();
            else return this;
        }
        internal VirtualWorkspace OpenVWDocument(VirtualWorkspace toBeOpened)
        {
            if (toBeOpened.VWCoordinate != null && toBeOpened != this) throw new InvalidOperationException("The VW to be opened isn't a valid target.");

            if(toBeOpened == this)
            {
                OpenedVWTrace.Clear();
                return this;
            }

            if(OpenedVWTrace.Contains(toBeOpened))
            {
                int index = OpenedVWTrace.IndexOf(toBeOpened);
                if(index + 1 < OpenedVWTrace.Count) OpenedVWTrace.RemoveRange(index + 1, OpenedVWTrace.Count - 1 - index);
                return OpenedVWTrace[index];
            }
            else
            {
                OpenedVWTrace.Add(toBeOpened);
                return toBeOpened;
            }
        }

        internal ObservableCollection<VWStackTraceView> GetStackTrace()
        {
            ObservableCollection<VWStackTraceView> newList = new ObservableCollection<VWStackTraceView>();
            // Add self if we have any children
            if (OpenedVWTrace.Count != 0)
            {
                VWStackTraceView view = new VWStackTraceView();
                view.SetVW(this);
                newList.Add(view);
            }
            // Iterate and add children
            foreach (VirtualWorkspace vw in OpenedVWTrace)
            {
                VWStackTraceView view = new VWStackTraceView();
                view.SetVW(vw);
                newList.Add(view);
            }
            return newList;
        }

        internal int GetDocumentCount()
        {
            int count = 0;
            foreach (Page page in Pages)
            {
                count += page.Documents.Count;
            }
            return count;
        }
        #endregion

        #region Location Conversion Helper
        // Row and col begin at index 0, as real grids
        // Convert rectangular coordinate to grid location and a descriptive coordiante using rim and offset
        public void LocationToRowCol(out int row, out int col, out int dimension, out EdgeDirection direction)
        {
            if (VWCoordinate == null) throw new InvalidOperationException("A document VW doesn't have a coordinate.");
            Coordinate coordinate = VWCoordinate.Value;

            // Get required dimensions
            int offset = Math.Max(Math.Abs(coordinate.X), Math.Abs(coordinate.Y));
            int maxDimension = (int) offset * 2 + 1;
            if (maxDimension == 1) { direction = EdgeDirection.Center; row = 1;col = 1; dimension = 3; return; }
            int higherDimension = maxDimension > 3 ? maxDimension : 3;   // Minimum dimension is 3
            dimension = higherDimension;

            // Calculate Locator info
            if(coordinate.X > 0)
            {
                if(coordinate.Y > 0 ) direction = EdgeDirection.BR;
                else if(coordinate.Y == 0) direction = EdgeDirection.RightEdge;
                else direction = EdgeDirection.TR;
            }
            else if (coordinate.X == 0)
            {
                if (coordinate.Y > 0) direction = EdgeDirection.BottomEdge;
                else if (coordinate.Y == 0) direction = EdgeDirection.Center;
                else direction = EdgeDirection.TopEdge;
            }
            else
            {
                if (coordinate.Y > 0) direction = EdgeDirection.BL;
                else if (coordinate.Y == 0) direction = EdgeDirection.LeftEdge;
                else direction = EdgeDirection.TL;
            }

            // Determine Gird Location, and highlight appropriate Locator Compass Icon at direction
            col = coordinate.X + offset;
            row = coordinate.Y + offset;
        }

        public static Coordinate ShiftLeftLocation(Coordinate location)
        {
            return new Coordinate(location.X - 1, location.Y);
        }
        public static Coordinate ShiftRightLocation(Coordinate location)
        {
            return new Coordinate(location.X + 1, location.Y);
        }
        public static Coordinate ShiftUpLocation(Coordinate location)
        {
            return new Coordinate(location.X, location.Y - 1);
        }
        public static Coordinate ShiftDownLocation(Coordinate location)
        {
            return new Coordinate(location.X, location.Y + 1);
        }

        public static Coordinate RowColToLocation(int dimension, int row, int col)
        {
            int offset = (dimension - 1) / 2;
            return new Coordinate(col - offset, row - offset);
        }

        public static VirtualWorkspace Import(System.IO.FileInfo file)
        {
            throw new NotImplementedException();
        }

        public override void Export()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
