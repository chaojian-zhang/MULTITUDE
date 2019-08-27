using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace MULTITUDE.Class.DocumentTypes
{
    [Serializable]
    [Flags]
    enum ElementStyle   // Combinable; Not all combinations usable, dependding on bit settings, e.g. if a Heading then FontStyle ignored
    {
        /// Don't use 0 for comparison needs that for failed comparison
        // Predefined Format
        Title = 1, 
        Heading = 2,
        Table = 4,
        Bullet = 8,
        Hyperlink = 16, 
        // Font Style
        Bold = 32,
        Italic = 64,
        Underline = 128,
        Strikethrough = 256,
        // Color
        Colored = 512
    }
    [Serializable]
    enum ElementType
    {
        Text,
        Hyperlink
    }
    [Serializable]
    class MDParagraphElement
    {
        public ElementStyle Style { get; set; }
        public string Content { get; set; }
    }
    [Serializable]
    class MDParagraph
    {
        public List<MDParagraphElement> Elements { get; set; }
    }

    class MarkdownPlusHelper
    {
        // Make a duplicate of current document to avoid parenting issues, use with care otherwise might cause performance issues
        // Ref: https://social.msdn.microsoft.com/Forums/vstudio/en-US/f4b26d9b-5b74-446b-85e7-e49e519380ad/copying-one-flowdocument-to-second-flowdocument?forum=wpf
        // Ref: https://stackoverflow.com/questions/729629/sharing-flowdocuments-between-multiple-richtextboxes
        public static FlowDocument CloneDocument(FlowDocument origin)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                TextRange range = new TextRange(origin.ContentStart, origin.ContentEnd);
                System.Windows.Markup.XamlWriter.Save(range, stream);   // Xaml Header
                range.Save(stream, DataFormats.XamlPackage);

                FlowDocument clone = new FlowDocument();
                TextRange cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
                cloneRange.Load(stream, DataFormats.XamlPackage);
                return clone;
            }
        }
    }

    // (For a long string inside a paragraph the string itself is enough to identify roughly what we want, but we added a keyword field to be a little more accurate - or we might use character count of that paragrah element but that's way to unreadable)
    // CEA Format 1 - Link out or clue accurate: ParagraphID:ContentID:CharacterLocation:CharacterCount
    // CEA Format 2 - Link in ambiguous: Paragraph Leading Keyword:Content Keyword:Other keywords -- And we generate a best match
    // CEA Format 3 - Link in ambiguous: Keywords seperated using single space, order matters -- And we generate a best match of a single sentence, delimilated by normal punctuations
    [Serializable]
    class MarkdownPlus : Document, ISerializable
    {
        public MarkdownPlus(string path, string metaname)
            :this(DocumentType.MarkdownPlus, path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")){ }
        // A wrapper for underlying base
        public MarkdownPlus(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        { _FlowDocument = new FlowDocument(); DistributedClonesForCurrentFlowDocument = new List<FlowDocument>(); }

        // Book keeper
        [NonSerialized]
        private List<FlowDocument> DistributedClonesForCurrentFlowDocument;

        // Raw Data
        private FlowDocument _FlowDocument;
        public FlowDocument GetFlowDocument()
        {
            if (_FlowDocument == null) LoadDocument();
            FlowDocument returnDocument = MarkdownPlusHelper.CloneDocument(_FlowDocument);
            DistributedClonesForCurrentFlowDocument.Add(returnDocument);
            return returnDocument;
        }
        public void SetFlowDocument(FlowDocument doc)
        {
            _FlowDocument = MarkdownPlusHelper.CloneDocument(doc);
            DistributedClonesForCurrentFlowDocument.Clear();
            bDirty = true;
        }
        internal bool FlowDocumentHasChangedSince(FlowDocument document)
        {
            return DistributedClonesForCurrentFlowDocument.Contains(document) == false;
        }
        // Transcripted Data
        public List<MDParagraph> Paragraphs { get; set; }
        public List<MDNote> Notes { get; set; }
        public List<MDImage> Images { get; set; }
        // ... // Cache, not all documents have a cache
        public static readonly string FileSuffix = ".MULTITUDEmd+";

        // Interface
        public string Text
        {
            get
            {
                FlowDocument docuemntClone = GetFlowDocument();
                TextRange textRange = new TextRange(docuemntClone.ContentStart, docuemntClone.ContentEnd);
                return textRange.Text;
            }
        }
        protected override string GetDetails()
        {
            return this.Text;
        }

        #region Serialization
        public MarkdownPlus(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Session-only variables
            DistributedClonesForCurrentFlowDocument = new List<FlowDocument>();

            //// Load properties
            //Paragraphs = (List<MDParagraph>)info.GetValue("Paragraphs", typeof(List<MDParagraph>));
            //Notes = (List<MDNote>)info.GetValue("Notes", typeof(List<MDNote>));
            //Images = (List<MDImage>)info.GetValue("Images", typeof(List<MDImage>));

            // LoadDocument();  // At this time home location cannot be fetched because Home hasn't been serialized yet; Ideally the content is loaded during first preview (handled above)
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            //// Save Properties
            //info.AddValue("Paragraphs", Paragraphs);
            //info.AddValue("Notes", Notes);
            //info.AddValue("Images", Images);
            SaveDocument();
        }

        /// <summary>
        /// Regarding serialization of FlowDocument:
        /// https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/document-serialization-and-storage
        /// http://arnssn.blogspot.ca/2013/01/howto-save-and-read-flowdocument.html -- People say FlowDocumetn can be serailized to binary but I don't see an easy way
        /// https://stackoverflow.com/questions/6978067/wpf-can-binaryformatter-serialize-flowdocument-instance
        /// https://stackoverflow.com/questions/2447856/saving-flowdocument-to-sql-server
        /// </summary>

        //// Binary serialization raises exeception
        //protected override void SaveDocument()
        //{
        //    Stream fileStream = File.Open(Path, FileMode.OpenOrCreate);
        //    BinaryFormatter serializer = new BinaryFormatter();
        //    serializer.Serialize(fileStream, FlowDocument);
        //    fileStream.Close();
        //}

        //protected override void LoadDocument()
        //{
        //    // Load existing home
        //    string path = Path;
        //    if (System.IO.File.Exists(path))
        //    {
        //        // Load serialized data
        //        Stream fileStream = File.OpenRead(path);
        //        BinaryFormatter deserializer = new BinaryFormatter();
        //        FlowDocument = (FlowDocument)deserializer.Deserialize(fileStream);
        //        fileStream.Close();
        //    }
        //}

        //// Text serialization
        //protected override void SaveDocument()
        //{
        //    MemoryStream ms = new MemoryStream();
        //    TextRange tRange = new TextRange(FlowDocument.ContentStart, FlowDocument.ContentEnd);
        //    tRange.Save(ms, DataFormats.Rtf);
        //    ms.Position = 0;
        //    string base64 = Convert.ToBase64String(ms.ToArray());
        //    System.IO.File.WriteAllText(Path, base64);
        //    ms.Close();
        //}

        //protected override void LoadDocument()
        //{
        //    // Load existing home
        //    string path = Path;
        //    if (System.IO.File.Exists(path))
        //    {
        //        byte[] content = Convert.FromBase64String(System.IO.File.ReadAllText(Path));

        //        MemoryStream ms = new MemoryStream(content);
        //        ms.Position = 0;

        //        FlowDocument = new FlowDocument();
        //        TextRange textRange = new TextRange(FlowDocument.ContentStart, FlowDocument.ContentEnd);
        //        textRange.Load(ms, DataFormats.Rtf);
        //        ms.Close();
        //    }
        //}

        // Binary serialization
        protected override void SaveDocument()
        {
            if(bDirty)
            {
                using (FileStream file = new FileStream(Path, FileMode.Create, System.IO.FileAccess.Write))
                {
                    TextRange tRange = new TextRange(_FlowDocument.ContentStart, _FlowDocument.ContentEnd);
                    tRange.Save(file, DataFormats.Xaml, false);    // Notice DataFormats.Rtf doesn't preserve paragraph formats
                }

                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            string path = Path;
            if (System.IO.File.Exists(path))
            {
                using (FileStream file = new FileStream(Path, FileMode.Open, FileAccess.Read))
                {
                    _FlowDocument = new FlowDocument();
                    TextRange textRange = new TextRange(_FlowDocument.ContentStart, _FlowDocument.ContentEnd);
                    textRange.Load(file, DataFormats.Xaml);
                }
            }           
        }

        public static MarkdownPlus Import(System.IO.FileInfo target)
        {
            if (target.Extension == FileSuffix)
            {
                MarkdownPlus mdp = new MarkdownPlus(target.FullName, target.Name);  // Notice we have no way to fetch its document name and creation data since that is stored with relavent Home which might not even be present at import time
                using (FileStream file = new FileStream(target.FullName, FileMode.Open, FileAccess.Read))
                {
                    TextRange textRange = new TextRange(mdp._FlowDocument.ContentStart, mdp._FlowDocument.ContentEnd);
                    textRange.Load(file, DataFormats.Xaml);
                }
                return mdp;
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for markdown plus.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mdb suffix
            throw new NotImplementedException();
        }
        #endregion Serialization
    }

    [Serializable]
    class MDFloat
    {
        public int RelativeParagraph { get; set; }  // Locating of MD+ floating elements is relative to the beginning of a paragraph, then automatically adjust relative to other floating elements
    }
    [Serializable]
    class MDNote : MDFloat
    {
        public string Content { get; set; }
    }
    [Serializable]
    class MDImage : MDFloat
    {
        public Document Image { get; set; }
    }
}
