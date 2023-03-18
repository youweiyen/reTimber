﻿using Grasshopper.Kernel;
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
        /// <summary>
        /// closed mesh of scanned timber
        /// </summary>
        public List<Mesh> ScannedMesh { get; set; }
        /// <summary>
        /// seperated mesh of scanned timber
        /// </summary>
        public List<Mesh> SegmentedMesh { get; set; }
        /// <summary>
        /// timber joint
        /// </summary>
        public Joint Joint { get; set; }
        /// <summary>
        /// section lines of scanned timber
        /// </summary>
        public List<Polyline> Sections { get; set; }
        /// <summary>
        /// pure centerline of timber
        /// </summary>
        public Polyline Centerline { get; set; }
        /// <summary>
        /// timber bounding box as brep
        /// </summary>
        public Brep Boundary { get; set; }
        /// <summary>
        /// normal of every timber surface
        /// </summary>
        public List<Vector3d> Normal { get; set; }
        /// <summary>
        /// Plane origin of Reclaimed Timber
        /// </summary>
        public Plane Plane { get; set; }


    }
}
