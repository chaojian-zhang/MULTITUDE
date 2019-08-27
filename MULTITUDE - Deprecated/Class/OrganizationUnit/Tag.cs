using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    class TagGroup : OrganizationUnit
    {
        public TagGroup Parent { get; set; }
        public List<Tag> SubTags { get; set; }
    }

    class Tag : OrganizationUnit
    {
        public TagGroup Parent { get; set; }
    }
}
