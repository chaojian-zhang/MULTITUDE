using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.Facility
{
    /// <summary>
    /// Provides an interface designed specifically for Airi, including functions like: 
    /// - listing articles in categories
    /// - listing all kinds of links on a page
    /// - A state of current navigation
    /// - Query abstract/definition for items
    /// - Query main page posts or subject related terminologies for informative purpose
    /// </summary>
    class Wikipedia
    {
        /// We will provide two interfaces: an real-time online one for real-time queries and updated contents; an off-line one with only considered useful information (both can be useful and thus necessary; if we don't have time then at least do online API first since that is easier and more portable)
        /// Online API tests see Wiki folder WikiAPITest project
        // Example online interface
        static void Query() { }
        // Example offline interface
        static void Abstract() { }
    }

    // See this for first paragraph:
    // http://stackoverflow.com/questions/7185288/how-to-get-wikipedia-content-using-wikipedias-api
}
