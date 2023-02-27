using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Chip.UnitHelper;

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
          : base("TimberCurve", "TC", "Find Timber Center Curve", "Chip", "Parameter")
        {
        }
        //List<Curve> previewCurve = new List<Curve>();

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
            pManager.AddCurveParameter("CenterSection", "CenterSection", "CenterSection", GH_ParamAccess.list);
            pManager.AddGeometryParameter("section", "section", "section", GH_ParamAccess.list);
            pManager.AddGeometryParameter("CenterCurve", "CenterCurve", "CenterCurve", GH_ParamAccess.list);
            pManager.AddGeometryParameter("CenterAxis", "CenterAxis", "CenterAxis", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            List<Mesh> ScannedMeshes = new List<Mesh>();
            List<Mesh> SectionSides = new List<Mesh>();
            DA.GetDataList(0, ScannedMeshes);
            DA.GetDataList(1, SectionSides);

            List<Point3d> contourPoints = new List<Point3d>();
            foreach (Mesh ss in SectionSides)
            {
                BoundingBox bbox = ss.GetBoundingBox(false);
                Point3d p = bbox.Center;
                //Brep bbrep = Brep.CreateFromBox(bbox);
                //Brep sectionFace = bbrep.Faces.OrderByDescending(f => AreaMassProperties.Compute(f, true, false, false, false).Area).First().ToBrep();
                //Point3d p = AreaMassProperties.Compute(sectionFace,false,false,false,false).Centroid;
                contourPoints.Add(p);
            }
            Vector3d sectionVector = new Vector3d(contourPoints[1].X - contourPoints[0].X, contourPoints[1].Y - contourPoints[0].Y, contourPoints[1].Z - contourPoints[0].Z);
            sectionVector.Unitize();
            Transform movealongsection = Transform.Translation(sectionVector);
            Point3d sectionstart = contourPoints[0];
            sectionstart.Transform(movealongsection);
            Mesh JoinMesh = new Mesh();
            foreach (Mesh sm in ScannedMeshes)
            {
                JoinMesh.Append(sm);
            }
            double contourDist = 0.02.FromMeter();
            IEnumerable<Curve> contourcurves = Mesh.CreateContourCurves(JoinMesh, sectionstart, contourPoints[1], contourDist, 0.01).ToList();
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
            List<Point3d> orderedcenter = centers.OrderByDescending(cen => cen.DistanceTo(contourPoints[1])).ToList().ToList();

            bool jointstart = false;
            Vector3d lastvector = new Vector3d();
            List<Polyline> centerCrv = new List<Polyline>();
            List<Point3d> axisPoints = new List<Point3d>();
            for (int i = 0; i < orderedcenter.Count; i++)
            {
                Polyline pline = new Polyline();
                if (i == 0)
                {
                    Vector3d centerdirection = new Vector3d(contourPoints[0].X - orderedcenter[i + 1].X,
                        contourPoints[0].Y - orderedcenter[i + 1].Y,
                        contourPoints[0].Z - orderedcenter[i + 1].Z);
                    pline.Add(contourPoints[0]);
                    pline.Add(orderedcenter[i + 1]);
                    axisPoints.Add(contourPoints[0]);
                    axisPoints.Add(orderedcenter[i + 1]);
                    centerCrv.Add(pline);
                    lastvector = centerdirection;
                }
                else if (i == orderedcenter.Count - 1)
                {
                    Vector3d centerdirection = new Vector3d(orderedcenter[i].X - contourPoints[1].X,
                        orderedcenter[i].Y - contourPoints[1].Y,
                        orderedcenter[i].Z - contourPoints[1].Z);


                    pline.Add(orderedcenter[i]);
                    pline.Add(contourPoints[1]);
                    axisPoints.Add(orderedcenter[i]);
                    axisPoints.Add(contourPoints[1]);
                    centerCrv.Add(pline);
                    lastvector = centerdirection;

                }

                else
                {
                    Vector3d centerdirection = new Vector3d(orderedcenter[i].X - orderedcenter[i + 1].X,
                        orderedcenter[i].Y - orderedcenter[i + 1].Y,
                        orderedcenter[i].Z - orderedcenter[i + 1].Z);
                    double anglediffer = Vector3d.VectorAngle(lastvector, centerdirection);
                    if (anglediffer > 0.3 && jointstart == false)
                    {
                        jointstart = true;
                        lastvector = centerdirection;
                        continue;


                    }
                    else if (jointstart)
                    {
                        //maybe future change the difference to double the angle of the first tilted vector
                        if (anglediffer > 1.04)
                        {
                            jointstart = false;
                            lastvector = new Vector3d(orderedcenter[i + 1].X - orderedcenter[i + 2].X,
                        orderedcenter[i + 1].Y - orderedcenter[i + 2].Y,
                        orderedcenter[i + 1].Z - orderedcenter[i + 2].Z);
                        }
                        continue;
                    }
                    else
                    {
                        pline.Add(orderedcenter[i]);
                        pline.Add(orderedcenter[i + 1]);
                        axisPoints.Add(orderedcenter[i]);
                        axisPoints.Add(orderedcenter[i + 1]);
                        centerCrv.Add(pline);
                        lastvector = centerdirection;
                    }

                }

            }

            List<Polyline> polylines = new List<Polyline>();
            for (int i = 0; i < orderedcenter.Count; i++)
            {
                Polyline pline = new Polyline();
                if (i == 0)
                {
                    pline.Add(contourPoints[0]);
                    pline.Add(orderedcenter[i + 1]);
                    polylines.Add(pline);
                }
                else if (i == orderedcenter.Count - 1)
                {
                    pline.Add(orderedcenter[i]);
                    pline.Add(contourPoints[1]);
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
            //previewCurve.AddRange(contourCrv);


            DA.SetDataList(0, polylines);
            DA.SetDataList(1, contourCrv);
            DA.SetDataList(2, centerCrv);
            DA.SetData(3, centeraxis);

        }

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