using MULTITUDE.Class.DocumentTypes;
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
            Home home = (App.Current as App).CurrentHome;

            List<Clue> results = new List<Clue>();

            // Automatically filter using Document-definged all recognizable formats
            // <Development> Multithread it and it will be very fast
            foreach (Document doc in home.Documents)
            {
                if (doc.Type == docType) results.AddRange(doc.Clues);
            }

            return results.Distinct().ToList();
        }

        static List<Document> GetSatisfyingDocuments(List<Clue> clues)
        {
            List<Document> results = new List<Document>();

            foreach (Clue clue in clues)
            {
                results.AddRange(ClueManager.Manager.GetDocuments(clue));
            }

            return results.Distinct().ToList();
        }
    }
}
