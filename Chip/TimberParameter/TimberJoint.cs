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
using System.Net.Configuration;

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
            pManager.AddMeshParameter("Surfaces", "S", "Remaining Mesh Surfaces", GH_ParamAccess.tree);
            pManager.AddBrepParameter("JointBound", "JB", "Bounding Box Brep", GH_ParamAccess.list);
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
            //timber.Boundary = boundingBrep;
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
                if(meshlists.Count > 0)
                {
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
            //not rtree

            //List<Mesh> jointGroup = new List<Mesh>();
            //if(JointMeshes.Count != 0)
            //{
            //    if (JointMeshes[0].Vertices.Count < 100)
            //    {
            //        List<int> grouped = new List<int>();
            //        double searchDistance = 0.06.FromMeter();
            //        for (int p = 0; p < JointMeshes.Count; p++)
            //        {
            //            if (grouped.Contains(p))
            //            {
            //                continue;
            //            }

            //            Mesh groupMesh = new Mesh();
            //            groupMesh.Append(JointMeshes[p]);
            //            grouped.Add(p);

            //            for (int k = 0; k < JointMeshes.Count; k++)
            //            {
            //                if (p == k)
            //                {
            //                    continue;
            //                }
            //                if (grouped.Contains(k))
            //                {
            //                    continue;
            //                }
            //                List<Point3d> verticesToCompare = new List<Point3d>();

            //                for (int i = 0; i < JointMeshes[k].Vertices.Count; i++)
            //                {
            //                    verticesToCompare.Add(JointMeshes[k].Vertices[i]);
            //                }
            //                List<Point3d> verticesToFind = new List<Point3d>();
            //                List<Point3d> treeClosestPoint= new List<Point3d>();
            //                List<int> treeClosestInt = new List<int>();
            //                for (int i = 0; i < JointMeshes[p].Vertices.Count; i++)
            //                {
            //                    verticesToFind.Add(JointMeshes[p].Vertices[i]);
            //                }
            //                foreach (Point3d vertCompare in verticesToCompare)
            //                {
            //                    foreach (Point3d vertFind in verticesToFind)
            //                    {
            //                        if (vertCompare.DistanceTo(vertFind) < searchDistance)
            //                        {
            //                            grouped.Add(k);
            //                            groupMesh.Append(JointMeshes[k]);
            //                            continue;
            //                        }

            //                    }
            //                }
            //            }
            //            //jointGroup.Add(p, groupMesh);
            //            jointGroup.Add(groupMesh);
            //        }
            //    }
            //}

            #endregion

            #region jointGroup_RTree
            List<Mesh> jointGroup = new List<Mesh>();
            if (JointMeshes.Count != 0)
            {
                List<int> alreadyGrouped = new List<int>();
                double searchDistance = 0.06.FromMeter();

                for (int p = 0; p < JointMeshes.Count; p++)
                {
                    if (alreadyGrouped.Contains(p))
                    {
                        continue;
                    }

                    Mesh groupMesh = new Mesh();
                    groupMesh.Append(JointMeshes[p]);
                    alreadyGrouped.Add(p);

                    for (int k = 0; k < JointMeshes.Count; k++)
                    {
                        if (p == k)
                        {
                            continue;
                        }
                        if (alreadyGrouped.Contains(k))
                        {
                            continue;
                        }
                        try
                        {
                            RTree tree = new RTree();

                            for (int t = 0; t < JointMeshes[k].Vertices.Count; t++)
                            {
                                tree.Insert(JointMeshes[k].Vertices[t], t);
                            }

                            //List<Point3d> treeClosestPoint = new List<Point3d>();
                            List<int> treeClosestInt = new List<int>();

                            for (int i = 0; i < JointMeshes[p].Vertices.Count; i++)
                            {
                                Point3d vI = JointMeshes[p].Vertices[i];
                                Sphere searchSphere = new Sphere(vI, searchDistance);

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
                                groupMesh.Append(JointMeshes[k]);
                                alreadyGrouped.Add(k);
                            }
                        } 
                        catch (Exception e)
                        {
                            throw new Exception(e.Message + " " + e.StackTrace);
                        }
                    }
                    jointGroup.Add(groupMesh);
                }
            }
            #endregion

            //make boundingbox around joint, for the scanned joints are scattered, and joints sizes can be measured
            //Get aligned object bounding box, same orientation and method as finding bounding box brep for timber element

            List<Brep>jointBreps= new List<Brep>();
            List<Mesh> jointMeshes = new List<Mesh>();
            foreach (Mesh joint in jointGroup) 
            {
                BoundingBox jointBound = joint.GetBoundingBox(boxPlane);

                //if face == 1, then is not joint
                if(joint.Faces.Count != 1)
                {
                    
                    //boundingBox to Brep
                    Brep jointBrep = Brep.CreateFromBox(jointBound);
                    if(jointBrep != null)
                    {
                        jointBrep.Transform(orient);
                        jointBreps.Add(jointBrep);

                        jointMeshes.Add(joint);
                    }

                }
                

            }

            //JointSize, Depth, UV
            List<double>depthlist= new List<double>();
            List<double>ulengthlist= new List<double>();
            List<double>vlengthlist= new List<double>();
            List<Plane> planeList = new List<Plane>();
            
            foreach (Brep jBox in jointBreps)
            {
                //find furthest Brep, find Brep distance to Curve(depth), Brep UV length
                //joint position on curve
                List<double> faceDepth = new List<double>();
                List<BrepFace> faceBrep = new List<BrepFace>();
                
                foreach(BrepFace brepFace in jBox.Faces)
                {
                    Point3d faceCenter = AreaMassProperties.Compute(brepFace.ToBrep()).Centroid;
                    centerAxis.ClosestPoint(faceCenter, out double t);
                    double distancedepth = faceCenter.DistanceTo(centerAxis.PointAt(t));
                    faceBrep.Add(brepFace);
                    faceDepth.Add(distancedepth);
                }
                
                var brepAnddepth = faceDepth.Select((d, i) => new { Depth = d, Face = i }).OrderBy(x => x.Depth);
                var closestBrepDepth = brepAnddepth.First();
                var secondBrepDepth = brepAnddepth.ElementAt(1);
                
                //if the joint is edge tenon joint, apply uvw 0, neglect the joint, cut it off
                if (closestBrepDepth.Depth < 0.001 && closestBrepDepth.Depth > -0.001 && secondBrepDepth.Depth < 0.001 && secondBrepDepth.Depth > -0.001)
                {
                    //use u length to cut off curve
                    BrepFace closestBrepFace = faceBrep[closestBrepDepth.Face];
                    BrepFace secondBrepFace = faceBrep[secondBrepDepth.Face];
                    double ulength = closestBrepFace.PointAt(0.5, 0.5).DistanceTo(secondBrepFace.PointAt(0.5, 0.5));

                    //For now not much of the length is effected, skip, but future need to cut off centeraxis length

                    #region jointPlane_OBSOLETE
                    depthlist.Add(-1);
                    ulengthlist.Add(ulength);
                    vlengthlist.Add(-1);

                    //Joint Position Plane
                    Point3d closestBrepCenter = AreaMassProperties.Compute(closestBrepFace).Centroid;
                    Point3d secondBrepCenter = AreaMassProperties.Compute(secondBrepFace).Centroid;

                    Point3d midPoint = new Point3d((closestBrepCenter.X + secondBrepCenter.X) / 2,
                                                    (closestBrepCenter.Y + secondBrepCenter.Y) / 2,
                                                        (closestBrepCenter.Z + secondBrepCenter.Z) / 2);
                    centerAxis.ClosestPoint(midPoint, out double pointOnCurveParam);
                    Plane jointPlane = new Plane(centerAxis.PointAt(pointOnCurveParam), closestBrepFace.NormalAt(0.5, 0.5));
                    planeList.Add(jointPlane);
                    #endregion
                }
                //normal joints
                else
                {
                    //Joint Depth
                    double depth = 0;
                    for (int cl = 0; cl < faceBrep.Count; cl++)
                    {
                        Vector3d eachNormal = faceBrep[cl].NormalAt(0.5, 0.5);
                        Vector3d closestNormal = faceBrep[closestBrepDepth.Face].NormalAt(0.5, 0.5);
                        double oppositeAngle = Vector3d.VectorAngle(eachNormal, closestNormal);
                        if (oppositeAngle.ToDegrees() < 190 && oppositeAngle.ToDegrees() > 170)
                        {
                            double dist = AreaMassProperties.Compute(faceBrep[cl].ToBrep()).Centroid
                                .DistanceTo(AreaMassProperties.Compute(faceBrep[closestBrepDepth.Face].ToBrep()).Centroid);
                            depth = dist;
                        }
                    }

                    //Joint Position Plane
                    BrepFace closestBrepFace = faceBrep[closestBrepDepth.Face];
                    centerAxis.ClosestPoint(closestBrepFace.PointAt(0.5, 0.5), out double pointOnCurveParam);
                    Plane jointPlane = new Plane(centerAxis.PointAt(pointOnCurveParam), closestBrepFace.NormalAt(0.5, 0.5));
                    planeList.Add(jointPlane);

                    //Joint UV Length
                    List<double> ulength = new List<double>();
                    List<double> vlength = new List<double>();
                    Brep closestBrep = closestBrepFace.ToBrep();

                    for (int i = 0; i < closestBrep.Edges.Count; i++)
                    {
                        Vector3d edgeVector = closestBrep.Edges[i].PointAtEnd - closestBrep.Edges[i].PointAtStart;
                        //centerVector is boundingbox orientation vector
                        double edgeAngle = Vector3d.VectorAngle(edgeVector, centerVector);

                        if (80 < edgeAngle.ToDegrees() && edgeAngle.ToDegrees() < 100)
                        {
                            vlength.Add(closestBrep.Edges[i].GetLength());
                        }
                        else
                        {
                            ulength.Add(closestBrep.Edges[i].GetLength());
                        }

                    }
                    //if joint ulength not identified, then add 0, usually problem is because the center curve is identified uncorrectly
                    if(ulength.Count != 0)
                    {
                        depthlist.Add(depth);
                        ulengthlist.Add(ulength[0]);
                        vlengthlist.Add(vlength[0]);
                    }
                    else
                    {
                        depthlist.Add(0);
                        ulengthlist.Add(0);
                        vlengthlist.Add(0);
                    }
                }

            }

            //add to timber container
            timberjoint.Face = jointMeshes;
            timberjoint.BoundingBrep= jointBreps;
            timberjoint.Depth = depthlist;
            timberjoint.uLength = ulengthlist;
            timberjoint.vLength = vlengthlist;
            timberjoint.Plane = planeList;
            timber.Joint = timberjoint;
            
            //add to list
            timberList.Add(timber);

            DA.SetDataTree(0, SeperatedShow);
            DA.SetDataList(1, jointBreps);
            DA.SetDataList(2, jointMeshes);
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