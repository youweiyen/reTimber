using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
using Chip.UnitHelper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Chip.TimberFab
{
    public class GrabPosition : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GrabPosition class.
        /// </summary>
        public GrabPosition()
          : base("GrabPosition", "GP", "Find Grabbing Plane","Chip", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ReclaimedTimber", "RT", "Recliamed Timber Data", GH_ParamAccess.list);
            pManager.AddPlaneParameter("GrabStart", "GS", "Grab Start Plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("timberWidth", "tW", "WIP timber width (should get from ReclaimedTimber Class)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("GrabPlane", "GP", "Tool Plane for Grabbing", GH_ParamAccess.list);
            pManager.AddMeshParameter("GrabSurface", "GS", "WIP Grabbing perpendicular surface", GH_ParamAccess.tree);
            pManager.AddMeshParameter("twoSides", "ts", "twoSides", GH_ParamAccess.list);
            pManager.AddBrepParameter("bf", "bf", "bf", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_Goo> timber_asGoo = new List<IGH_Goo>();
            DA.GetDataList(0, timber_asGoo);

            Plane grabStart = new Plane();
            DA.GetData(1, ref grabStart);

            double timberWidth = 0;
            DA.GetData(2, ref timberWidth);
            List<ReclaimedElement> timber = new List<ReclaimedElement>();
            foreach(IGH_Goo tGoo in timber_asGoo)
            {
                ReclaimedElement timberElement;
                tGoo.CastTo(out timberElement);
                timber.Add(timberElement);
            }

            
            List<Mesh> twosides= new List<Mesh>();
            List<Brep> bf  = new List<Brep>();
            DataTree<Mesh> SeperatedShow = new DataTree<Mesh>();
            int j = 0;

            foreach (ReclaimedElement rt in timber)
            {
                List<Vector3d> rtNormal = new List<Vector3d>();
                //Each Mesh normal
                foreach (BrepFace bF in rt.Boundary.Faces)
                {
                    Vector3d faceNormal = bF.NormalAt(0.5, 0.5);
                    rtNormal.Add(faceNormal);
                }

                //the end tool plane is sometimes flipped so need to compare both opposite normal and see which is closer
                Vector3d closestNormal = rtNormal.OrderBy(fnorm => Vector3d.VectorAngle(grabStart.Normal, fnorm)).First();
                Vector3d furthestNormal = rtNormal.OrderBy(fnorm => Vector3d.VectorAngle(grabStart.Normal, fnorm)).Last();

                int closestInt = rtNormal.IndexOf(closestNormal);
                int furthestInt = rtNormal.IndexOf(furthestNormal);

                double distToclosest = rt.Boundary.Faces[closestInt].PointAt(0.5, 0.5).DistanceTo(grabStart.Origin);
                double distTofurthest = rt.Boundary.Faces[furthestInt].PointAt(0.5, 0.5).DistanceTo(grabStart.Origin);

                BrepFace topFace;
                if (distToclosest > distTofurthest)
                {
                    topFace = rt.Boundary.Faces[furthestInt];
                }
                else
                {
                    topFace = rt.Boundary.Faces[closestInt];
                }
                //BrepFace topFace = rt.Boundary.Faces.OrderBy(top => top.PointAt(0.5, 0.5).DistanceTo(grabStart.Origin)).First();

                //get the perpendicular surfaces
                List<BrepFace> perpFaces = new List<BrepFace>();
                foreach (BrepFace bF in rt.Boundary.Faces)
                {
                    double perpAngle = Vector3d.VectorAngle(topFace.NormalAt(0.5, 0.5), bF.NormalAt(0.5, 0.5)).ToDegrees();
                    if (perpAngle > 88 && perpAngle < 92)
                    {
                        perpFaces.Add(bF);
                    }
                }
                perpFaces.OrderBy(faceArea => AreaMassProperties.Compute(faceArea, true, false, false, false));
                bf.Add(topFace.ToBrep());
                //get the surfaces that are the same normal to each faces of the brep
                //Bounding Brep Normal
                List<Vector3d> brepfacenormals = new List<Vector3d>();
                List<Brep> brepfaces = new List<Brep>();
                Dictionary<int, List<Mesh>> directionMeshes = new Dictionary<int, List<Mesh>>();
                for (int i = 0; i < rt.Boundary.Faces.Count; i++)
                {
                    brepfacenormals.Add(rt.Boundary.Faces[i].NormalAt(0.5, 0.5));
                    brepfaces.Add(rt.Boundary.Faces[i].ToBrep());
                    directionMeshes.Add(i, new List<Mesh>());
                }
                ////group by Each Mesh normal
                //foreach (Mesh mF in rt.SegmentedMesh)
                //{
                //    mF.UnifyNormals();

                //    double avX = mF.Normals.Average(normals => normals.X);
                //    double avY = mF.Normals.Average(normals => normals.Y);
                //    double avZ = mF.Normals.Average(normals => normals.Z);
                //    Vector3d faceNormal = new Vector3d(avX, avY, avZ);

                //    //foreach mesh, compare and assign to closest brep face normal group 
                //    Vector3d closestItem = brepfacenormals.OrderByDescending(fc => (faceNormal - fc).Length).Last();
                //    int closest = brepfacenormals.IndexOf(closestItem);
                //    directionMeshes[closest].Add(mF);
                //}
                //group by closest distance brep
                foreach(Mesh mF in rt.SegmentedMesh)
                {
                    double avX = mF.Vertices.Average(center => center.X);
                    double avY = mF.Vertices.Average(center => center.Y);
                    double avZ = mF.Vertices.Average(center => center.Z);
                    Point3d meshCenterPoint = new Point3d(avX, avY, avZ);

                    
                    Brep closestFace = brepfaces.OrderBy(fc => fc.ClosestPoint(meshCenterPoint).DistanceTo(meshCenterPoint)).First();
                    int closest = brepfaces.IndexOf(closestFace);
                    directionMeshes[closest].Add(mF);
                }
                //locate surface and its normal, find the surface closest to its opposite normal
                int firstFaceInt = rt.Boundary.Faces.OrderBy(brepFace => perpFaces[0].PointAt(0.5, 0.5).DistanceTo(brepFace.PointAt(0.5, 0.5))).First().FaceIndex;
                int secondFaceInt = rt.Boundary.Faces.OrderBy(brepFace => perpFaces[1].PointAt(0.5, 0.5).DistanceTo(brepFace.PointAt(0.5, 0.5))).First().FaceIndex;

                List<Mesh> firstSide = directionMeshes[firstFaceInt];
                List<Mesh> secondSide = directionMeshes[secondFaceInt];

                //order the meshes by the size, big sizes first
                List<Mesh> orderFirstSide = firstSide.OrderByDescending(firstMesh => AreaMassProperties.Compute(firstMesh, true, false, false, false).Area).ToList();
                List<Mesh> orderSecondSide = secondSide.OrderByDescending(secondMesh => AreaMassProperties.Compute(secondMesh, true, false, false, false).Area).ToList();
                twosides.AddRange(orderFirstSide);
                twosides.AddRange(orderSecondSide);
                //smallest mesh grabbing size
                double meshSize = 0.01.FromMeter2();

                foreach (Mesh fM in orderFirstSide)
                {
                    
                    double fMArea = AreaMassProperties.Compute(fM, true, false, false, false).Area;
                    
                    if (fMArea > meshSize)
                    {

                        foreach (Mesh bM in orderSecondSide)
                        {
                            List<Mesh> groupMesh = new List<Mesh>();
                            double bMArea = AreaMassProperties.Compute(bM, true, false, false, false).Area;
                            if (bMArea > meshSize)
                            {
                                try
                                {
                                    RTree tree = new RTree();

                                    for (int t = 0; t < bM.Vertices.Count; t++)
                                    {
                                        tree.Insert(bM.Vertices[t], t);
                                    }

                                    //List<Point3d> treeClosestPoint = new List<Point3d>();
                                    List<int> treeClosestInt = new List<int>();

                                    for (int i = 0; i < fM.Vertices.Count; i++)
                                    {
                                        Point3d vI = fM.Vertices[i];
                                        Sphere searchSphere = new Sphere(vI, timberWidth);

                                        tree.Search(searchSphere, (object sender, RTreeEventArgs events) =>
                                        {
                                            // this will be execute for each point that matches the radius.
                                            RTreeEventArgs e = events;
                                            // look up which point this is
                                            //treeClosestPoint.Add(JointMeshes[k].Vertices[e.Id]);
                                            treeClosestInt.Add(e.Id);
                                        });
                                    }
                                    if (treeClosestInt.Count != 0)
                                    {
                                        groupMesh.Add(fM);
                                        groupMesh.Add(bM);

                                        //for showing in datatree
                                        foreach (Mesh m in groupMesh)
                                        {
                                            GH_Path pth = new GH_Path(j);
                                            SeperatedShow.Add(m, pth);
                                        }
                                        j++;
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(e.Message + " " + e.StackTrace);
                                }
                            }
                            
                        }
                    }
                }   
                    
            }
            DA.SetDataTree(1, SeperatedShow);
            DA.SetDataList(2, twosides);
            DA.SetDataList(3,bf);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("56B86B06-5AA5-4BAB-8494-459A5E4C96A5"); }
        }
    }
}