using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    [Serializable]
    // Web is purely memory, it can represent a command or a real .html
    // But as a Materializable document type, it makes sense to give it a physical back-end -- it's just all those data are always loaded during creation time (or if this is way too slow we can ignore the file we have materialized)
    class Web : Document, ISerializable
    {
        public Web(string path, string metaname)
            :base(DocumentType.Command, path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")){ }

        // A wrapper for underlying base
        public Web(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly){ }

        // Data
        public Uri URL { get; set; }    // https://www.dotnetperls.com/uri
        public static readonly string FileSuffix = ".MULTITUDEwb";
        public static readonly string Extensions = ".html";

        #region Serialization
        public Web(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Load properties
            URL = (Uri)info.GetValue("URL", typeof(Uri));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save properties
            info.AddValue("URL", URL);
        }

        protected override void SaveDocument()
        {
            // Empty, do nothing; Document is stored purely in memory 
        }

        protected override void LoadDocument()
        {
            // Empty, do nothing; Document is stored purely in memory
        }

        public static Web Import(System.IO.FileInfo file)
        {
            if(file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else if(MULTITUDE.Class.Facility.StringHelper.ExtensionContains(Extensions, file.Extension))
            {
                Web newWeb = new Web(file.FullName, System.IO.Path.GetFileNameWithoutExtension(file.Name));
                return newWeb;
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for web.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mwb suffix
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert from a doc, depending on its type to Web; Notice we might implement some Document base function for documents-shared properties (like meta) can be handled by base
        /// <Development> This is just an example, other document types pending declaration of this function;
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Web ConvertFrom(Document doc)
        {
            throw new NotImplementedException();
        }
        #endregion Serialization
    }
}
