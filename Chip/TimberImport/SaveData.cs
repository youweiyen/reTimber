using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Input.Custom;
//using RecTimberClass;

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
            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager[1].DataMapping = GH_DataMapping.Flatten;
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

            bool bake = false;
            DA.GetData(2, ref bake);

            List<ReclaimedElement> timberCurve = new List<ReclaimedElement>();
            List<ReclaimedElement> jointCurve = new List<ReclaimedElement>();

            foreach (IGH_Goo curveGoo in curve_asGoo)
            {
                ReclaimedElement curvesingle = new ReclaimedElement();
                curveGoo.CastTo(out curvesingle);
                timberCurve.Add(curvesingle);
            }

            foreach (IGH_Goo jointGoo in joint_asGoo)
            {
                ReclaimedElement jointsingle = new ReclaimedElement();
                jointGoo.CastTo(out jointsingle);
                jointCurve.Add(jointsingle);
            }

            
            List<ReclaimedElement> reclaimedTimberElements = new List<ReclaimedElement>();

            //for (int i  = 0; i < timberCurve.Count; i++)
            //{
            //    timberCurve[i].SegmentedMesh = jointCurve[i].SegmentedMesh;
            //    timberCurve[i].Joint = jointCurve[i].Joint;
            //}

            for (int i = 0; i < timberCurve.Count; i++)
            {
                ReclaimedElement reclaimedTimber = new ReclaimedElement
                {
                    ScannedMesh = timberCurve[i].ScannedMesh,
                    SegmentedMesh = jointCurve[i].SegmentedMesh,
                    Centerline = timberCurve[i].Centerline,
                    uLength = timberCurve[i].uLength,
                    vLength = timberCurve[i].vLength,
                    wLength = timberCurve[i].wLength,
                    Joint = jointCurve[i].Joint,
                    Boundary = timberCurve[i].Boundary,
                    Plane = timberCurve[i].Plane
                };

                reclaimedTimberElements.Add(reclaimedTimber);
            }

            DA.SetDataList(0, reclaimedTimberElements);

            //TO DO: Save file to 3dm
            //TO DO: Bake attributes

            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            string parent = "ReclaimedTimber";
            if (bake)
            {
                //create parent layer
                int index = doc.Layers.FindByFullPath(parent, -1);
                if (index < 0) doc.Layers.Add(parent, System.Drawing.Color.Black);
                index = doc.Layers.FindByFullPath(parent, -1);
                Rhino.DocObjects.Layer parentLayer = doc.Layers[index];

                //construct child layer
                for(int la = 0; la < reclaimedTimberElements.Count; la++)
                {
                    string child = $"{la}";
                    Rhino.DocObjects.Layer childLayer = new Rhino.DocObjects.Layer();
                    childLayer.ParentLayerId = parentLayer.Id;
                    childLayer.Name = child;
                    childLayer.Color = System.Drawing.Color.BlueViolet;

                    string childrenName = parent + "::" + child;

                    //create child layer
                    index = doc.Layers.FindByFullPath(childrenName, -1);
                    if (index < 0) index = doc.Layers.Add(childLayer);

                    Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();

                    att.LayerIndex = index;
                    att.SetUserString($"_{la}_Ulength", Math.Round(reclaimedTimberElements[la].uLength, 2).ToString());
                    att.SetUserString($"_{la}_VLength", Math.Round(reclaimedTimberElements[la].vLength, 2).ToString());
                    att.SetUserString($"_{la}_WLength", Math.Round(reclaimedTimberElements[la].wLength, 2).ToString());

                    for(int jo = 0; jo < reclaimedTimberElements[la].Joint.Depth.Count; jo++)
                    {
                        att.SetUserString($"_{la}_{jo}_Joint_ULength", Math.Round(reclaimedTimberElements[la].Joint.uLength[jo], 2).ToString());
                        att.SetUserString($"_{la}_{jo}_Joint_VLength", Math.Round(reclaimedTimberElements[la].Joint.vLength[jo], 2).ToString());
                        att.SetUserString($"_{la}_{jo}_Joint_Depth", Math.Round(reclaimedTimberElements[la].Joint.Depth[jo], 2).ToString());
                        att.SetUserString($"_{la}_{jo}_Joint_Plane", reclaimedTimberElements[la].Joint.Plane[jo].ToString());
                        att.SetUserString($"_{la}_{jo}_Joint_Mesh", reclaimedTimberElements[la].Joint.Face[jo].ToString());
                        att.SetUserString($"_{la}_{jo}_Joint_Bound", reclaimedTimberElements[la].Joint.BoundingBrep[jo].ToString());
                    }

                    att.SetUserString($"_{la}_Scanned_Mesh", reclaimedTimberElements[la].ScannedMesh.ToString());
                    att.SetUserString($"_{la}_Segmented_Mesh", reclaimedTimberElements[la].SegmentedMesh.ToString());
                    att.SetUserString($"_{la}_Boundary", reclaimedTimberElements[la].Boundary.ToString());
                    att.SetUserString($"_{la}_Plane", reclaimedTimberElements[la].Plane.ToString());
                    att.SetUserString($"_{la}_Center_Curve", reclaimedTimberElements[la].Centerline.ToString());

                    doc.Objects.Add(reclaimedTimberElements[la].ScannedMesh, att);
                    //doc.Objects.Add(reclaimedTimberElements[la].SegmentedMesh);
                    //doc.Objects.Add(reclaimedTimberElements[la].Plane);
                    //doc.Objects.Add(reclaimedTimberElements[la].Centerline);
                }

            }
            
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