using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Chip
{
    public class ChipComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ChipComponent()
          : base("TimberCurve", "TimberCurve",
            "TimberCurve",
            "Category", "Subcategory")
        {
        }
        //List<Curve> previewCurve = new List<Curve>();

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("ScanMesh", "ScanMesh", "ScanMesh", GH_ParamAccess.list);
            pManager.AddMeshParameter("SectionSides", "SectionSides", "SectionSides", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("CenterCurve","CenterCurve", "CenterCurve", GH_ParamAccess.list);
            pManager.AddGeometryParameter("section","section","section",GH_ParamAccess.list);
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
            foreach(Mesh ss in SectionSides) 
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
            foreach(Mesh sm in ScannedMeshes)
            {
                JoinMesh.Append(sm);
            }
            double contourDist = NumericExtensions.FromMeter(0.02);
            IEnumerable<Curve> contourcurves = Mesh.CreateContourCurves(JoinMesh, sectionstart, contourPoints[1], contourDist, 0.01).ToList();
            List<Curve> contourCrv = Curve.JoinCurves(contourcurves, contourDist*0.1, false).ToList();
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
            List<Point3d> centers = new List<Point3d>();
            foreach(Curve crv in contourCrv)
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
            List<Polyline> polylines = new List<Polyline>();
            for(int i= 0; i< orderedcenter.Count; i++)
            {
                Polyline pline = new Polyline();
                if(i == 0)
                {
                    pline.Add(contourPoints[0]);
                    pline.Add(orderedcenter[i+1]);
                    polylines.Add(pline);
                }
                else if (i == orderedcenter.Count-1) 
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

            //previewCurve.AddRange(contourCrv);


            DA.SetDataList(0, polylines);
            DA.SetDataList(1, contourCrv);

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
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c4e36432-eeac-42f7-8682-f08a6f8cf886");
    }
    public static class NumericExtensions
    {
        public static double FromMeter(this double length)
        {
            return length * GetConversionFactor();
        }


        public static double GetConversionFactor()
        {

            switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
            {
                case Rhino.UnitSystem.Meters:
                    return 1.0;

                case Rhino.UnitSystem.Millimeters:
                    return 1000.0;

                case Rhino.UnitSystem.Centimeters:
                    return 100.0;

                case Rhino.UnitSystem.Feet:
                    return 304.8 / 1000.0;

                default:
                    throw new Exception("unknown units");
            }
        }
    }
}