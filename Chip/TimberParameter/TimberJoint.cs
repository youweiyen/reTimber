using System;
using System.Collections.Generic;
using System.Linq;
using Chip.TimberContainer;
//using RecTimberClass;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Chip.UnitHelper;
using System.IO.IsolatedStorage;

namespace Chip.TimberParameter
{
    public class TimberJoint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TimberJoint()
          : base("TimberJoint", "TJ", "Find Timber Joints", "Chip", "Geometry")
        {
        }

        List<ReclaimedElement> timberList = new List<ReclaimedElement>();

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("SegmentedMesh", "SegMesh", "Segmented Mesh", GH_ParamAccess.list);
            pManager.AddCurveParameter("CenterCurve", "Crv", "Center Curve of Timber", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("NonJoint", "SegMesh", "Non Joint Meshes", GH_ParamAccess.tree);
            pManager.AddBrepParameter("BoundingBrep", "Br", "Bounding Box Brep", GH_ParamAccess.item);
            pManager.AddMeshParameter("JointMesh", "JointMesh", "JointMesh", GH_ParamAccess.list);
            pManager.AddGenericParameter("ReclaimedTimber", "RT", "Timber Joint Data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            timberList.Clear();
            ReclaimedElement timber = new ReclaimedElement();
            //List<Mesh> meshFaces = timber.SegmentedMesh;
            List<Mesh> meshFaces = new List<Mesh>();
            Curve centerAxis = null;
            DA.GetDataList(0, meshFaces);
            DA.GetData(1, ref centerAxis);


            //Get aligned object bounding box
            Point3d startpoint = centerAxis.PointAtStart;
            Point3d endpoint = centerAxis.PointAtEnd;
            Vector3d centerVector = new Vector3d(startpoint.X - endpoint.X, startpoint.Y - endpoint.Y, startpoint.Z - endpoint.Z);
            Plane boxPlane = new Plane(startpoint, centerVector);
            Mesh joinedmesh = new Mesh();
            foreach (Mesh mesh in meshFaces) { joinedmesh.Append(mesh); }
            //plane aligned to origin. transform brep to aligned object
            BoundingBox boundingBox = joinedmesh.GetBoundingBox(boxPlane);
            Plane originplane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
            Transform orient = Transform.PlaneToPlane(originplane, boxPlane);
            //boundingBox to Brep
            Brep boundingBrep = Brep.CreateFromBox(boundingBox);
            boundingBrep.Transform(orient);
            //Add to timber
            timber.Boundary = boundingBrep;
            timber.SegmentedMesh = meshFaces;


            //get the surfaces that are the same normal to each faces of the brep
            //Bounding Brep Normal
            List<Vector3d> brepfacenormals = new List<Vector3d>();
            Dictionary<int, List<Mesh>> directionMeshes = new Dictionary<int, List<Mesh>>();
            for (int i = 0; i < boundingBrep.Faces.Count; i++)
            {
                brepfacenormals.Add(boundingBrep.Faces[i].NormalAt(0.5, 0.5));
                directionMeshes.Add(i, new List<Mesh>());
            }
            //Each Mesh normal
            foreach (Mesh mF in meshFaces)
            {
                mF.UnifyNormals();

                double avX = mF.Normals.Average(normals => normals.X);
                double avY = mF.Normals.Average(normals => normals.Y);
                double avZ = mF.Normals.Average(normals => normals.Z);
                Vector3d faceNormal = new Vector3d(avX, avY, avZ);

                //foreach mesh, compare and assign to closest brep face normal group 
                Vector3d closestItem = brepfacenormals.OrderByDescending(fc => (faceNormal - fc).Length).Last();
                int closest = brepfacenormals.IndexOf(closestItem);
                directionMeshes[closest].Add(mF);
                //timber.Normal.Add(faceNormal);
            }

            
            //For each group of normals, find the largest surface there is to set as the timber surface
            Dictionary<int, List<Mesh>>.ValueCollection meshsegs = directionMeshes.Values;
            Joint timberjoint = new Joint();
            List<Mesh> JointMeshes = new List<Mesh>();
            //List<double> depthList = new List<double>();
            DataTree<Mesh> SeperatedShow = new DataTree<Mesh>();

            double jointdepth = 0.009.FromMeter();
            int j = 0;
            //meshsegs = the list of meshes in each group
            foreach (List<Mesh> meshlists in meshsegs)
            {
                //largest mesh center, normal and plane
                Mesh largestmesh = meshlists.OrderByDescending(msh => AreaMassProperties.Compute(msh, true, false, false, false).Area).First();
                double avX = largestmesh.Normals.Average(normals => normals.X);
                double avY = largestmesh.Normals.Average(normals => normals.Y);
                double avZ = largestmesh.Normals.Average(normals => normals.Z);
                Vector3d faceNormal = new Vector3d(avX, avY, avZ);
                double centX = largestmesh.Vertices.Average(ver => ver.X);
                double centY = largestmesh.Vertices.Average(ver => ver.Y);
                double centZ = largestmesh.Vertices.Average(ver => ver.Z);
                Point3d faceCenter = new Point3d(centX, centY, centZ);
                Plane largestPlane = new Plane(faceCenter, faceNormal);

                //move to the origin point to compare z value
                Transform orientBylargestMesh = Transform.PlaneToPlane(largestPlane, originplane);
                //Joint Meshes in single list
                foreach (Mesh msh in meshlists)
                {
                    Mesh dupMsh = new Mesh();
                    dupMsh.Append(msh);
                    dupMsh.Transform(orientBylargestMesh);
                    //z value of each mesh in the list
                    double depth = dupMsh.Vertices.Average(ver => ver.Z);

                    if (Math.Abs(depth) > jointdepth)
                    {
                        JointMeshes.Add(msh);
                        //depthList.Add(depth);
                    }
                }

                //for showing in datatree
                foreach (Mesh m in meshlists)
                {
                    GH_Path pth = new GH_Path(j);
                    SeperatedShow.Add(m, pth);
                }
                j++;
            }
            #region jointGroup
            //make joint meshes into joint group
            //RTree rTree = new RTree();

            List<Mesh> jointGroup = new List<Mesh>();

            if (JointMeshes[0].Vertices.Count < 100)
            {
                List<int> grouped = new List<int>();

                double searchDistance = 0.06.FromMeter();

                for (int p = 0; p < JointMeshes.Count; p++)
                {
                    if (grouped.Contains(p))
                    {
                        continue;
                    }
                    Mesh groupMesh = new Mesh();
                    groupMesh.Append(JointMeshes[p]);
                    grouped.Add(p);
                    for (int k = 0; k < JointMeshes.Count; k++)
                    {
                        if (p == k)
                        {
                            continue;
                        }
                        if (grouped.Contains(k))
                        {
                            continue;
                        }
                        List<Point3d> verticesToCompare = new List<Point3d>();
                        for (int i = 0; i < JointMeshes[k].Vertices.Count; i++)
                        {
                            //rTree.Insert(JointMeshes[k].Vertices[i], i);
                            verticesToCompare.Add(JointMeshes[k].Vertices[i]);
                        }
                        List<Point3d> verticesToFind = new List<Point3d>();
                        for (int i = 0; i < JointMeshes[p].Vertices.Count; i++)
                        {
                            //Point3d vI = JointMeshes[p].Vertices[i];
                            //Sphere searchSphere = new Sphere(vI, searchDistance);

                            //rTree.Search(searchSphere,
                            //    (sender, args) => { if (i < args.Id)
                            //        {
                            //            grouped.Add(k); 
                            //            groupMesh.Append(JointMeshes[k]);

                            //        } });
                            verticesToFind.Add(JointMeshes[p].Vertices[i]);
                        }
                        foreach (Point3d vertCompare in verticesToCompare)
                        {
                            foreach (Point3d vertFind in verticesToFind)
                            {
                                if (vertCompare.DistanceTo(vertFind) < searchDistance)
                                {
                                    grouped.Add(k);
                                    groupMesh.Append(JointMeshes[k]);
                                    continue;
                                }

                            }
                        }
                    }
                    //jointGroup.Add(p, groupMesh);
                    jointGroup.Add(groupMesh);
                }
            }
            #endregion
            //Dictionary<int, Mesh>.ValueCollection meshGroup = jointGroup.Values;
            //make boundingbox around joint, for the scanned joints are scattered, and joints sizes can be measured

            timberjoint.Face = jointGroup;
            timber.Joint = timberjoint;
            //add to timber container


            //add to list
            timberList.Add(timber);

            DA.SetDataTree(0, SeperatedShow);
            DA.SetData(1, boundingBrep);
            DA.SetDataList(2, jointGroup);
            //DA.SetDataList(2, JointMeshes);
            DA.SetDataList(3, timberList);
        }
        public void GroupMeshesUsingRTree(List<Mesh> jointMesh, double searchDistance)
        {
            RTree rTree = new RTree();
            List<int> grouped = new List<int>();
            Dictionary<int, Mesh> jointGroup = new Dictionary<int, Mesh>();

            for (int j = 0; j < jointMesh.Count; j++)
            {
                if (grouped.Contains(j))
                {
                    continue;
                }
                
                for (int k = 0; k < jointMesh.Count; k++)
                {
                    if (j==k)
                    {
                        continue;
                    }

                    for (int i = 0; i < jointMesh[k].Vertices.Count; i++)
                    {
                        rTree.Insert(jointMesh[k].Vertices[i], i);
                    }

                    for (int i = 0; i < jointMesh[j].Vertices.Count; i++)
                    {
                        Point3d vI = jointMesh[j].Vertices[i];
                        Sphere searchSphere = new Sphere(vI, searchDistance);

                        rTree.Search(searchSphere,
                            (sender, args) => { if (i < args.Id) grouped.Add(k); jointGroup.Add(j, jointMesh[k]); });
                    }
                }

            }
            //return jointGroup;
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
            get { return new Guid("E9228D30-AEF8-4777-8020-EAE2FD922BA6"); }
        }
    }
}