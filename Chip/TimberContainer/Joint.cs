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
        /// <summary>
        /// Joint base plane
        /// </summary>
        public Plane Plane { get; set; }
        /// <summary>
        /// joint type
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// joint depth compared to same largest normal surface
        /// </summary>
        public double Depth { get; set; }
        /// <summary>
        /// joint meshes
        /// </summary>
        public Mesh Face { get; set; }

        //public Joint()
        //{
        //    Plane = Plane.Unset;
        //    Type = "None";
        //    Depth = double.NaN;
        //    Face = new Mesh();
        //}

    }
}
