using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using MULTITUDE.CustomControl.CanvasSpaces;
using System.Collections.ObjectModel;

namespace MULTITUDE.Class.DocumentTypes
{
    [Serializable]
    public enum NodeType
    {
        SimpleText,
        Link,
        RichFlowText,    // Internally serialized as embeded content
        Jumper  // Graph-wise jumping facility
    }

    [Serializable]
    [Flags]
    public enum GraphNoteFormat
    {
        /// Don't use 0 for comparison needs that for failed comparison
        // Predefined Format
        Heading1 = 1,
        Heading2 = 2,
        Heading3 = 4,
        Heading4 = 8,
        Heading5 = 16,
        // Font Style
        Bold = 32,
        Underline = 64,
        // Color
        Gray = 128,
        White = 256,
        Black = 512,
        LightBlue = 1024,
        AccentRed = 2048,
        GreenBlue = 4096
    }

    /// <summary>
    /// Simple textual graph nodes
    /// </summary>
    [Serializable]
    class GraphNode: ISerializable
    {
        #region Constructors
        public GraphNode(string title, GraphNodeView view)
        {
            Type = NodeType.SimpleText;
            Title = title;
            View = view;
            Content = string.Empty;
            MDDocument = new FlowDocument();
            Ref = null;
        }
        public GraphNode(GraphNode refNode, GraphNodeView view)
        {
            Type = NodeType.Jumper;
            Title = string.Empty;
            View = view;
            Content = string.Empty;
            MDDocument = new FlowDocument();    // Notice the default value for this variable isn't null which isn't very economical
            Ref = refNode;
        }
        public GraphNode(int GUID, string searchString, GraphNodeView view)
        {
            Type = NodeType.Link;
            View = view;
            LinkGUID = GUID;
            OriginalSearchString = searchString;
            MDDocument = new FlowDocument();
            Ref = null;
        }
        public GraphNode(GraphNodeView view, NodeType type = NodeType.RichFlowText)
        {
            Type = type;
            switch (type)
            {
                case NodeType.SimpleText:
                    Title = "Default Node";
                    Content = string.Empty;
                    break;
                case NodeType.Link:
                    throw new InvalidOperationException("Link type graph nodes should call another constructor.");
                case NodeType.RichFlowText:
                    MDDocument = new FlowDocument();
                    break;
                case NodeType.Jumper:
                    throw new InvalidOperationException("Jumper type graph nodes should call another constructor."); 
            }
            MDDocument = new FlowDocument();
            Ref = null;
            View = view;
        }
        [NonSerialized]
        public GraphNodeView View = null;  // Sessional Usage, not serialized
        #endregion

        #region Datas
        // Shared
        public Point Location { get; set; }
        public NodeType Type { get; set; }

        // Type A
        public string Title { get; set; }
        public string Content { get; set; }
        public GraphNoteFormat Formats { get; set; }

        // Type B
        public int LinkGUID { get; set; }
        public string OriginalSearchString { get; set; }

        // Type C
        public FlowDocument _MDDocument;
        public FlowDocument MDDocument {
            get
            {
                if (_MDDocument == null) throw new InvalidOperationException("_MDDocument is null.");
                _MDDocument = MarkdownPlusHelper.CloneDocument(_MDDocument);
                return _MDDocument;
            }
            set
            {
                if(_MDDocument != value ) _MDDocument = value;
            }
        }

        // Type D
        public GraphNode Ref { get; set; }
        #endregion

        #region Interface
        // Delta should be location netrual with scale = 1
        public void Translate(double dx, double dy)
        {
            Location = new Point(Location.X + dx, Location.Y + dy);
        }
        #endregion

