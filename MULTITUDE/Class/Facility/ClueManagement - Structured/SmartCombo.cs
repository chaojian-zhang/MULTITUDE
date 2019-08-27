using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// A class that represents a smart combo box
    /// </summary>
    static class SmartCombo
    {
        static List<Clue> GetComboBoxChoices(DocumentType docType)
        {
            // Automatically filter using Document-definged all recognizable format suffixes

            throw new NotImplementedException();
        }

        // Beginning clue should end with -; E.g. in VW wallpaper
        static List<Clue> GetComboBoxChoices(string beginningClue)
        {
            // Automatically filter out all clues satisfying that beginning clue

            throw new NotImplementedException();
        }
    }
}
