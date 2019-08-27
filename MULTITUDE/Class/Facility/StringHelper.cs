using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    static class StringHelper
    {
        public static bool ExtensionContains(string Extensions, string compare)
        {
            int lastIndex = Extensions.LastIndexOf(compare);
            return (Extensions.Contains(compare + '.') || (lastIndex != -1 && lastIndex == Extensions.Length - compare.Length));
        }
    }
}
