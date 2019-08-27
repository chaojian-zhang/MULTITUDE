using MULTITUDE.CustomControl.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MULTITUDE.Class.DocumentTypes
{
    /// <summary>
    /// The type of data that is being stored in current DataCollection
    /// </summary>
    [Serializable]
    enum DataType
    {
        Value,  // Single key, single value; A restricted version of dictionary
        Dictionary, // Single key, multiple value; A restricted version of table
        Table   // Multiple entries without a key
    }

    /// <summary>
    /// A lightweight linear table with expandable headers (columns)
    /// It is capable of emulating: StringValuePair and Dictionary<string, string> and also itself: Table
    /// </summary>
    [Serializable]
    class Table
    {
        public Table(DataType type, int headerCount = 0, int rowCount = 0)
        {
            Headers = new List<string>();
            Rows = new List<List<string>>();
            Type = type;

            // Defaults
            for (int i = 0; i < headerCount; i++)
            {
                Headers.Add("Header");
            }
            for (int i = 0; i < rowCount; i++)
            {
                List<string> row = new List<string>();
                for (int j = 0; j < headerCount; j++)
                {
                    row.Add("Content");
                }
                Rows.Add(row);
            }
        }

        #region Table Data
        // Defines possible columns for the table, not all columns need to be used for each row
        public List<string> Headers { get; set; }   // A column can be any type: int or string or anything else but to make things easier we just use strong formated string, with a bit waste of storage space for numerical types which anyway isn't our main intention for Tables in our application; Distinguish lower and upper case
        public List<List<string>> Rows { get; set; }    // A row is implemented as a list of a list of values that fills the columns in order; balancing is kept during editing; If a cell is empty then make it empty; This is the most economical way of storying table data
        public DataType Type { get; set; }
        #endregion

        // Interfaces
        /// <summary>
        /// Add a new header to current headers, if the header already exists then nothing happens
        /// </summary>
        /// <param name="name">name of header</param>
        public void AddHeader(string name, string fillValue = "")
        {
            if (Headers.Contains(name) == false)
            {
                Headers.Add(name);
                foreach (List<string> row in Rows)
                {
                    row.Add(fillValue);
                }
            }
        }
        /// <summary>
        /// Remove a header, also remove rows' contents that no longer correspond to any header; Raise exception if header doesn't exist
        /// </summary>
        /// <param name="name">name of header</param>
        public void DeleteHeader(string name)
        {
            int index = Headers.IndexOf(name);
            if (index == -1) throw new IndexOutOfRangeException("No header matches the name");
            else
            {
                Headers.RemoveAt(index);
                foreach (List<string> row in Rows)
                {
                    row.RemoveAt(index);
                }
            }
        }
        /// <summary>
        /// Change name of a header; Do nothing if header doesn't exist.
        /// </summary>
        public void ChangeHeader(string oldName, string newName)
        {
            int index = Headers.IndexOf(oldName);
            if (index == -1) return;
            else
            {
                Headers[index] = newName;
            }
        }
        /// <summary>
        /// Add contents as a new row, consume values as long as it is provided, and as much as it doesn't exceed limit of current headers (otherwise filled with empty) with an exception ArgumentOutOfRange
        /// </summary>
        public void AddNewRow(params string[] values)
        {
            if (values.Length > Headers.Count) throw new ArgumentOutOfRangeException("More cell values than existing headers.");
            List<string> newRow = new List<string>();
            for (int i = 0; i < Headers.Count; i++)
            {
                if (i < values.Length) newRow.Add(values[i]);
                else newRow.Add(string.Empty);
            }
            Rows.Add(newRow);
        }
        /// <summary>
        /// Alias to AddHeader for semantic purpose
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fillValue"></param>
        public void AddNewColumn(string name, string fillValue = "")
        {
            AddHeader(name, fillValue);
        }
        /// <summary>
        /// Replace contents as a new row, consume values as long as it is provided, and as much as it doesn't exceed limit of current headers (otherwise discarded) with an exception ArgumentOutOfRange
        /// </summary>
        public void ReplaceRow(int index, params string[] values)
        {
            throw new NotImplementedException();
            // Why is this method ever useful and needed?
        }
        /// <summary>
        /// Replace the value at header at index, if not exist then create it
        /// </summary>
        public void ChangeRow(int index, string header, string value)
        {
            throw new NotImplementedException();
            // Why is this method ever useful and needed?
        }
    }

    /// <summary>
    /// Implements storage for certain application documents
    /// (Implemented using SQL and LINQ might be too overwhelming, a custom type is more suitable, with JSON maybe) This type of data needs to be able to dynamically expand type (e.g. for table items), and small in size (for there might be more types than existing values), and easily modifiable (through Excel interface?). 
    /// </summary>
    [Serializable]
    class DataCollection : Document, ISerializable
    {
        public DataCollection(string path, string name)
            :this(DocumentType.DataCollection, path, name, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")) {}

        // A wrapper for underlying base
        public DataCollection(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly){ Data = new Table(DataType.Dictionary, 2, 2); DistributedClonesForCurrentFlowDocument = new List<FlowDocument>(); }

        public Table Data { get; set; }
        public static readonly string FileSuffix = ".MULTITUDEdc";
        // Might also want to support simple Comma delimited value files as export and imports
        // ....

        // Interface functions
        public DataCollection InstantiateTableFromTable(Table referenceTable) // Generate a new table from existing table, with its headers but not contents
        {
            throw new NotImplementedException();
        }

        #region MD+ Compatible Editing
        // Book keeper
        [NonSerialized]
        private List<FlowDocument> DistributedClonesForCurrentFlowDocument;


        // One desirable unit is one unit of space wanted
        internal double GetDesirableUnits()
        {
            return Data.Headers.Count() > 0 ? Data.Headers.Count() : 2;
        }

        internal FlowDocument GenerateFlowDocument()
        {
            // Load if not already
            if (Data == null) LoadDocument();

            FlowDocument flowDocument = new FlowDocument();
            // Insert a default table
            flowDocument.Blocks.Add(MarkdownPlusEditor.CreateTable(this.Data));   // We have only one block and it's a table
            // Add to reference
            DistributedClonesForCurrentFlowDocument.Add(flowDocument);
            return flowDocument;
        }

        internal void RefreshTableData(FlowDocument document)
        {
            if (document.Blocks.First() is System.Windows.Documents.Table == false) throw new InvalidOperationException("FlowDocument doesn't representat a table of data.");

            // Generate a new table from document - also deduce its mode
            Table newTable = new Table(DataType.Table);   // <Pending> Deduce its mode
            System.Windows.Documents.Table flowTable = document.Blocks.First() as System.Windows.Documents.Table;
            // Our flow document table will have a header row and then content rows
            // Header row
            TableRow headerRow = flowTable.RowGroups[0].Rows[0];
            foreach (TableCell cell in headerRow.Cells)
            {
                TextRange textRange = new TextRange(cell.ContentStart, cell.ContentEnd);
                newTable.Headers.Add(textRange.Text);
            }
            // Content rows
            for (int i = 1; i < flowTable.RowGroups[0].Rows.Count; i++)
            {
                TableRow contentRow = flowTable.RowGroups[0].Rows[i];
                string[] values = contentRow.Cells.Select(item => new TextRange(item.ContentStart, item.ContentEnd).Text).ToArray();
                newTable.AddNewRow(values);
            }
            // Deduce its mode
            bool bContainsRepeat = newTable.Rows.Count - newTable.Rows.Select(row => row[0]).Distinct().Count() > 0;
            if(bContainsRepeat) newTable.Type = DataType.Table;
            else
            {
                if (newTable.Headers.Count > 2) newTable.Type = DataType.Value;
                else newTable.Type = DataType.Dictionary;
            }

            // Assign
            Data = newTable;
            bDirty = true;
            SaveDocument();

            // Update bookkeeping
            DistributedClonesForCurrentFlowDocument.Clear();
        }

        internal bool TableDataHasChangedSince(FlowDocument document)
        {
            return DistributedClonesForCurrentFlowDocument.Contains(document) == false;
        }
        #endregion

        #region Serialization
        public DataCollection(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Session-only variables
            DistributedClonesForCurrentFlowDocument = new List<FlowDocument>();

            // Properties and Data
            Data = null;
            // LoadDocument(); // Ideally  content is loaded during first preview(handled above)
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            SaveDocument();
        }

        protected override void SaveDocument()
        {
            if (bDirty)
            {
                // Save Table Data
                Stream fileStream = File.Create(Path);
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(fileStream, Data);
                fileStream.Close();

                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            string path = Path;
            if (System.IO.File.Exists(path))
            {
                // Load Table data
                Stream fileStream = File.OpenRead(Path);
                BinaryFormatter deserializer = new BinaryFormatter();
                Data = (Table)deserializer.Deserialize(fileStream);
                fileStream.Close();
            }
        }

        public static DataCollection Import(System.IO.FileInfo file)
        {
            if(file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable suffix.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mdc suffix
            throw new NotImplementedException();
        }
        #endregion Serialization
    }
}
