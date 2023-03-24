using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
using Chip.UnitHelper;
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
              "Chip", "Assign")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ReclaimedElement", "RE", "Recalimed Timber Dataset", GH_ParamAccess.list);
            pManager.AddBrepParameter("ModelElement", "ME", "3D Model Element to be assigned material", GH_ParamAccess.list);
            pManager.AddBrepParameter("JointGeo", "JG", "Joint geometry to be cut off", GH_ParamAccess.list);
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
            List<Brep> jointModel = new List<Brep>();
            DA.GetDataList(2, jointModel);

            //Brep Center Curve
            List<Polyline> centerPoly = new List<Polyline>();
            List<Vector3d> centerVector = new List<Vector3d>();
            foreach(Brep br in modelElement)
            {
                List<Brep> faceList = new List<Brep>();
                for(int i = 0; i< br.Faces.Count; i++)
                {
                    faceList.Add(br.Faces[i].ToBrep());
                }
                List<Brep> orderFace = faceList.OrderBy(face => face.GetArea()).ToList();
                Vector3d brepVector = AreaMassProperties.Compute(orderFace[1]).Centroid - AreaMassProperties.Compute(orderFace[0]).Centroid;
                Polyline brepPoly = new Polyline
                {
                    AreaMassProperties.Compute(orderFace[0]).Centroid,
                    AreaMassProperties.Compute(orderFace[1]).Centroid
                };
                centerVector.Add(brepVector);
                centerPoly.Add(brepPoly);
            }

            double depthThreshold = 0.05.FromMeter();
            double widthTolerance = 0.03.FromMeter();
            //if timber v/w length is enough, rotate both ways
            //if joint is under threshold, then save joint as possible joint cutting part
            //if over threshold, cut off joint part, and calculate each remaining pieces u length, and save the usable joint position on curve
            //move closed mesh from reused joint part as edge

            //model element dimensions, and its joint dimensions
            for (int b  = 0; b < modelElement.Count;b++)
            {
                List<double> eachEdgeLength = new List<double>();
                foreach(BrepEdge elemEdge in modelElement[b].Edges)
                {
                    eachEdgeLength.Add(elemEdge.GetLength());
                }
                List<double> edgeLength = eachEdgeLength.Distinct().OrderByDescending(length => length).ToList();
                double uBrep, vBrep, wBrep;
                if(edgeLength.Count == 2)
                {
                    uBrep = edgeLength[0];
                    vBrep = edgeLength[1];
                    wBrep = edgeLength[1];
                }
                else
                {
                    uBrep = edgeLength[0];
                    vBrep = edgeLength[1];
                    wBrep = edgeLength[2];
                }

                //all the model joint dimensions and position to assign to
                List<double> depthlist = new List<double>();
                List<double> ulengthlist = new List<double>();
                List<double> vlengthlist = new List<double>();
                List<Plane> planeList = new List<Plane>();
                foreach (Brep jm in jointModel)
                {
                    //find furthest Brep, find Brep distance to Curve(depth), Brep UV length
                    //joint position on curve
                    List<double> faceDepth = new List<double>();
                    List<BrepFace> faceBrep = new List<BrepFace>();

                    foreach (BrepFace brepFace in jm.Faces)
                    {
                        Point3d faceCenter = AreaMassProperties.Compute(brepFace.ToBrep()).Centroid;
                        double distancedepth = faceCenter.DistanceTo(centerPoly[b].ClosestPoint(faceCenter));
                        faceBrep.Add(brepFace);
                        faceDepth.Add(distancedepth);
                    }

                    var brepAnddepth = faceDepth.Select((d, j) => new { Depth = d, Face = j }).OrderByDescending(x => x.Depth);
                    var firstBrepDepth = brepAnddepth.First();
                    var lastBrepDepth = brepAnddepth.Last();
                    //Joint Depth
                    double depth = firstBrepDepth.Depth - lastBrepDepth.Depth;
                    //Joint Position Plane
                    BrepFace furthestBrepFace = faceBrep[firstBrepDepth.Face];
                    Plane jointPlane = new Plane(centerPoly[b].ClosestPoint(furthestBrepFace.PointAt(0.5, 0.5)), furthestBrepFace.NormalAt(0.5, 0.5));
                    planeList.Add(jointPlane);

                    //Joint UV Length
                    List<double> ulength = new List<double>();
                    List<double> vlength = new List<double>();
                    Brep furthestBrep = furthestBrepFace.ToBrep();

                    for (int f = 0; f < furthestBrep.Edges.Count; f++)
                    {
                        Vector3d edgeVector = furthestBrep.Edges[f].PointAtEnd - furthestBrep.Edges[f].PointAtStart;
                        //centerVector is boundingbox orientation vector
                        double edgeAngle = Vector3d.VectorAngle(edgeVector, centerVector[b]);

                        if (80 < edgeAngle.ToDegrees() && edgeAngle.ToDegrees() < 100)
                        {
                            vlength.Add(furthestBrep.Edges[f].GetLength());
                        }
                        else
                        {
                            ulength.Add(furthestBrep.Edges[f].GetLength());
                        }

                    }
                    depthlist.Add(depth);
                    ulengthlist.Add(ulength[0]);
                    vlengthlist.Add(vlength[0]);
                }

                //try to apply each reclaimed element
                foreach (ReclaimedElement element in reclaimedTimber)
                {
                    double maxV = vBrep + widthTolerance;
                    double maxW = wBrep + widthTolerance;
                    double minV = vBrep - widthTolerance;
                    double minW = wBrep - widthTolerance;

                    //if v and w size is within threshold, valid in either rotation then the timber width is correct
                    if(element.vLength > minV && element.vLength < maxV && element.wLength > minW && element.wLength < maxW 
                        || element.wLength > minV && element.wLength < maxV && element.vLength > minW && element.vLength < maxW)
                    {
                        //if joint fits in any of the model joints
                        for (int p = 0; p < element.Joint.Plane.Count; p++)
                        {
                            List<double> modeljointnum = new List<double>();
                            List<double> reclaimJointnum = new List<double>();
                            //compare to each joint in model element
                            for (int uL = 0; uL< ulengthlist.Count; uL++)
                            {
                                if (element.Joint.uLength[p] < ulengthlist[uL] 
                                    && element.Joint.vLength[p] < vlengthlist[uL] 
                                    && element.Joint.Depth[p] < depthlist[uL])
                                {
                                    if(reclaimJointnum.Any(item => item == p) == false)
                                    {

                                    }
                                }
                            }
                            
                        }
                        //if joint depth is under threshold, not then cut away piece
                        for (int i = 0; i < element.Joint.Depth.Count; i++)
                        {
                            if (element.Joint.Depth[i] < depthThreshold)
                            {

                            }
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