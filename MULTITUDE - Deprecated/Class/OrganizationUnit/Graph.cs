using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    /// <summary>
    /// A graph describes interconnections between a bunch of OUs
    /// </summary>
    class Graph : OrganizationUnit
    {
        public List<Link> GraphLinks;
        public string Topic { get { return Name; } set { Name = value; } }
    }
}
