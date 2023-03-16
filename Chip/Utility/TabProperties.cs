using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip.Utility
{
    public class TabProperties : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var server = Grasshopper.Instances.ComponentServer;

            server.AddCategoryShortName("Chip", "CP");
            server.AddCategorySymbolName("Chip", 'C');
            server.AddCategoryIcon("Chip", Properties.Resources.icon);

            return GH_LoadingInstruction.Proceed;
        }
    }
}
