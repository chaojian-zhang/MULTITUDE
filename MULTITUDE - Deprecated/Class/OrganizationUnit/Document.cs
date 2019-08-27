using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    enum DocumentType
    {
        FlowDocument,
        DataCollection,
        RealFile,
        RealFolder
    }

    class Document : OrganizationUnit
    {
        public DocumentType Type { get; set; }  // A re-interpretation from OU type; <Pending> implementation get{}
    }
}
