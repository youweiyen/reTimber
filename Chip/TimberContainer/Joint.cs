using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Chip.TimberContainer
{
    public class Joint
    {
        public Plane Plane;
        public string Type;
        public double Depth;
        public Mesh Face;

        public Joint()
        {
            Plane = Plane.Unset;
            Type = "None";
            Depth = double.NaN;
            Face = new Mesh();
        }



    }
}
