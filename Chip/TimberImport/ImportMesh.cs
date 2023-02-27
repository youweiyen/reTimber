using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Chip.TimberImport
{
    public class ImportMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImportSegments class.
        /// </summary>
        public ImportMesh()
          : base("ImportMesh", "Import", "Import Scanned Meshes to Rhino", "Chip", "Parameter")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Import", "I", "True to import", GH_ParamAccess.item);
            pManager.AddTextParameter("File", "F", "File location", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Summary", "S", "Import Summary", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool toggle = false;
            DA.GetData(0, ref toggle);

            String fileFolder = null;
            DA.GetData(1, ref fileFolder);

            string sum = null;

            while (toggle)
            {
                for (int i = 0; i < 200; i++)
                {
                    String tempFile = fileFolder + "/mesh" + i + ".obj";
                    Rhino.FileIO.FileReadOptions read_options = new Rhino.FileIO.FileReadOptions();
                    read_options.BatchMode = true;
                    read_options.ImportMode = true;
                    Rhino.FileIO.FileObjReadOptions obj_options = new Rhino.FileIO.FileObjReadOptions(read_options);
                    bool objMesh = Rhino.FileIO.FileObj.Read(tempFile, RhinoDoc.ActiveDoc, obj_options);
                    if (objMesh == false)
                    {
                        sum = $"imported {i} files";
                        break;
                    }

                }
                break;
            }

            DA.SetData(0, sum);
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
            get { return new Guid("D2993035-0C13-42D5-8EEF-E841FE2B910D"); }
        }
    }
}