using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Chip.TimberImport
{
    public class SaveData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SaveData class.
        /// </summary>
        public SaveData()
          : base("SaveData", "SD", "Save Reclaimed Timber Data",
              "Chip", "Parameter")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TimberCurve", "TC", "TimberCurve", GH_ParamAccess.list);
            pManager.AddGenericParameter("TimberJoint", "TJ", "TimberJoint", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Save", "S", "Save to 3dm File", GH_ParamAccess.item, false);
            pManager[2].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ReclaimedTimber", "RT", "Reclaimed Timber Element", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_Goo> curve_asGoo = new List<IGH_Goo>();
            List<IGH_Goo> joint_asGoo = new List<IGH_Goo>();

            DA.GetDataList(0, curve_asGoo);
            DA.GetDataList(1, joint_asGoo);

            List<ReclaimedElement> timberCurve = new List<ReclaimedElement>();

            foreach (IGH_Goo curvegoo in curve_asGoo)
            {
                List<IGH_Goo> gooObject = new List<IGH_Goo> { curvegoo };

                timberCurve = gooObject.Cast<ReclaimedElement>().ToList();
            }

            List<ReclaimedElement> timberCurve = new List<ReclaimedElement>();
            List<ReclaimedElement> jointCurve = new List<ReclaimedElement>();
            timberCurve = curve_asGoo.Cast<ReclaimedElement>().ToList();
            jointCurve = joint_asGoo.Cast<ReclaimedElement>().ToList();


            for(int i  = 0; i < timberCurve.Count; i++)
            {
                timberCurve[i].SegmentedMesh = jointCurve[i].SegmentedMesh;
                timberCurve[i].Joint = jointCurve[i].Joint;
            }

            DA.SetDataList(0, timberCurve);

            //TO DO: Save file to 3dm

            
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
            get { return new Guid("D18FB284-7EC4-4EEA-9E0D-F429FFAEF7C4"); }
        }
    }
}