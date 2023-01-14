using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Chip
{
    public class ChipInfo : GH_AssemblyInfo
    {
        public override string Name => "Chip";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("1117cc77-b684-497c-af24-40e938f06310");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}