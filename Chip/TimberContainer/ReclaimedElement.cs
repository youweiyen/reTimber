using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip.TimberContainer
{

    public class ReclaimedElement
    {
        public List <ReclaimedElement> reclaimed { get; set; } = new List<ReclaimedElement>();
        /// <summary>
        /// closed mesh of scanned timber
        /// </summary>
        public Mesh ScannedMesh { get; set; }
        /// <summary>
        /// seperated mesh of scanned timber
        /// </summary>
        public List<Mesh> SegmentedMesh { get; set; }
        /// <summary>
        /// timber joint
        /// </summary>
        public Joint Joint { get; set; }
        /// <summary>
        /// u length of timber, parallel to timber center curve
        /// </summary>
        public double uLength { get; set; }
        /// <summary>
        /// vLength of Timber, first length perpendicular to timber center curve
        /// </summary>
        public double vLength { get; set; }
        /// <summary>
        /// vLength of Timber, second length perpendicular to timber center curve
        /// </summary>
        public double wLength { get; set; }
        /// <summary>
        /// pure centerline of timber
        /// </summary>
        public Polyline Centerline { get; set; }
        /// <summary>
        /// timber bounding box as brep
        /// </summary>
        public Brep Boundary { get; set; }
        /// <summary>
        /// Plane origin of Reclaimed Timber, to compare rotated position
        /// </summary>
        public Plane Plane { get; set; }


    }
}
