using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
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
            pManager.AddGeometryParameter("ModelElement", "ME", "3D Model Element to be assigned material", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("FitPiece", "FP", "Pieces that are  applicable", GH_ParamAccess.list);
            pManager.AddNumberParameter("WasteSum", "WS", "Cut off length", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_Goo>reTimber_asGoo = new List<IGH_Goo>();
            DA.GetDataList(0, reTimber_asGoo);

            List<ReclaimedElement> reclaimedTimber = new List<ReclaimedElement>();

            foreach(IGH_Goo goo in reTimber_asGoo)
            {
                ReclaimedElement reTimber = new ReclaimedElement();
                goo.CastTo(out reTimber);
                reclaimedTimber.Add(reTimber);
            }

            List<Brep> modelElement = new List<Brep>();
            DA.GetDataList(1, modelElement);

            
            foreach(Brep mElement in modelElement)
            {
                foreach (ReclaimedElement element in reclaimedTimber)
                {
                    //find furthest Brep, find Brep distance to Curve(depth), Brep UV length

                    //element.Joint.BoundingBrep.OrderByDescending(br => AreaMassProperties.Compute(br).Centroid.DistanceTo(element.Centerline.ClosestPoint())
                    //joint position on curve
                    foreach(Brep bBrep in element.Joint.BoundingBrep)
                    {
                        Brep furthestBrepFace = bBrep.Faces.OrderByDescending(brepFace => AreaMassProperties.Compute(brepFace.ToBrep()).Centroid.DistanceTo
                            (element.Centerline.ClosestPoint
                            (AreaMassProperties.Compute(brepFace.ToBrep()).Centroid))).First().ToBrep();

                        double depth = AreaMassProperties.Compute(furthestBrepFace).Centroid.DistanceTo
                            (element.Centerline.ClosestPoint
                            (AreaMassProperties.Compute(furthestBrepFace).Centroid));
                        for(int i = 0; i<bBrep.Edges.Count; i++)
                        {
                            Vector3d edgeVector = bBrep.Edges[i].PointAtEnd - bBrep.Edges[i].PointAtStart; 
                            //if(edgeVector)
                            //bBrep.Edges[i].GetLength;
                        }
                        
                    }
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
            get { return new Guid("868E9F54-5DE6-46DE-841D-37EF438DB837"); }
        }
    }
}