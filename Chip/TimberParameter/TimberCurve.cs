using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Chip.UnitHelper;
using Chip.TimberContainer;
//using RecTimberClass;

namespace Chip.TimberParameter
{
    public class TimberCurve : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TimberCurve()
          : base("TimberCurve", "TC", "Find Timber Center Curve", "Chip", "Geometry")
        {
        }
        //List<Curve> previewCurve = new List<Curve>();
        List<ReclaimedElement> timberList = new List<ReclaimedElement>();

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("ScanMesh", "ScanMesh", "ScanMesh", GH_ParamAccess.list);
            pManager.AddMeshParameter("SectionSides", "SectionSides", "SectionSides", GH_ParamAccess.list);
        }

        
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("SectionCenter", "SectionCenter", "SectionCenter", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Section", "Section", "Section", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("CenterCurve", "CenterCurve", "CenterCurve", GH_ParamAccess.list);
            pManager.AddGeometryParameter("CenterCurve", "CenterCurve", "CenterCurve", GH_ParamAccess.item);
            pManager.AddGenericParameter("ReclaimedTimber", "RT", "Timber Curve Data", GH_ParamAccess.list);
            pManager.AddGeometryParameter("testbox", "tb", "testbox", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            timberList.Clear();
            ReclaimedElement timber = new ReclaimedElement();
            List<Mesh> ScannedMeshes = new List<Mesh>();
            List<Mesh> SectionSides = new List<Mesh>();
            DA.GetDataList(0, ScannedMeshes);
            DA.GetDataList(1, SectionSides);

            timber.ScannedMesh = ScannedMeshes;

            //Get Endpoints for contour direction
            List<Point3d> contourEndPoints = new List<Point3d>();
            foreach (Mesh ss in SectionSides)
            {
                BoundingBox bbox = ss.GetBoundingBox(false);
                Point3d p = bbox.Center;
                //Brep bbrep = Brep.CreateFromBox(bbox);
                //Brep sectionFace = bbrep.Faces.OrderByDescending(f => AreaMassProperties.Compute(f, true, false, false, false).Area).First().ToBrep();
                //Point3d p = AreaMassProperties.Compute(sectionFace,false,false,false,false).Centroid;
                contourEndPoints.Add(p);
            }

            //move starting point a bit inward to get a closed section crv
            Vector3d sectionVector = new Vector3d(contourEndPoints[1].X - contourEndPoints[0].X,
                contourEndPoints[1].Y - contourEndPoints[0].Y,
                contourEndPoints[1].Z - contourEndPoints[0].Z);//0 to 1 direction
            sectionVector.Unitize();
            Transform movealongsection = Transform.Translation(sectionVector);
            Point3d sectionstart = contourEndPoints[0];
            sectionstart.Transform(movealongsection);

            //make sure mesh is single
            Mesh JoinMesh = new Mesh();
            foreach (Mesh sm in ScannedMeshes)
            {
                JoinMesh.Append(sm);
            }

            //contour crv
            double contourDist = 0.02.FromMeter();
            IEnumerable<Curve> contourcurves = Mesh.CreateContourCurves(JoinMesh, sectionstart, contourEndPoints[1], contourDist, 0.01).ToList();
            List<Curve> contourCrv = Curve.JoinCurves(contourcurves, contourDist * 0.1, false).ToList();

            #region dunno
            //List<Curve> contourCrv = new List<Curve>();
            //for(int j = 0; j < joined.Count; j++)
            //{
            //    foreach(Curve crv in joined)
            //    {
            //        if (GeometryBase.GeometryEquals(crv, joined[j])== false)
            //        {
            //            continue;
            //        }
            //        else { contourCrv.Add(joined[j]); }
            //    }

            //}
            //List<Curve> contourCrv = new List<Curve>();
            //for (int i = 0; i < joinedcontourCrv.Count; i++)
            //{
            //    if (i == 0) { contourCrv.Add(joinedcontourCrv[i]); }
            //    else if (i == joinedcontourCrv.Count - 1) { continue; }
            //    else
            //    {
            //        joinedcontourCrv[i - 1].ClosestPoints(joinedcontourCrv[i], out Point3d pointoncurve1, out Point3d pointoncurve2);
            //        if (pointoncurve1.DistanceTo(pointoncurve2) < contourDist * 2)
            //        {
            //            contourCrv.Add(joinedcontourCrv[i]);
            //        }
            //    }
            //}
            #endregion

            //center points of each section and order from closest to starting end
            List<Point3d> centers = new List<Point3d>();
            foreach (Curve crv in contourCrv)
            {
                crv.DivideByCount(20, true, out Point3d[] cP);
                List<Point3d> curvePoints = cP.ToList();
                double averageX = curvePoints.Average(bp => bp.X);
                double averageY = curvePoints.Average(bp => bp.Y);
                double averageZ = curvePoints.Average(bp => bp.Z);
                Point3d average = new Point3d(averageX, averageY, averageZ);
                centers.Add(average);
            }
            //Remove duplicate points
            for (int i = 0 ; i < centers.Count; i++)
            {
                for(int j = centers.Count - 1; j > i; j--)
                {
                    double dupDist = centers[i].DistanceTo(centers[j]);
                    if(dupDist < 0.0001.FromMeter())
                    {
                        centers.RemoveAt(j);
                    }
                }
            }

            List<Point3d> orderedcenter = centers.OrderByDescending(cen => cen.DistanceTo(contourEndPoints[1])).ToList().ToList();

            ////move starting point a bit outward to get a first direction vector
            //Point3d directionstart = contourEndPoints[0];
            //Vector3d initialvector = new Vector3d(contourEndPoints[0].X - contourEndPoints[1].X, 
            //    contourEndPoints[0].Y - contourEndPoints[1].Y, 
            //    contourEndPoints[0].Z - contourEndPoints[1].Z);
            //initialvector.Unitize();
            //Transform movealongdirection = Transform.Translation(initialvector);
            //directionstart.Transform(movealongdirection);

            //bool jointstart = false;
            jointstart jointstart = jointstart.end;
            Vector3d lastvector = new Vector3d();
            //List<Polyline> centerCrv = new List<Polyline>();
            List<Point3d> axisPoints = new List<Point3d>();//points that are not out of range
            double angleTolerance = 0.1;

            for (int i = 0; i < orderedcenter.Count; i++)
            {
                //if it is the starting point, lastvector is the initial vector that the next vector compares to
                if (i == 0)
                {
                    axisPoints.Add(contourEndPoints[0]);

                    //centerCrv.Add(pline);
                    //lastvector = new Vector3d(contourEndPoints[0].X - directionstart.X,
                    //    contourEndPoints[0].Y - directionstart.Y,
                    //    contourEndPoints[0].Z - directionstart.Z);
                    lastvector = new Vector3d(orderedcenter[0].X - contourEndPoints[0].X,
                        orderedcenter[0].Y - contourEndPoints[0].Y,
                        orderedcenter[0].Z - contourEndPoints[0].Z);
                }
                else if (i == orderedcenter.Count - 1)//if it is the last point
                {
                    //Vector3d thisdirection = new Vector3d(orderedcenter[i].X - contourEndPoints[1].X,
                    //    orderedcenter[i].Y - contourEndPoints[1].Y,
                    //    orderedcenter[i].Z - contourEndPoints[1].Z);
                    Vector3d thisdirection = new Vector3d(contourEndPoints[1].X - orderedcenter[i].X,
                        contourEndPoints[1].Y - orderedcenter[i].Y,
                        contourEndPoints[1].Z - orderedcenter[i].Z);

                    double anglediffer = Vector3d.VectorAngle(lastvector, thisdirection);
                    if (Math.Abs(anglediffer) > angleTolerance)
                    {
                        axisPoints.Add(contourEndPoints[1]);
                    }
                    else
                    {
                        axisPoints.Add(orderedcenter[i]);
                        axisPoints.Add(contourEndPoints[1]);
                        //centerCrv.Add(pline);
                    }
                }

                else
                {
                    Vector3d thisdirection = new Vector3d(orderedcenter[i].X - orderedcenter[i - 1].X,
                        orderedcenter[i].Y - orderedcenter[i - 1].Y,
                        orderedcenter[i].Z - orderedcenter[i - 1].Z);
                    //Vector3d thisdirection = new Vector3d(orderedcenter[i].X - orderedcenter[i + 1].X,
                    //    orderedcenter[i].Y - orderedcenter[i + 1].Y,
                    //    orderedcenter[i].Z - orderedcenter[i + 1].Z);

                    double anglediffer = Vector3d.VectorAngle(lastvector, thisdirection);
                    
                    //if the angle is larger than 0.1 and it is the first different vector, then skip 
                    if (Math.Abs(anglediffer) > angleTolerance && jointstart == jointstart.end)
                    {
                        jointstart = jointstart.start;
                        lastvector = thisdirection;

                    }
                    else if (jointstart == jointstart.start)
                    {
                        if(Math.Abs(anglediffer) > angleTolerance)
                        {
                            jointstart = jointstart.middle;
                            //    lastvector = new Vector3d(orderedcenter[i+1].X - orderedcenter[i].X,
                            //orderedcenter[i + 1].Y - orderedcenter[i].Y,
                            //orderedcenter[i + 1].Z - orderedcenter[i].Z);
                            lastvector = new Vector3d(orderedcenter[i].X - orderedcenter[i - 1].X,
                                orderedcenter[i].Y - orderedcenter[i - 1].Y,
                                orderedcenter[i].Z - orderedcenter[i - 1].Z);
                            
                        }

                        //if it is the end of the different vector then skip and go back to comparing with normal vector

                    }
                    else if (jointstart == jointstart.middle)
                    {
                        if(Math.Abs(anglediffer) > angleTolerance)
                        {
                            jointstart = jointstart.end;
                            //lastvector = new Vector3d(orderedcenter[i + 1].X - orderedcenter[i].X,
                            //    orderedcenter[i + 1].Y - orderedcenter[i].Y,
                            //    orderedcenter[i + 1].Z - orderedcenter[i].Z);
                            lastvector = new Vector3d(orderedcenter[i+1].X - orderedcenter[i].X,
                                orderedcenter[i+1].Y - orderedcenter[i].Y,
                                orderedcenter[i + 1].Z - orderedcenter[i].Z);
                        }

                    }
                    //if its the same vector and it has the same vector as the starting vector, then add point
                    else
                    {
                        axisPoints.Add(orderedcenter[i]);
                        //centerCrv.Add(pline);
                        lastvector = thisdirection;
                    }

                }

            }

            List<Polyline> polylines = new List<Polyline>();
            for (int i = 0; i < orderedcenter.Count; i++)
            {
                Polyline pline = new Polyline();
                if (i == 0)
                {
                    pline.Add(contourEndPoints[0]);
                    pline.Add(orderedcenter[i + 1]);
                    polylines.Add(pline);
                }
                else if (i == orderedcenter.Count - 1)
                {
                    pline.Add(orderedcenter[i]);
                    pline.Add(contourEndPoints[1]);
                    polylines.Add(pline);
                }
                else
                {
                    pline.Add(orderedcenter[i]);
                    pline.Add(orderedcenter[i + 1]);
                    polylines.Add(pline);
                }

            }
            Polyline centeraxis = new Polyline();
            foreach (Point3d aP in axisPoints)
            {
                centeraxis.Add(aP);
            }

            //Timber Dimension
            //Get aligned object bounding box
            Vector3d centerVector = contourEndPoints[1] - contourEndPoints[0];
            Plane boxPlane = new Plane(contourEndPoints[0], centerVector);
            //plane aligned to origin. transform brep to aligned object
            Plane originplane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
            BoundingBox boundingBox = JoinMesh.GetBoundingBox(boxPlane);
            Transform orient = Transform.PlaneToPlane(originplane, boxPlane);
            //boundingBox to Brep
            Brep boundingBrep = Brep.CreateFromBox(boundingBox);
            boundingBrep.Transform(orient);
            
            List<double> allEdgeLength = new List<double>();
            foreach(BrepEdge edge in boundingBrep.Edges)
            {
                allEdgeLength.Add(edge.GetLength());
            }

            List<double>singleLength = allEdgeLength.Distinct().OrderBy(edgeLength => edgeLength).ToList();

            double ulength, vlength, wlength;

            if (singleLength.Count == 2)
            {
                ulength = singleLength[1];
                vlength = singleLength[0];
                wlength = singleLength[0];
            }
            else
            {
                vlength = singleLength[0];
                wlength = singleLength[1];
                ulength = singleLength[2];
            }
            //timber plane
            Plane timberPlane = new Plane(centeraxis.ToPolylineCurve().PointAtStart, centerVector);

            //Add to timber
            timber.Boundary = boundingBrep;
            //previewCurve.AddRange(contourCrv);
            timber.Centerline = centeraxis;
            timber.uLength = ulength;
            timber.vLength = vlength;
            timber.wLength = wlength;
            timber.Plane= timberPlane;

            //Add timber to List
            timberList.Add(timber);

            DA.SetDataList(0, polylines);
            DA.SetDataList(1, contourCrv);
            //DA.SetDataList(2, centerCrv);
            DA.SetData(2, centeraxis);
            DA.SetDataList(3, timberList);
            DA.SetData(4, boundingBrep);

        }
        #region Preview
        //preview
        //public override void DrawViewportMeshes(IGH_PreviewArgs args)
        //{
        //    Rhino.Display.DisplayMaterial dm = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Blue);
        //    if (previewCurve != null)
        //    {
        //        foreach (var cr in previewCurve)
        //        {
        //            if (cr != null)
        //                args.Display.DrawCurve(cr, System.Drawing.Color.AliceBlue, 2);
        //        }


        //    }
        //}
        #endregion
        public enum jointstart
        {
            start = 0,
            middle = 1,
            end = 2,
        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c4e36432-eeac-42f7-8682-f08a6f8cf886");
    }
    
}