        #region Serialization
        public GraphNode(SerializationInfo info, StreamingContext ctxt)
        {
            // Load Properties
            Location = (Point)info.GetValue("Location", typeof(Point));
            Type = (NodeType)info.GetValue("Type", typeof(NodeType));
            Title = (string)info.GetValue("Title", typeof(string));
            Content = (string)info.GetValue("Content", typeof(string));
            Formats = (GraphNoteFormat)info.GetValue("Formats", typeof(GraphNoteFormat));
            LinkGUID = (int)info.GetValue("LinkGUID", typeof(int));
            OriginalSearchString = (string)info.GetValue("OriginalSearchString", typeof(string));
            Ref = (GraphNode)info.GetValue("Ref", typeof(GraphNode));
            // Load Document
            MDDocument = new FlowDocument();
            byte[] data = (byte[])info.GetValue("MDDocument", typeof(byte[]));
            using (MemoryStream stream = new MemoryStream(data))
            {
                TextRange tRange = new TextRange(_MDDocument.ContentStart, _MDDocument.ContentEnd);
                tRange.Load(stream, DataFormats.Xaml);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Save Properties
            info.AddValue("Location", Location);
            info.AddValue("Type", Type);
            info.AddValue("Title", Title);
            info.AddValue("Content", Content);
            info.AddValue("Formats", Formats);
            info.AddValue("LinkGUID", LinkGUID);
            info.AddValue("OriginalSearchString", OriginalSearchString);
            info.AddValue("Ref", Ref);
            // Save Document
            using (MemoryStream stream = new MemoryStream())
            {
                TextRange tRange = new TextRange(_MDDocument.ContentStart, _MDDocument.ContentEnd);
                tRange.Save(stream, DataFormats.Xaml, false);
                info.AddValue("MDDocument", stream.ToArray());
            }
        }
        #endregion
    }

    [Serializable]
    class GraphConnection
    {
        public GraphConnection(GraphNode nodeA, GraphNode nodeB)
        {
            NodeA = nodeA;
            NodeB = nodeB;
        }

        // Data
        public GraphNode NodeA { get; }
        public GraphNode NodeB { get; }
    }

    // A layer of indirection so we can serialize this structure as a whole
    [Serializable]
    class GraphData
    {
        public GraphData()
        {
            // Initialization of some data structures
            Nodes = new List<GraphNode>();
            Connections = new List<GraphConnection>();
            Bookmarks = new List<GraphNode>();
        }

        public GraphData(List<GraphNodeView> nodes, List<GraphNodeConnection> connections, List<GraphNodeView> Bookmarks)
        {
            this.Nodes = nodes.Select(item => item.Node).ToList();
            this.Connections = connections.Select(item => new GraphConnection(item.Start.Node, item.End.Node)).ToList();
            this.Bookmarks = Bookmarks.Select(item => item.Node).ToList();
        }

        public List<GraphNode> Bookmarks { get; set; }
        public List<GraphNode> Nodes { get; set; }
        public List<GraphConnection> Connections { get; set; }
    }

    // CEA format: NODEID
    [Serializable]
    class Graph : Document, ISerializable
    {
        #region Basic Constructors
        public Graph(string path, string metaname) : this(DocumentType.Graph, path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")) { }
        // A wrapper for underlying base 
        public Graph(DocumentType type, string path, string metaname, string date)
            : base(type, path, metaname, date != null ? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {
            _Data = new GraphData();
        }
        #endregion

        #region Static Configurations
        public static readonly string FileSuffix = ".MULTITUDEgh";
        public static readonly string DefaultName = "Unnamed Graph";
        #endregion

        // Data
        private bool bDataLoaded = false;
        private GraphData _Data;
        public GraphData Data
        {
            get
            {
                if (bDataLoaded == false) LoadDocument();
                return _Data;
            }
            set
            {
                if(_Data != value) _Data = value;
            }
        }

        internal void SaveData(List<GraphNodeView> nodes, List<GraphNodeConnection> connections, ObservableCollection<GraphNodeView> bookmarks)
        {
            _Data = new GraphData(nodes, connections, bookmarks.ToList());
            bDirty = true;
            SaveDocument();
        }

        #region Serialization
        public Graph(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Load properties
            // Nothing to load

            // Load Data
            // LoadDocument();  // Don't do this until first use
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save Properties
            // Nothing to save

            // Save Data
            SaveDocument();
        }

        protected override void SaveDocument()
        {
            if(bDirty)
            {
                // Save data into a file
                Stream fileStream = File.Create(Path);
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(fileStream, _Data);
                fileStream.Close();

                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            // Load from a file
            Stream fileStream = File.OpenRead(Path);
            BinaryFormatter deserializer = new BinaryFormatter();
            _Data = (GraphData)deserializer.Deserialize(fileStream);
            fileStream.Close();

            bDataLoaded = true;
        }

        public static Graph Import(System.IO.FileInfo file)
        {
            if (file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for command.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mgh suffix
            throw new NotImplementedException();
        }
        #endregion Serialization
    }
}
