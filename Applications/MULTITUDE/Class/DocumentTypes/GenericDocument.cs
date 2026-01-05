using System;
using System.Runtime.Serialization;

namespace MULTITUDE.Class.DocumentTypes
{
    /// <summary>
    /// Represents a documetn that isn't designed internally by our application, but rather either view-only, or dispathced for external applications to view and edit.
    /// </summary>
    [Serializable]
    class GenericDocument : Document, ISerializable
    {
        // A wrapper for underlying base
        public GenericDocument(DocumentType type, string path, string metaname, string date)
            :base(type, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly) { bDirty = false; }

        #region Serialization
        public GenericDocument(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
        #endregion Serialization

        public override void Export()
        {
            throw new NotSupportedException("Generic documents can only be refrenced.");
        }

        protected override void LoadDocument()
        {
            throw new NotSupportedException("Generic documents can only be refrenced.");
        }

        protected override void SaveDocument()
        {
            return; // Do nothing
        }
    }
}
