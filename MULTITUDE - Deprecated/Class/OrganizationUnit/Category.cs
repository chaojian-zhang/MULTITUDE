using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    class Category : OrganizationUnit
    {
        public List<Category> Parents;
        public List<OrganizationUnit> Children; // Chlidren can be either other categories, or any of the documents, or other structure nodes that are categorizable, not more than one of them at the same time. This property is inherited from root cateogry in this collection
    }
}
