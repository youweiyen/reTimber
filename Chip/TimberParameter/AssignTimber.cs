using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Chip.TimberParameter
{
    public class AssignTimber : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AssignTimber class.
        /// </summary>
        public AssignTimber()
          : base("AssignTimber", "AT",
              "Assign Timber to Design",
              "Chip", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ReclaimedElement", "RE", "Recalimed Timber Dataset", GH_ParamAccess.list);
            pManager.AddGeometryParameter("ModelElement", "ME", "3D Model Element to be assigned material", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.icon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("868E9F54-5DE6-46DE-841D-37EF438DB837"); }
        }
    }
}