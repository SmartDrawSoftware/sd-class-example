using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XMLToJSONDocDump
{
    /// <summary>
    /// Class for specifically doing a dump of the SDON assembly.
    /// </summary>
    public class SDONDocDump : JSONDocDump
    {
        protected override bool ShouldIncludeType(Type type)
        {
            if (type == null) return false;
            if (type.Namespace == "SDON.Model") return true; //dont include anything that isnt in the model namespace

            return false;
        }
    }
}
