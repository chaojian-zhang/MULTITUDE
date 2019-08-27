using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    /// <summary>
    /// The type of data that is being stored in current DataCollection
    /// </summary>
    enum DataType
    {
        Value,
        Dictionary,
        Table
    }

    /// <summary>
    /// A lightweight linear table with expandable headers (columns)
    /// </summary>
    class Table
    {
        // Defines possible columns for the table, not all columns need to be used for each row
        public List<string> Headers { get; set; }   // A column can be any type: int or string or anything else but to make things easier we just use strong formated string, with a bit waste of storage space for numerical types which anyway isn't our main intention for Tables in our application
        public List<KeyValuePair<int, string>> Rows;    // A row is implemented as a list of key value pairs the selectively fills the columns; Thus a complete row will take a const amount of auxliary description information

        // Interfaces
        /// <summary>
        /// Add a new header to current headers, if the header already exists then nothing happens
        /// </summary>
        /// <param name="name">name of header</param>
        public void AddHeader(string name)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Remove a header, also remove rows' contents that no longer correspond to any header
        /// </summary>
        /// <param name="name">name of header</param>
        public void DeleteHeader(string name)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Change name of a header
        /// </summary>
        public void ChangeHeader(string oldName, string newName)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Add contents as a new row, consume values as long as it is provided, and as much as it doesn't exceed limit of current headers (otherwise discarded) with an exception ArgumentOutOfRange
        /// </summary>
        public void AddNewRow(string header, params string[] values)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Add one new column to a row at index
        /// </summary>
        public void AddToRow(int index, string header)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Replace contents as a new row, consume values as long as it is provided, and as much as it doesn't exceed limit of current headers (otherwise discarded) with an exception ArgumentOutOfRange
        /// </summary>
        public void ReplaceRow(int index, params string[] values)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Replace the value at header at index, if not exist then create it
        /// </summary>
        public void ChangeRow(int index, string header, string value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Implements storage for certain application documents
    /// (Implemented using SQL and LINQ might be too overwhelming, a custom type is more suitable, with JSON maybe) This type of data needs to be able to dynamically expand type (e.g. for table items), and small in size (for there might be more types than existing values), and easily modifiable (through Excel interface?). 
    /// </summary>
    class DataCollection : OrganizationUnit
    {
        // Static accessors
        public static List<KeyValuePair<string, string>> NamedValues;   // Even if a bit wasty, we are not using KeyValuePair<string, object> for simplicity
        public static List<Dictionary<string, string>> Dictionaries;    // Even if a bit wasty, we are not using KeyValuePair<string, object>
        public static List<Table> Tables;

        // Instance data
        public Object Data { get; set; }    // Reference to the underlying data, it should be one item of the lists above; We can also use an index here but use object saves an indexing operation
        private DataType Type { get; set; } // An indicate as to when refered to we know which list to look up at

        // Interface functions
        public DataCollection InstantiateTableFromTable(Table referenceTable) // Generate a new table from existing table, with its headers but not contents
        {
            throw new NotImplementedException();
        }
    }
}
