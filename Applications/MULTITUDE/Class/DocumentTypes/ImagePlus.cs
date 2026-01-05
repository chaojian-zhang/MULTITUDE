using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    // Not suport CEA
    [Serializable]
    class ImagePlus : Document, ISerializable
    {
        public ImagePlus(string path, string metaname)
            : this(DocumentType.ImagePlus, path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")) { }
        // A wrapper for underlying base
        public ImagePlus(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly){ }

        // Data
        public List<ImageAnnotation> Annotations { get; set; }
        public static readonly string FileSuffix = ".MULTITUDEip";
        public static readonly string Extensions = ".jpg.png.tiff.bmp.gif";

        // Interface

        #region Serialization
        public ImagePlus(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Load properties
            Annotations = (List<ImageAnnotation>)info.GetValue("Annotations", typeof(List<ImageAnnotation>));
            // LoadDocument();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save Properties
            info.AddValue("Annotations", Annotations);
            // SaveDocument()
        }

        protected override void SaveDocument()
        {
            // Empty, do nothing; Document is stored purely in memory
        }

        protected override void LoadDocument()
        {
            // Empty, do nothing; Document is stored purely in memory
        }

        public static ImagePlus Import(System.IO.FileInfo file)
        {
            // Determine whether we are importing regular image or image plus (i.e. with custom data)
            if (file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else if (MULTITUDE.Class.Facility.StringHelper.ExtensionContains(Extensions, file.Extension))
            {
                // For regular images, just create a document referencing it
                return new ImagePlus(file.FullName, file.Name);
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for command.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mip suffix
            throw new NotImplementedException();
        }
        #endregion Serialization
    }

    [Serializable]
    class ImageAnnotation
    {
        public int X { get; set; }  // Locating of floating text, relative to image origin (i.e. upper left corner)
        public int Y { get; set; }
        public string Content { get; set; }
    }
}
