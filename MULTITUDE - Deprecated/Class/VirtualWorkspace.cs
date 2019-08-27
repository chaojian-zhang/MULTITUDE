using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    class VirtualWorkspace
    {
        // VW data
        public string Name { get; set; }
        public List<OrganizationUnit> Items;
        public List<Type> GadgetTypes;  // Type of gadgets that is used to instantiate actual windows
    }
}
