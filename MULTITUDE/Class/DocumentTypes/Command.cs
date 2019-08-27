using MULTITUDE.Class.Facility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    /// <summary>
    /// Commands are purely in memory, and it can represent a command or a real executable
    /// Notice currently commands are not treated as scripts
    /// To execute a command, use Path + Parameters
    /// </summary>
    [Serializable]
    class Command : Document, ISerializable
    {
        public Command(string path, string metaname, bool bScript = false)
            :base(DocumentType.Command, path, metaname, SystemHelper.CurrentTimeFileNameFriendly){}

        // A wrapper for underlying base
        public Command(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly){ }

        #region Data
        public string Parameters { get; set; }  // A condense representation of strings, manipulated using interface below
        #endregion
        public static readonly string FileSuffix = ".MULTITUDEcm";
        public static readonly string Extensions = ".exe.java.bat";

        #region Interface
        public void AddParameter(string parameter)
        {
            Parameters += " " + Parameters;
        }
        public void RemoveParameter(string parameter)
        {
            Parameters = Parameters.Replace(" " + parameter, "");
        }
        #endregion

        #region Serialization
        public Command(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Load properties
            Parameters = (string)info.GetValue("Parameters", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save Properties
            info.AddValue("Parameters", Parameters);
        }

        protected override void SaveDocument()
        {
            // Empty, do nothing; Document is stored purely in memory
        }

        protected override void LoadDocument()
        {
            // Empty, do nothing; Document is stored purely in memory
        }

        public static Command Import(System.IO.FileInfo file)
        {
            if (file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else if (MULTITUDE.Class.Facility.StringHelper.ExtensionContains(Extensions, file.Extension))
            {
                Command newCommand = new Command(file.FullName, System.IO.Path.GetFileNameWithoutExtension(file.Name));
                return newCommand;
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for command.");
            }
        }

        public override void Export()
        {
            // Create a physical file with .mcm suffix
            throw new NotImplementedException();
        }
        #endregion Serialization
    }
}
