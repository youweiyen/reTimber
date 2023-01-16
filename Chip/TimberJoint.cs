using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace Chip
{
    public class TimberJoint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TimberJoint()
          : base("TimberJoint", "TimberJoint",
              "TimberJoint",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("SegmentMeshes", "SegmentMeshes", "SegmentMeshes", GH_ParamAccess.list);
            pManager.AddCurveParameter("CenterAxis", "CenterAxis", "CenterAxis", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("SeperatedMeshes", "SeperatedMeshes", "SeperatedMeshes", GH_ParamAccess.tree);
            pManager.AddBrepParameter("brep", "brep", "brep", GH_ParamAccess.item);
            pManager.AddMeshParameter("JointMesh", "JointMesh", "JointMesh", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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

            Brep brep = Brep.CreateFromBox(boundingBox);
            brep.Transform(orient);

            //get the surfaces that are the same normal to each faces of the brep
            List<Vector3d> brepfacenormals = new List<Vector3d>();
            Dictionary<int, List<Mesh>> directionMeshes = new Dictionary<int, List<Mesh>>(); 
            for (int i = 0;i < brep.Faces.Count; i++) 
            {
                brepfacenormals.Add(brep.Faces[i].NormalAt(0.5, 0.5));
                directionMeshes.Add(i, new List<Mesh>());
            }
            

            foreach(Mesh mF in meshFaces)
            {
                mF.UnifyNormals();

                double avX = mF.Normals.Average(normals => normals.X);
                double avY = mF.Normals.Average(normals => normals.Y);
                double avZ = mF.Normals.Average(normals => normals.Z);
                Vector3d faceNormal = new Vector3d(avX, avY, avZ);
                //Vector3d meshNormal = (Vector3d)mF.Normals[meshcount/2];
                Vector3d closestItem = brepfacenormals.OrderByDescending(fc => (faceNormal - fc).Length).Last();
                int closest = brepfacenormals.IndexOf(closestItem);
                directionMeshes[closest].Add(mF);
            }
            Dictionary<int,List<Mesh>>.ValueCollection meshsegs= directionMeshes.Values;

            //List<Mesh> SeperatedMeshes = new List<Mesh>();
            DataTree<Mesh> SeperatedMeshes = new DataTree<Mesh>();
            //GH_Structure<IGH_Goo> seperate = new GH_Structure<IGH_Goo>();
            int j = 0;
            foreach (List<Mesh> meshlists in meshsegs)
            {
                
                foreach(Mesh m in meshlists)
                {
                    GH_Path pth = new GH_Path(j);
                    SeperatedMeshes.Add(m, pth);
                }
                j++;
            }

            DA.SetDataTree(0, SeperatedMeshes);
            DA.SetData(1, brep);
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
            get { return new Guid("E9228D30-AEF8-4777-8020-EAE2FD922BA6"); }
        }
    }
}