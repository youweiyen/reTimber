using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Runtime;

namespace Chip.TimberParameter
{
    public class TimberBoolean : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TimberBoolean class.
        /// </summary>
        public TimberBoolean()
          : base("TimberBoolean", "TB", "Boolean Scanned Meshes", "Chip", "Parameter")
        {
        }

        private Mesh m_ = new Mesh();

        private List<Line> l_ = new List<Line>();

        private BoundingBox bbox_ = BoundingBox.get_Unset();

        public override GH_Exposure Exposure => (GH_Exposure)16;

        public override BoundingBox ClippingBox => bbox_;
        protected override void BeforeSolveInstance()
        {
            m_ = new Mesh();
            l_ = new List<Line>();
            bbox_ = BoundingBox.get_Unset();
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (!((GH_Component)this).get_Hidden() && !((GH_ActiveObject)this).get_Locked() && m_ != null)
            {
                args.get_Display().DrawMeshFalseColors(m_);
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            bool previewMeshEdges = CentralSettings.get_PreviewMeshEdges();
            Color color = (((GH_DocumentObject)this).get_Attributes().get_Selected() ? args.get_WireColour_Selected() : args.get_WireColour());
            if (!((GH_Component)this).get_Hidden() && !((GH_ActiveObject)this).get_Locked() && m_ != null)
            {
                if (l_.Count == 0 && previewMeshEdges)
                {
                    args.get_Display().DrawMeshWires(m_, color);
                }
                else
                {
                    args.get_Display().DrawLines((IEnumerable<Line>)l_, Color.Black, 2);
                }
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh0", "M0", "One Mesh to subtract from", (GH_ParamAccess)2);
            pManager.AddMeshParameter("Mesh1", "M1", "Multiple Meshes as cutters", (GH_ParamAccess)2);
            pManager.AddIntegerParameter("Type", "T", "0 Difference, 1 Union, 2 Intersection", (GH_ParamAccess)0, 0);
            pManager.AddNumberParameter("Scale", "S", "Scale cutters", (GH_ParamAccess)0, 1.0);
            pManager.AddColourParameter("Color", "C", "Color for cuts", (GH_ParamAccess)0);
            pManager.AddBooleanParameter("Edges", "E", "Show sharp mesh edge", (GH_ParamAccess)0, false);
            ((GH_ParamManager)pManager).get_Param(1).set_Optional(true);
            ((GH_ParamManager)pManager).get_Param(2).set_Optional(true);
            ((GH_ParamManager)pManager).get_Param(4).set_Optional(true);
            ((GH_ParamManager)pManager).get_Param(5).set_Optional(true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            new Random();
            GH_Structure<GH_Mesh> val = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Mesh> val2 = new GH_Structure<GH_Mesh>();
            DA.GetDataTree<GH_Mesh>(0, ref val);
            DA.GetDataTree<GH_Mesh>(1, ref val2);
            int num = 0;
            DA.GetData<int>(2, ref num);
            double num2 = 1.001;
            DA.GetData<double>(3, ref num2);
            bool flag = false;
            DA.GetData<bool>(5, ref flag);
            Color white = Color.White;
            bool flag2 = DA.GetData<Color>(4, ref white);
            if (white.R == byte.MaxValue && white.G == byte.MaxValue && white.B == byte.MaxValue)
            {
                flag2 = false;
            }
            DataTree<Mesh> val3 = new DataTree<Mesh>();
            BoundingBox boundingBox;
            for (int i = 0; i < val.get_PathCount(); i++)
            {
                GH_Path val4 = val.get_Paths()[i];
                val3.Add(((GH_Goo<Mesh>)(object)val.get_DataItem(val4, 0)).get_Value(), val4);
                if (!val2.PathExists(val4))
                {
                    continue;
                }
                new HashSet<Point3d>();
                List<GH_Mesh> list = new List<GH_Mesh>();
                foreach (GH_Mesh item in val2.get_DataList(val4))
                {
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
                for (int j = 0; j < list.Count; j++)
                {
                    Mesh obj = ((GH_Goo<Mesh>)(object)list[j]).get_Value().DuplicateMesh();
                    boundingBox = ((GeometryBase)obj).GetBoundingBox(true);
                    ((GeometryBase)obj).Transform(Transform.Scale(((BoundingBox)(ref boundingBox)).get_Center(), num2));
                    Mesh val5 = obj;
                    val3.Add(val5, val4);
                }
            }
            DataTree<Mesh> val6 = new DataTree<Mesh>();
            for (int k = 0; k < val3.get_BranchCount(); k++)
            {
                GH_Path val7 = val3.get_Paths()[k];
                List<Mesh> list2 = val3.Branch(val7);
                if (list2.Count == 1)
                {
                    Mesh val8 = list2[0].DuplicateMesh();
                    val8.Unweld(0.0, true);
                    val8.get_VertexColors().CreateMonotoneMesh(Color.FromArgb(200, 200, 200));
                    m_.Append(val8);
                    if (flag)
                    {
                        l_.AddRange(MeshEdgesByAngle(val8, 0.37));
                    }
                    if (((BoundingBox)(ref bbox_)).get_IsValid())
                    {
                        ((BoundingBox)(ref bbox_)).Union(((GeometryBase)m_).GetBoundingBox(false));
                    }
                    else
                    {
                        bbox_ = ((GeometryBase)m_).GetBoundingBox(false);
                    }
                    val6.Add(val8, val7);
                    continue;
                }
                try
                {
                    Mesh val9 = (flag2 ? TestCGAL.CreateMeshBooleanArrayTrackColors(list2.ToArray(), white, Math.Abs(num)) : TestCGAL.CreateMeshBooleanArray(list2.ToArray(), Math.Abs(num)));
                    if (!((CommonObject)val9).get_IsValid())
                    {
                        continue;
                    }
                    Mesh[] array = null;
                    if (num == 0 || num == 1)
                    {
                        array = val9.SplitDisjointPieces();
                        double[] array2 = new double[array.Length];
                        for (int l = 0; l < array.Length; l++)
                        {
                            int num3 = l;
                            boundingBox = ((GeometryBase)array[l]).GetBoundingBox(false);
                            Vector3d diagonal = ((BoundingBox)(ref boundingBox)).get_Diagonal();
                            array2[num3] = ((Vector3d)(ref diagonal)).get_Length();
                        }
                        Array.Sort(array2, array);
                    }
                    else
                    {
                        array = (Mesh[])(object)new Mesh[1] { val9 };
                    }
                    if (!flag2)
                    {
                        array[array.Length - 1].get_VertexColors().CreateMonotoneMesh(Color.FromArgb(200, 200, 200));
                    }
                    m_.Append(array[array.Length - 1]);
                    if (flag)
                    {
                        l_.AddRange(MeshEdgesByAngle(array[array.Length - 1], 0.37));
                    }
                    if (((BoundingBox)(ref bbox_)).get_IsValid())
                    {
                        ((BoundingBox)(ref bbox_)).Union(((GeometryBase)m_).GetBoundingBox(false));
                    }
                    else
                    {
                        bbox_ = ((GeometryBase)m_).GetBoundingBox(false);
                    }
                    val6.Add(array[array.Length - 1], val7);
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine(ex.ToString());
                }
            }
            DA.SetDataTree(0, (IGH_DataTree)(object)val6);
        }

        public List<Line> MeshEdgesByAngle(Mesh mesh, double d = 0.49)
        {
            //IL_003c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0041: Unknown result type (might be due to invalid IL or missing references)
            //IL_004f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0054: Unknown result type (might be due to invalid IL or missing references)
            //IL_0095: Unknown result type (might be due to invalid IL or missing references)
            List<Line> list = new List<Line>();
            mesh.get_FaceNormals().ComputeFaceNormals();
            MeshTopologyEdgeList topologyEdges = mesh.get_TopologyEdges();
            for (int i = 0; i < mesh.get_TopologyEdges().get_Count(); i++)
            {
                int[] connectedFaces = mesh.get_TopologyEdges().GetConnectedFaces(i);
                if (connectedFaces.Length == 2)
                {
                    double num = Vector3d.VectorAngle(Vector3d.op_Implicit(mesh.get_FaceNormals().get_Item(connectedFaces[0])), Vector3d.op_Implicit(mesh.get_FaceNormals().get_Item(connectedFaces[1])));
                    if (num > (0.5 - d) * 3.14159265359 && num < (0.5 + d) * 3.14159265359)
                    {
                        list.Add(topologyEdges.EdgeLine(i));
                    }
                }
            }
            return list;
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
            get { return new Guid("0EB9BA65-4AC5-4125-86E6-FC1D6027FDEF"); }
        }
    }
}