using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    /// <summary>
    /// Defines an interface for documents that provide addressing of their contents
    /// Consider deprecate this interface, for CEA is volitile by design, thus should just be a form of embeded saerch, not some solid link
    /// </summary>
    interface IContentAddressable
    {
        /// <summary>
        /// Given an address (without []), provide a list of matched content identifiers (in document-type-specific appropriate form); This is used for seraching purpose
        /// </summary>
        /// <param name="address">A well-formated part of a more complete address, interpretation of which is document type specific</param>
        /// <returns></returns>
        object FindMatchedContents(string address);

        /// <summary>
        /// Provides a quick check whether the string partially matches any content in current document; To get accurate search results the document shall provide a seperate interface which isn't required; Notice this is just ambiguous search, not CEA (content element addressing)
        /// </summary>
        /// <returns></returns>
        bool IsPartialMatchContent(string search);
    }
}
