using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Popup
{
    interface IDocumentPropertyViewableWindow
    {
        /// <summary>
        /// Notify user that the document is deleted by clicking "Remove" button in DocumentPropertyPanel; The user window should also be the owner of this DocumentPropertyPanel
        /// The corresponding user window should do three things: 
        ///     1. Hide the DocumentPropertyPanel since it's not longer valid
        ///     2. Delete the corresponding document from the window's view
        ///     3. Move the corresponding document to Home.VoidSpace
        /// </summary>
        /// <param name="document">Document type objet declared as object so as to confine to interface public standard</param>
        void DeleteDocumentFromView(Object document);
    }
}
