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
    public class VWStackTraceView: INotifyPropertyChanged
    {
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
        }

        // ID information
        public int DocumentID { get; set; }
        // Placement information
        public IconArea Occupation { get; set; }
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
    public class Coordiante
    {
        public int X { get; set; }
        public int Y { get; set; }
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
        public Coordiante Location { get; set; }   // VW Relative Location: Location of this VW in counter clockwise spiral coordinate; Begin at 1; -1 indicates a packed VW document that isn't managed under active VW spaces
        public int PageIndex { get; set; }   // Index begin from 0
        public List<VWGadget> Gadgets { get; set; } // VW Gadgets
        // public ToolTray ToolTray { get; set; }  // VW Tooltray
        public List<Page> Pages { get; set; } // VW Pages
        public List<VirtualWorkspace> OpenedVWTrace { get; set; }  // Stack of VWs that are opened under current VW
        #endregion
        public static string FileSuffix = ".MULTITUDEvw";

        #region Constructor and Serialization Handling
        public const string DefaultWorkspaceSubject = "My Workspace";   // https://stackoverflow.com/questions/55984/what-is-the-difference-between-const-and-readonly
        public VirtualWorkspace(Coordiante location = null, string name = DefaultWorkspaceSubject, string path = null, string creationdate = null)
            :this(DocumentType.VirtualWorkspace, path, 
                 (name == DefaultWorkspaceSubject? VirtualWorkspace.GetRandomWorkspaceName() : name), 
                 creationdate == null ? SystemHelper.CurrentTimeFileNameFriendly : creationdate, location){}
        // A wrapper for underlying base
        public VirtualWorkspace(DocumentType type, string path, string metaname, string date, Coordiante location = null)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {             
            Location = location;
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
            Location = (int)info.GetValue("Location", typeof(int));
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
            info.AddValue("Location", Location);
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
            if (toBeOpened.Location != -1 && toBeOpened != this) throw new InvalidOperationException("The VW to be opened isn't a valid target.");

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
        #endregion

        #region Location Conversion Helper
        // Row and col begin at index 0, as real grids

        // Convert spiral coordinate to grid location and a descriptive coordiante using rim and offset
        public void LocationToRowCol(out int row, out int col, out int dimension, out EdgeDirection direction, out int cordRim, out int cordOffset)
        {
            if (Location == -1) throw new InvalidOperationException("A document VW doesn't have a spiral coordinate.");

            // Get required dimensions: e.g. Location = 17; 13; 6; 46; 9
            int lowerDimension = (int)Math.Sqrt(Location);
            lowerDimension = (lowerDimension % 2 == 0) ? lowerDimension - 1 : lowerDimension;    // e.g. lowerDimension = 3; 3; 1; 5; 3
            int higherDimension = (int)Math.Ceiling(Math.Sqrt(Location));
            higherDimension = (higherDimension % 2 == 0) ? higherDimension + 1 : higherDimension;    // e.g. higherDimension = 5; 5; 3; 7; 3
            if (higherDimension == 1) { direction = EdgeDirection.Center; row = 1;col = 1; dimension = 3; cordRim = 3; cordOffset = 1; return; }
            higherDimension = higherDimension > 3 ? higherDimension : 3;   // Minimum dimension is 3
            dimension = higherDimension;

            // Calculate Coordinate
            cordRim = higherDimension;  // e.g. rim = 5; 5; 3; 7; 3
            cordOffset = Location - lowerDimension * lowerDimension;   // e.g. cordOffset = 8; 4; 5; 21; 0
            // Calculate Locator info
            if (cordOffset == 0) direction = EdgeDirection.BR;      // e.g. cordOffset = 8; 4; 5; 21; 0
            else if (cordOffset <= higherDimension - 2) direction = EdgeDirection.RightEdge;
            else if (cordOffset == higherDimension - 1) direction = EdgeDirection.TR;
            else if (cordOffset <= 2 * higherDimension - 3) direction = EdgeDirection.TopEdge;
            else if (cordOffset == 2 * higherDimension - 2) direction = EdgeDirection.TL;
            else if (cordOffset <= 3 * higherDimension - 4) direction = EdgeDirection.LeftEdge;
            else if (cordOffset == 3 * higherDimension - 3) direction = EdgeDirection.BL;
            else direction = EdgeDirection.BottomEdge;  // e.g. direction = ; ; ; ; ; BR

            // Determine Gird Location, and highlight appropriate Locator Compass Icon at direction
            int destRow = (higherDimension - 1) / 2;    // Default center, e.g. 2; 2; 1; 3; 1
            int destCol = (higherDimension - 1) / 2;    // Default center, e.g. 2; 2; 1; 3; 1
            switch (direction)
            {
                case EdgeDirection.LeftEdge:
                    destRow = 3 * higherDimension - 3 - cordOffset;
                    destCol = 0;
                    break;
                case EdgeDirection.RightEdge:
                    destRow = higherDimension - 1 - cordOffset;
                    destCol = higherDimension - 1;
                    break;
                case EdgeDirection.TopEdge:
                    destRow = 0;
                    destCol = 2 * higherDimension - 2 - cordOffset;
                    break;
                case EdgeDirection.BottomEdge:
                    destRow = higherDimension - 1;
                    destCol = 4 * higherDimension - 4 - cordOffset;
                    break;
                case EdgeDirection.Center:
                    destRow = 1;
                    destCol = 1;
                    break;
                case EdgeDirection.TL:
                    destRow = 0;
                    destCol = 0;
                    break;
                case EdgeDirection.TR:
                    destRow = 0;
                    destCol = higherDimension - 1;
                    break;
                case EdgeDirection.BL:
                    destRow = higherDimension - 1;
                    destCol = 0;
                    break;
                case EdgeDirection.BR:
                    destRow = higherDimension - 1;
                    destCol = higherDimension - 1;
                    break;
            }
            row = destRow;
            col = destCol;
        }

        public static int ShiftLeftLocation(int location)
        {
            return location;
        }
        public static int ShiftRightLocation(int location)
        {
            return location;
        }
        public static int ShiftUpLocation(int location)
        {
            return location;
        }
        public static int ShiftDownLocation(int location)
        {
            return location;
        }

        // Well there seem is the solution: https://math.stackexchange.com/questions/888361/given-coordinates-find-the-number-at-that-coordinates-in-spiral-matrix
        // https://stackoverflow.com/questions/3706219/algorithm-for-iterating-over-an-outward-spiral-on-a-discrete-2d-grid-from-the-or
        // Notice one quickest solution for this can be hard-code a 2D matrix
        // https://stackoverflow.com/questions/10094745/find-the-position-nth-element-of-a-rectangular-tiled-spiral
        // http://mathforum.org/library/drmath/view/62201.html
        // Convert from a given row and col to spiral coordinate; Assume dimension is odd
        public static int RowColToLocation(int dimension, int row, int col)
        {
            // Seriously I don't have a better solution
            int destRow = row - (dimension - 1) /2;
            int destCol = col - (dimension - 1) /2;
            int curRow = 0, curCol = 0;
            int counter = 1;
            int currentDimension = 1;
            // Initiation
            if(curRow == destRow && curCol == destCol) return counter;
            else
            {
                // Walk right a step
                curCol++;
                counter++;
                currentDimension += 2;
            }
            // Mechanical Method
            while(curRow != destRow || curCol != destCol)
            {
                // March up first
                int upSteps = 0;
                while(upSteps != currentDimension - 2)
                {
                    upSteps++;
                    counter++;
                    curRow--;
                    if (curRow == destRow && curCol == destCol) return counter;
                }
                // Then march left
                int leftSteps = 0;
                while (leftSteps != currentDimension - 1)
                {
                    leftSteps++;
                    counter++;
                    curCol--;
                    if (curRow == destRow && curCol == destCol) return counter;
                }
                // Then march down
                int downSteps = 0;
                while (downSteps != currentDimension - 1)
                {
                    downSteps++;
                    counter++;
                    curRow++;
                    if (curRow == destRow && curCol == destCol) return counter;
                }
                // Then march right
                int rightSteps = 0;
                while (rightSteps != currentDimension - 1)
                {
                    rightSteps++;
                    counter++;
                    curCol++;
                    if (curRow == destRow && curCol == destCol) return counter;
                }
                if (curRow == destRow && curCol == destCol) return counter;
                else
                {
                    // Walk right a step
                    curCol++;
                    counter++;
                    currentDimension += 2;
                }
            }
            return counter;
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
