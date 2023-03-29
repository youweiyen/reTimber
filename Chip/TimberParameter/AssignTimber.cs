﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Chip.TimberContainer;
using Chip.UnitHelper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Types.Transforms;
using Rhino.DocObjects;
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
            pManager.AddCurveParameter("C", "c", "c", GH_ParamAccess.list);
            pManager.AddPointParameter("cP", "cP", "cP", GH_ParamAccess.list);
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
            List<Curve> centerCurve = new List<Curve>();
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
                brepPoly.ToPolylineCurve();
                centerVector.Add(brepVector);
                centerCurve.Add(brepPoly.ToPolylineCurve());
            }

            double depthThreshold = 0.05.FromMeter();
            double widthTolerance = 0.03.FromMeter();
            //if timber v/w length is enough, rotate both ways
            //if joint is under threshold, then save joint as possible joint cutting part
            //if over threshold, cut off joint part, and calculate each remaining pieces u length, and save the usable joint position on curve
            //move closed mesh from reused joint part as edge
            List<Point3d> centertest = new List<Point3d>();
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

                    //Make boundingbox for imported unstandard objects
                    Plane boxPlane = new Plane(centerCurve[b].PointAtStart, centerVector[b]);
                    BoundingBox jointBound = jm.GetBoundingBox(boxPlane);
                    //boundingBox to Brep
                    Brep jointBrep = Brep.CreateFromBox(jointBound);
                    Plane originplane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
                    Transform orient = Transform.PlaneToPlane(originplane, boxPlane);
                    jointBrep.Transform(orient);

                    foreach (BrepFace brepFace in jointBrep.Faces)
                    {
                        Point3d faceCenter = AreaMassProperties.Compute(brepFace.ToBrep()).Centroid;
                        centerCurve[b].ClosestPoint(faceCenter, out double t);
                        double distancedepth = faceCenter.DistanceTo(centerCurve[b].PointAt(t));
                        faceBrep.Add(brepFace);
                        faceDepth.Add(distancedepth);
                        //centertest.Add(faceCenter);
                    }

                    var brepAnddepth = faceDepth.Select((d, j) => new { Depth = d, Face = j }).OrderBy(x => x.Depth);
                    var closestBrepDepth = brepAnddepth.First();

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
                    centerCurve[b].ClosestPoint(closestBrepFace.PointAt(0.5, 0.5), out double pointOnCurveParam);
                    Plane jointPlane = new Plane(centerCurve[b].PointAt(pointOnCurveParam), closestBrepFace.NormalAt(0.5, 0.5));
                    planeList.Add(jointPlane);

                    //Joint UV Length
                    List<double> ulength = new List<double>();
                    List<double> vlength = new List<double>();
                    Brep furthestBrep = closestBrepFace.ToBrep();

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
                        Dictionary<int, List<int>> fitCondition = new Dictionary<int, List<int>>();
                        Dictionary<int, List<int>> unfitCondition = new Dictionary<int, List<int>>();
                        //if joint fits in any of the model joints
                        for (int uL = 0; uL < ulengthlist.Count; uL++)
                        {
                            List<int> fitRecNum;
                            List<int> unfitRecNum;
                            //compare to each joint in model element
                            for (int p = 0; p < element.Joint.Plane.Count; p++)
                            {
                                //if reclaimed joint size < to cut model joint size
                                if (element.Joint.uLength[p] < ulengthlist[uL] 
                                    && element.Joint.vLength[p] < vlengthlist[uL] 
                                    && element.Joint.Depth[p] < depthlist[uL])
                                {
                                    if (!fitCondition.TryGetValue(uL, out fitRecNum))
                                    {
                                        fitRecNum = new List<int>();
                                        fitCondition.Add(uL, fitRecNum);
                                    }
                                    fitRecNum.Add(p);
                                    //if(reclaimJointnum.Any(item => item == p) == false)
                                }
                                //else then trim off
                                else
                                {
                                    if (!unfitCondition.TryGetValue(uL, out unfitRecNum))
                                    {
                                        unfitRecNum = new List<int>();
                                        unfitCondition.Add(uL, unfitRecNum);
                                    }
                                    unfitRecNum.Add(p);
                                }
                            }
                        }
                        //arrange reclaimed timber position several conditions
                        //use model joint distance to end,and apply the distance to reclaimed joint to see how much material we are cutting off
                        //List<Plane> dupElJointPlane = new List<Plane>(element.Joint.Plane.Select(pl => pl.Clone()).ToList());
                        for (int i = 0; i < fitCondition.Count; i++)
                        {
                            if (fitCondition[i] != null)
                            {
                                //distance of model joint to end of model center curve
                                List<int> eleFitJoint = fitCondition[i];
                                double ModJointToCurveStartDist = planeList[i].Origin.DistanceTo(centerCurve[b].PointAtStart);
                                double ModJointToCurveEndDist = planeList[i].Origin.DistanceTo(centerCurve[b].PointAtEnd);
                                Vector3d RecJointToCurveStartVect = element.Centerline.First - planeList[i].Origin;
                                Vector3d RecJointToCurveEndVect = element.Centerline.Last - planeList[i].Origin;
                                RecJointToCurveStartVect.Unitize();
                                RecJointToCurveEndVect.Unitize();
                                
                                for (int fj = 0; fj < eleFitJoint.Count; fj++)
                                {
                                    Point3d RecJointOrigin = element.Joint.Plane[eleFitJoint[fj]].Origin;

                                    //Condition 1
                                    Transform moveStartCond1 = Transform.Translation(Vector3d.Multiply(RecJointToCurveStartVect, ModJointToCurveStartDist));
                                    Transform moveEndCond1 = Transform.Translation(Vector3d.Multiply(RecJointToCurveEndVect, ModJointToCurveEndDist));
                                    Point3d cond1StartRecJointOrigin = new Point3d(RecJointOrigin);
                                    Point3d cond1EndRecJointOrigin = new Point3d(RecJointOrigin);
                                    cond1StartRecJointOrigin.Transform(moveStartCond1);
                                    cond1EndRecJointOrigin.Transform(moveEndCond1);

                                    //test
                                    Point3d _test = cond1EndRecJointOrigin;
                                    Point3d _test2 = cond1StartRecJointOrigin;
                                    centertest.Add(_test);
                                    centertest.Add(_test2);

                                    //Flip the beam, Condition 2
                                    Transform moveStartCond2 = Transform.Translation(Vector3d.Multiply(RecJointToCurveStartVect, ModJointToCurveEndDist));
                                    Transform moveEndCond2 = Transform.Translation(Vector3d.Multiply(RecJointToCurveEndVect, ModJointToCurveStartDist));
                                    Point3d cond2StartRecJointOrigin = new Point3d(RecJointOrigin);
                                    Point3d cond2EndRecJointOrigin = new Point3d(RecJointOrigin);
                                    cond2StartRecJointOrigin.Transform(moveStartCond2);
                                    cond2EndRecJointOrigin.Transform(moveEndCond2);

                                    //if the length is enough then add joint combination group
                                    //there are two sides of length to compare
                                    //cut off the length that have: unfit joints and are over threshold, keep the length with joints that are under threshold
                                    //if the other joints happen to be in the same position as their matching joints then dont cut: distanceModelJointToJoint = distanceRecJointToJoint

                                    //Condition 1 point end distance to curve closest point
                                    double cond1DistStart = cond1StartRecJointOrigin.DistanceTo(element.Centerline.ClosestPoint(cond1StartRecJointOrigin));
                                    double cond1DistEnd = cond1EndRecJointOrigin.DistanceTo(element.Centerline.ClosestPoint(cond1EndRecJointOrigin));
                                    //Condition 2 point end distance
                                    double cond2DistStart = cond1StartRecJointOrigin.DistanceTo(element.Centerline.ClosestPoint(cond2StartRecJointOrigin));
                                    double cond2DistEnd = cond2EndRecJointOrigin.DistanceTo(element.Centerline.ClosestPoint(cond2EndRecJointOrigin));
                                    
                                    //the timber u length is enough, but there are joints in the way that affect the use of u length
                                    if (cond1DistStart < 0.03.FromMeter() && cond1DistEnd < 0.03.FromMeter())
                                    {
                                        ValidCondition validCondition = ValidCondition.Valid;
                                        //split curve into parts that joints should be deleted 
                                        double cond1StartParam = element.Centerline.ClosestParameter(cond1StartRecJointOrigin);
                                        double cond1EndParam = element.Centerline.ClosestParameter(cond1EndRecJointOrigin);

                                        List<double> jointParameter = new List<double>
                                            {
                                                cond1StartParam,
                                                cond1EndParam,
                                            };
                                        List<double> orderJointParam = jointParameter.OrderBy(num => num).ToList();
                                        List<int> paramKey = new List<int> { 0, 0, };
                                        List<double> jointUEnds = new List<double>();
                                        for (int pa = 0; pa < element.Joint.Plane.Count; pa++)
                                        {
                                            //order points by distance to curve start, if another joint is between condition start and end and is too deep, then eliminate option
                                            //if not too deep then keep option

                                            if(pa != eleFitJoint[fj])
                                            {
                                                //the u ends of the joint, see whether parameter inside start and end
                                                Point3d jointUlengthStart = new Point3d(element.Joint.Plane[pa].Origin);
                                                Transform moveJointUlengthStart = Transform.Translation(Vector3d.Multiply(RecJointToCurveStartVect, element.Joint.uLength[pa] / 2));
                                                jointUlengthStart.Transform(moveJointUlengthStart);
                                                Point3d jointUlengthEnd = new Point3d(element.Joint.Plane[pa].Origin);
                                                Transform moveJointUlengthEnd = Transform.Translation(Vector3d.Multiply(Vector3d.Negate(RecJointToCurveStartVect), element.Joint.uLength[pa] / 2));
                                                jointUlengthEnd.Transform(moveJointUlengthEnd);

                                                double ulengthStartParam = element.Centerline.ClosestParameter(jointUlengthStart);
                                                double ulengthEndParam = element.Centerline.ClosestParameter(jointUlengthEnd);
                                                jointUEnds.Add(ulengthStartParam);
                                                jointUEnds.Add(ulengthEndParam);

                                                //see if there is another joint inside the range that is affecting the use of the length
                                                for (int ue = 0; ue < jointUEnds.Count; ue++)
                                                {
                                                    if (jointUEnds[ue] > orderJointParam[0] 
                                                        && jointUEnds[ue] < orderJointParam[1] 
                                                        && element.Joint.Depth[pa] > depthThreshold) 
                                                    {
                                                        validCondition = ValidCondition.Invalid;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if(validCondition == ValidCondition.Valid)
                                        {
                                            Interval useDomain = new Interval(cond1StartParam, cond1EndParam);
                                            Polyline usePoly = element.Centerline.Trim(useDomain);
                                        }

                                    }
                                    if( cond2DistStart < 0.05.FromMeter() && cond2DistEnd < 0.05.FromMeter())
                                    {
                                        ValidCondition validCondition = ValidCondition.Valid;
                                        //split curve into parts that joints should be deleted 
                                        double cond2StartParam = element.Centerline.ClosestParameter(cond2StartRecJointOrigin);
                                        double cond2EndParam = element.Centerline.ClosestParameter(cond2EndRecJointOrigin);

                                        List<double> jointParameter = new List<double>
                                            {
                                                cond2StartParam,
                                                cond2EndParam,
                                            };
                                        List<double> orderJointParam = jointParameter.OrderBy(num => num).ToList();
                                        List<int> paramKey = new List<int> { 0, 0, };
                                        List<double> jointUEnds = new List<double>();
                                        for (int pa = 0; pa < element.Joint.Plane.Count; pa++)
                                        {
                                            //order points by distance to curve start, if another joint is between condition start and end and is too deep, then eliminate option
                                            //if not too deep then keep option

                                            if (pa != eleFitJoint[fj])
                                            {
                                                //the u ends of the joint, see whether parameter inside start and end
                                                Point3d jointUlengthStart = new Point3d(element.Joint.Plane[pa].Origin);
                                                Transform moveJointUlengthStart = Transform.Translation(Vector3d.Multiply(RecJointToCurveStartVect, element.Joint.uLength[pa] / 2));
                                                jointUlengthStart.Transform(moveJointUlengthStart);
                                                Point3d jointUlengthEnd = new Point3d(element.Joint.Plane[pa].Origin);
                                                Transform moveJointUlengthEnd = Transform.Translation(Vector3d.Multiply(Vector3d.Negate(RecJointToCurveStartVect), element.Joint.uLength[pa] / 2));
                                                jointUlengthEnd.Transform(moveJointUlengthEnd);

                                                double ulengthStartParam = element.Centerline.ClosestParameter(jointUlengthStart);
                                                double ulengthEndParam = element.Centerline.ClosestParameter(jointUlengthEnd);
                                                jointUEnds.Add(ulengthStartParam);
                                                jointUEnds.Add(ulengthEndParam);

                                                //see if there is another joint inside the range that is affecting the use of the length
                                                for (int ue = 0; ue < jointUEnds.Count; ue++)
                                                {
                                                    if (jointUEnds[ue] > orderJointParam[0]
                                                        && jointUEnds[ue] < orderJointParam[1]
                                                        && element.Joint.Depth[pa] > depthThreshold)
                                                    {
                                                        validCondition = ValidCondition.Invalid;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (validCondition == ValidCondition.Valid)
                                        {
                                            Interval useDomain = new Interval(cond2StartParam, cond2EndParam);
                                            Polyline usePoly = element.Centerline.Trim(useDomain);
                                        }
                                    }
                                }

                            }

                        }

                        //reclaimed timber without joints or all the joints are under threshold, then compare beam u,v,w to see if comparable
                        //last chance for joints: keep if joint depth is under threshold, not then cut away piece
                        for (int i = 0; i < element.Joint.Depth.Count; i++)
                        {
                            if (element.Joint.Depth[i] < depthThreshold)
                            {
                               
                            }
                        }
                    }
                }
            }
            DA.SetDataList(2, centerCurve);
            DA.SetDataList(3, centertest);
        }

        public enum ValidCondition
        {
            Valid,
            Invalid,
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