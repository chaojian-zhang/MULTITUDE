using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    // <Debug> Cautious localization problem, i.e. ANSi vs Unicode problem
    // Documents cached in real time in memory
    [Serializable]
    class PlainText : Document, ISerializable
    {
        public static readonly string FileSuffix = ".txt";
        public static readonly string Extensions = "cpp.h.md";  // For supported imports
        public PlainText(string path, string metaname)
            :this(path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")) { }

        // A wrapper for underlying base
        public PlainText(string path, string metaname, string date)
            :base(DocumentType.PlainText, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly){ _Content = null;/* Don't given content a defualt value because it's not materialized yet and should be bound with actual file-behind*/}

        // Data
        public string _Content;
        public string Content
        {
            get
            {
                if (_Content == null) { LoadDocument(); return _Content; }
                else return _Content;
            }
            set {
                _Content = value;
                bDirty = true;
            }
        }
        public int RecentIndex { get; set; }// Records the text index of last editing so during loading we can restore previous state; Automatically scroll to index using CaretIndex, ScrollToEnd, ScrollToLine

        protected override string GetDetails()
        {
            return this.Content;
        }

        #region Serialization
        public PlainText(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // LoadDocument(); // At this time home location cannot be fetched because Home hasn't been serialized yet; Ideally the content is loaded during first preview (handled above)
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            SaveDocument();
        }

        protected override void SaveDocument()
        {
            // Save data into a text file
            if(bDirty)
            {
                System.IO.File.WriteAllText(Path, Content);
                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            _Content = System.IO.File.ReadAllText(Path);
        }

        public static PlainText Import(System.IO.FileInfo file)
        {
            if (file.Extension == FileSuffix)
            {
                PlainText newDoc = new PlainText(file.FullName, System.IO.Path.GetFileNameWithoutExtension(file.Name), file.LastWriteTime.ToString("MMMM dd, yyyy HHmmss"));
                // newDoc.LoadDocument();   // Don't load, causes import slow down and is not necessary since Deep Search not enabled now
                return newDoc;
            }
            else if (MULTITUDE.Class.Facility.StringHelper.ExtensionContains(Extensions, file.Extension))
            {
                // Need to do some extra content conversion or maybe sometime later
                PlainText newDoc = new PlainText(file.FullName, System.IO.Path.GetFileNameWithoutExtension(file.Name), file.LastWriteTime.ToString("MMMM dd, yyyy HHmmss"));
                // newDoc.LoadDocument();
                return newDoc;
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for plain texts.");
            }
        }

        public override void Export()
        {
            throw new NotImplementedException();
        }
        #endregion Serialization
    }
}
