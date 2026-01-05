using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.CoreInterface
{
    /// <summary>
    /// A facility to fit any document into a designer specified structure (the more detailed the structure definition the better)
    /// There are three main clues to help organization:
    ///     - Hieachy as defined by user
    ///     - Synnoyms
    ///     - Existing knowledge of vocabular and terminology from Wikipedia, and knowledge of other previously defiend categrization structures
    /// The design deliberately supports overlapping of different categorization structures because:
    ///     1. Philosphically speaking everything is realted
    ///     2. This way overlapped areas can be inspiring since we support up-down and down-up navigation
    /// </summary>
    class Categorizer
    {
        // void FitDocument(string docPath, string structurePath);
        // Structure DocumentRelationDiagrams
    }
}
