using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MULTITUDE.Class.DocumentTypes;

namespace MULTITUDE.Class
{
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
    class VWGadget
    {
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
        CanvasRelativeLocation Location { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    // Represents the documents that are placed on this VW
    [Serializable]
    class DocumentIcon
    {
        // ID information
        public int DocumentID { get; set; }
        // Placement information
        public IconArea Occupation { get; set; }
    }

    [Serializable]
    class Page
    {
        public List<DocumentIcon> Documents;
    }
    #endregion

    #region Tool Tray Related
    [Serializable]
    class ToolTray
    {
        public List<Document> ToolTrayItems { get; set; }
    }
    #endregion

    /// <summary>
    /// This is a pure data class
    /// </summary>
    [Serializable]
    class VirtualWorkspace: Document, ISerializable
    {
        #region User VW Configurations
        public string BackgroundImageClue { get; set; } // VW Background Image
        public string BackgroundMelodyClue { get; set; }    // VW Background Melody
        #endregion
        #region Content and Layout Data
        public int Location { get; set; }   // VW Relative Location: Location of this VW in counter clockwise spiral coordinate
        public List<VWGadget> Gadgets { get; set; } // VW Gadgets
        public ToolTray ToolTray { get; set; }  // VW Tooltray
        public List<Page> Pages { get; set; } // VW Pages
        #endregion

        #region Constructor
        public VirtualWorkspace(int location = 0)
        {
            Location = location;
            Gadgets = new List<VWGadget>();
            Pages = new List<Page>();
            ToolTray = new ToolTray();
        }
        #endregion
    }
}
