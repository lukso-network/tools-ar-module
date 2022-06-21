using Assets.scripts.Avatar;
using Lukso;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Lukso.Skeleton;

namespace Assets
{
  public partial class Avatar
  {

    private SkeletonTransform gradientSkeletonTransform = new SkeletonTransform();
    private SkeletonTransform initalSkeletonTransform = new SkeletonTransform();
    private SkeletonTransform clothSkeletonTransform = new SkeletonTransform();

    private List<PointParameter> ikCalcualationParameters = new List<PointParameter>();
    
    private Transform[] affectedSource;
    private Vector3[] affectedTarget;

    private OneEuroFilter3D[] posFilters = new OneEuroFilter3D[] { };

    private Dictionary<Point, Joint[]> dependendJoints = new Dictionary<Point, Joint[]>();

    public void Update(float gradStep, float moveStep, int steps) {
      /*if (ikTarget.Length > 0) {
          UpdateBones();
      }

      return;
      */

      if (ikTarget.Length > 0) {
        UpdateFastBySteps(gradStep, moveStep, steps);
      }

      //TODO CHECK performance
      clothSkeletonTransform.CopyFrom(clothJoints);
      ApplyClothShift(true);

      foreach (var j in calculatedJoints) {
        // j.Filter();
      }

      return;
    }

    public void UpdateFastBySteps2(float gradStep, float moveStep, int steps) {
      initalSkeletonTransform.CopyTo(this.joints);

      MoveHipsToCenter();
      ScaleHips();


      var chest = GetChest();
      var hips = GetHips();
      var spine = GetSpine();
      
      foreach (var j in calculatedJoints) {
        if (settings.chestOnly && (j != chest) && (j != hips)) {// && (j !=hips)){// {// && j != hips)) {
          continue;
        }
        if (j.definition.AffectedPoints != null) {
          var enabledJoints = new List<Joint>() { j };
          affectedTarget = (from z in j.definition.AffectedPoints select allTarget[z].Value).ToArray();
          affectedSource = (from z in j.definition.AffectedPoints select GetJoint(z).transform).ToArray();

          foreach (var p in parameters) {
            p.calculated = false;
          }

          try {
            for (int i = 0; i < steps; ++i) {
              foreach (var p in parameters) {
                if (!p.calculated) {
                  var res = SolveIk(enabledJoints, p.calcFilter, p.gradStep * settings.gradientCalcStep, ref p.moveStep, p.minStep);
                  if (!res) {
                    p.calculated = true;
                  }
                }
              }
            }
          } catch (Exception e) {
            Debug.LogError("Exception on gradient: " + e.Message);
          }
        }

      }
      

      if (settings.enableAttaching) {
        PullAttachJoints();
      }

    }


    private bool manualMode = false;
    private bool DebugManualIK() {
      int count = Math.Min(ikCalcualationParameters.Count, settings.parametersTest.Length);
      if (!settings.manualIk) {
        manualMode = false;
        for (int i = 0; i < count; ++i) {
          settings.parametersTest[i] = ikCalcualationParameters[i].Get();
        }
        return false;
      }

      manualMode = true;

      for (int i = 0; i < count; ++i) {
        ikCalcualationParameters[i].Set(settings.parametersTest[i]);
      }
      return true;

    }


    //TODO
    // 1 priority queue
    // 2 calculate step for every parameter (more difference, more speed)
    // 3 keep change of every parameter if small then ignore several times
    public void UpdateFastBySteps(float gradStep, float moveStep, int steps) {

      if (settings.useOldIk) {
        UpdateFastBySteps2(gradStep, moveStep, steps);
        return;
      }

      initalSkeletonTransform.CopyTo(this.joints);

      MoveHipsToCenter();
      ScaleHips();

      if (ikCalcualationParameters.Count == 0) {
        InitIK();
      }

      foreach (var ik in skeleton.ikCalculator) {
        if (ik.ReinitAlways) {
          Joint joint = GetJointByPoint(ik.point);
          ik.Init(joint);
        }
      }


        //TOOD temp
        foreach (var par in ikCalcualationParameters) {
        var currentJoint = GetJointByPoint(par.AssignedObj.point);
        par.AssignedObj.Apply(currentJoint, 1);
      }

      if (DebugManualIK()) {
        return;
      }

        //affectedTarget = (from z in j.definition.AffectedPoints select allTarget[z].Value).ToArray();
        //affectedSource = (from z in j.definition.AffectedPoints select GetJoint(z).transform).ToArray();

        var chest = GetChest();
      var hips = GetHips();
      var spine = GetSpine();

      
      float dx = settings.gradDescentStep;
      float regularization = settings.ikRegularization;
      float lambda = settings.ikGradDescentLambda;
      const float EARLY_STOP_THRESHOLD = 1 + 0.0000001f;

      int EARLY_STOP_COUNT = ikCalcualationParameters.Count * 2;
      int unchangeCount = 0;

      for (int step = 0; step < 300; ++step) {
        //int idx = rnd.Next(0, parameters.Count);
        int idx = step % ikCalcualationParameters.Count;
        var par = ikCalcualationParameters[idx];

        var dependent = dependendJoints[par.AssignedObj.point];
        var currentJoint = GetJointByPoint(par.AssignedObj.point);


        // calculate gradient
        var value = TargetFunction(dependent);
        float prevVal = value;
        var oldX = par.Get();
        par.Set(oldX + dx);

        par.AssignedObj.Apply(currentJoint, 1);
        //continue;

        var newValue = TargetFunction(dependent);
        var grad = (newValue - value) / dx;

        var temp = lambda;
        
        var tryCount = 3;
        var found = false;
        for (int k = 0; k < tryCount; ++k) {

          par.Set(oldX * (1 - temp * regularization) - temp * grad);
          par.AssignedObj.Apply(currentJoint, 1);

          var value2 = TargetFunction(dependent);
         // found = true;
          //break;
          if (value2 <= value) {
            value = value2;
            found = true;
            break;
          }

          temp /= 4;
        }

        if (!found) {
          par.Set(oldX);
          par.AssignedObj.Apply(currentJoint, 1);
        }


        if (value / prevVal < EARLY_STOP_THRESHOLD) {
          unchangeCount += 1;
          if (unchangeCount > EARLY_STOP_COUNT) {
         //   Debug.Log($"EARLY_STOP: iter={step}");
            break;
          }

        } else {
          unchangeCount = 0;
        }

      }


      if (settings.enablePostIKSmoothing) {
        filterPoints();
      }

      if (settings.enableAttaching) {
       PullAttachJoints();
      }


    }

    private void InitSkeletonFilters() {
      posFilters = new OneEuroFilter3D[skeleton.filterPoints.Length];
      for(int i = 0; i<posFilters.Length; ++i) {
        posFilters[i] = new OneEuroFilter3D(settings.filterPosSmoothingParams);
      }
    }

    private void filterPoints() {
      int i = 0;
      var timestamp = Time.realtimeSinceStartup;

      var hips = GetHips();
      var scale = 1;// hips.transform.localScale.x;
      foreach (var p in skeleton.filterPoints) {
        var j = GetJointByPoint(p);
        var pos = j.transform.position;
        pos = posFilters[i++].Filter(pos, timestamp);
        j.transform.position = pos;
      }
    }

    private void MoveHipsToCenter() {
      var hips = GetHips();

      var left = allTarget[(int)Skeleton.Point.LEFT_HIP].Value;
      var right = allTarget[(int)Skeleton.Point.RIGHT_HIP].Value;

      var leftArm = allTarget[(int)Skeleton.Point.LEFT_SHOULDER].Value;
      var rightArm = allTarget[(int)Skeleton.Point.RIGHT_SHOULDER].Value;


      var center = (left + right) / 2;
      var hipLen = (left - right).magnitude;
      var updir = ((leftArm + rightArm) / 2 - center).normalized;

      hips.transform.position = center + updir * hipLen * 0.2f;

      var forward = Vector3.Cross((right - left).normalized, updir);
      var modelForward = Vector3.forward;
      var modelUp = Vector3.up;

      var q1 = Quaternion.FromToRotation(modelUp, updir);
      var newForward = q1 * modelForward;

      Quaternion q2;
      if (Vector3.Dot(forward, newForward) < -0.95f) {
        q2 = Quaternion.AngleAxis(180, updir);
      } else {
        q2 = Quaternion.FromToRotation(newForward, forward);
      }
      hips.transform.rotation = q2 * q1 * hips.transform.rotation;
    }

    public void UpdateFast(float gradStep, float moveStep, int steps) {
      var constraints = settings.useConstraints;

      if (settings.enableMoveHips) {
        MoveHipsToCenter();
      }

      foreach (var p in parameters) {
        p.calculated = false;
      }

      var enabledJoints = calculatedJoints;

      try {
        for (int i = 0; i < steps; ++i) {
          foreach (var p in parameters) {
            if (!p.calculated) {
              var res = SolveIk(enabledJoints, p.calcFilter, p.gradStep * settings.gradientCalcStep, ref p.moveStep, p.minStep);
              if (!res) {
                p.calculated = true;
              }
            }
          }
        }
      } catch (Exception e) {
        Debug.LogError("Exception on gradient: " + e.Message);
      }
    }


    private void PullAttachJoints() {
      //TODO
      float[] size = new float[skeleton.ScaleBones.Count];

      int i = 0;
      foreach (var bone in skeleton.ScaleBones) {
        var j = GetJoint(bone.fromIdx);
        var c = GetJoint(bone.toIdx);
        size[i] = (j.transform.position - c.transform.position).magnitude;
        ++i;
      }

      i = 0;
      foreach (var bone in skeleton.ScaleBones) {
        var j = GetJoint(bone.fromIdx);
        var c = GetJoint(bone.toIdx);
        if (settings.enableAttaching) {
          // set parent position first
          j.transform.position = allTarget[bone.fromIdx].Value; ;
        }

        var pt = allTarget[bone.toIdx].Value;
        var v1 = (c.transform.position - j.transform.position).normalized;
        var v2 = (pt - j.transform.position).normalized;
        var rot = Quaternion.FromToRotation(v1, v2);
        j.transform.rotation = rot * j.transform.rotation;

        if (settings.enableAttaching) {
          var l2 = (j.transform.position - pt).magnitude;
          var s = j.transform.localScale;
          s.y *= l2 / size[i];

          jointByPointId[bone.fromIdx].lengthScale = l2 / size[i];
          // j.lenghtScale = l2 / size[i];

          c.transform.position = pt;
        }

        ++i;
      }
    }


    private bool SolveIk(List<Joint> enabledJoints, ICalcFilter calcFilter, float gradStep, ref float moveStep, float minStep, float minValDistance = 1e-5f) {

      var zeroLevel = TargetFunction();

      KeepJoints(enabledJoints);
      FindGradients(enabledJoints, calcFilter, gradStep, zeroLevel);
      RestoreJoints(enabledJoints);

      var maxGrad = FindMaxGradient(enabledJoints, calcFilter);
      int moved = 0;
      var val = zeroLevel;
      var stopCalc = false;
      //for (int j = 0; j < 30 && moveStep > minStep; ++j) {
      for (int j = 0; j < 10; ++j) {
        KeepJoints(enabledJoints);
        MoveByGradients(enabledJoints, calcFilter, moveStep);

        var maxDelta = maxGrad * moveStep;
        if (maxGrad == 0) {
          RestoreJoints(enabledJoints);
          return false;
        }

        if (maxDelta < minStep) {
          RestoreJoints(enabledJoints);
          if (moveStep > 0) {
            moveStep = moveStep / settings.gradStepScale;
          }
          break;
        }

        var cur = TargetFunction();
        var delta = cur - val;
        if (-minValDistance < delta && delta < minValDistance) {
          RestoreJoints(enabledJoints);
          return false;
        } else if (cur > val) {
          RestoreJoints(enabledJoints);
          if (moved != 0) {
            if (moved > 1) {
              moveStep /= settings.gradStepScale;
            }
            break;
          }

          moveStep *= settings.gradStepScale;
        } else {
          val = cur;
          moved++;

          if (moved % 2 == 0) {
            //  moveStep /= settings.gradStepScale;
          }
        }
      }

      // moveStep /= settings.gradStepScale;
      return true;

    }




    private void ScaleHips() {
      float l1 = GetScaleBonesLength(skeleton);
      float l2 = GetTargetBonesLength(skeleton);

      float scale = l2 / l1;
      var hips = GetHips();
      hips.transform.localScale = hips.transform.localScale * scale;
    }

    private float GetTargetBonesLength(Skeleton skeleton) {
      float l1 = 0;
      foreach (var bone in skeleton.ScaleBones) {
        int idx1 = bone.fromIdx;
        int idx2 = bone.toIdx;

        if (idx1 >= jointByPointId.Length || idx2 >= jointByPointId.Length) {
          continue;
        }

        l1 += (allTarget[idx1].Value - allTarget[idx2].Value).magnitude;
      }
      return l1;
    }


    void KeepJoints(List<Joint> enabledJoints) {
      gradientSkeletonTransform.CopyFrom(enabledJoints);
    }

    void RestoreJoints(List<Joint> enabledJoints) {
      gradientSkeletonTransform.CopyTo(enabledJoints);

    }


    public float TargetFunction2D() {
      float s = 0;
      var ln = Math.Min(ikSource.Length, ikTarget.Length);
      for (int i = 0; i < ln; ++i) {
        var p1 = ikSource[i].transform.position;
        var p2 = ikTarget[i];
        if (p2 != null) {
          s += Dist(p1, p2.Value);
        }
      }

      /*foreach(var j in joints) {
          s += Dist(j.transform.position, j.initPosition)*0.1f;
      }*/
      return Mathf.Sqrt(s);
    }

    /* public float TargetFunctionOld() {
       double s = 0;
       var ln = Math.Min(ikSource.Length, ikTarget.Length);
       for (int i = 0; i < ln; ++i) {
         var p1 = ikSource[i].transform.position;
         var p2 = ikTarget[i];
         if (p2 != null) {
           s += (p1 - p2).Value.sqrMagnitude;
         }
       }

       //*foreach(var j in joints) {
         //  s += Dist(j.transform.position, j.initPosition)*0.1f;
       //}
       return (float)Math.Sqrt(s);
     }*/

    public float TargetFunction(Joint[] testJoints) {
      double s = 0;
      foreach (var j in testJoints) {
        var p1 = j.transform.position;
        var p2 = allTarget[j.definition.pointId];
        if (p2 != null) {
          var dp = p1 - p2.Value;
          s += dp.x * dp.x + dp.y * dp.y + dp.z * dp.z * settings.targetFunctionZScale;
        }
      }
      //return (float)s;
      return (float)Math.Sqrt(s);
    }

    public float TargetFunction() {
      double s = 0;
      var ln = affectedSource.Length;
      for (int i = 0; i < ln; ++i) {
        var p1 = affectedSource[i].transform.position;
        var p2 = affectedTarget[i];
        if (p2 != null) {
          s += (p1 - p2).sqrMagnitude;
        }
      }

      /*foreach(var j in joints) {
          s += Dist(j.transform.position, j.initPosition)*0.1f;
      }*/
      return (float)Math.Sqrt(s);
    }

    public float[] DiffPos() {
      double s = 0;
      var ln = Math.Min(ikSource.Length, ikTarget.Length);

      var res = new float[ln];
      for (int i = 0; i < ln; ++i) {
        var p1 = ikSource[i].transform.position;
        var p2 = ikTarget[i];
        if (p2 != null) {
          res[i] = (p1 - p2).Value.magnitude;
        }
      }

      float mx = res.Max();
      return res;// res.Select(x => x / mx).ToArray();

    }



    public void FindGradients(List<Joint> enabledJoints, ICalcFilter calcFilter, float step, float zeroLevel = -1) {
      zeroLevel = zeroLevel > 0 ? zeroLevel : TargetFunction();
      foreach (var j in enabledJoints) {
        zeroLevel = j.CalcGradients(calcFilter, zeroLevel, TargetFunction, step, step * settings.posMoveMultiplier, GradientThreshold, settings);
      }
    }
    private float FindMaxGradient(List<Joint> enabledJoints, ICalcFilter calcFilter) {
      float maxRotGrad = 0;
      foreach (var j in enabledJoints) {
        maxRotGrad = Mathf.Max(maxRotGrad, j.FindMaxGradient(calcFilter));
      }
      return maxRotGrad;
    }

    private void MoveByGradients(List<Joint> enabledJoints, ICalcFilter calcFilter, float step) {
      foreach (var j in enabledJoints) {
        j.MoveByGradients(calcFilter, step, -1, -1, settings);
      }
    }

    private float Dist(Vector3 p1, Vector3 p2) {
      return (p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y);
    }


    private void InitIK() {
      ikCalcualationParameters.Clear();
      foreach(var ik in skeleton.ikCalculator) {
        ikCalcualationParameters.AddRange(ik.GetParameters());

        Joint joint = GetJointByPoint(ik.point);
        ik.Init(joint);
      }

      foreach (var j in joints) {
        if (j.definition != null && j.definition.AffectedPoints != null) {
          var dependendIds = j.definition.AffectedPoints;
          var depJoints = (from z in j.definition.AffectedPoints select GetJointByPoint((Point)z)).ToArray();

          dependendJoints[j.definition.point] = depJoints;
        }
      }


    }

    public void SetIkSource() {
      InitSkeletonFilters();
      this.ikSource = skeleton.GetkeyPointIds().Select(id => jointByPointId[id].transform).ToArray();

      //InitIK();
    

      this.joints.ForEach(x => x.gradEnabled = x.definition?.gradCalculator != null);
      //this.joints.ForEach(x => x.gradEnabled = x.transform.name == "Hips");

      this.joints.ForEach(j => j.transform.GetComponent<JointController>().gradientEnabled = j.gradEnabled);

      calculatedJoints = this.joints.Where(x => x.gradEnabled).ToList();

      Debug.Log("Enabled gradient:" + calculatedJoints.Count + " of " + joints.Count);
      foreach (var j in calculatedJoints) {
        Debug.Log("Grad: " + j.transform.gameObject.name + " ");
      }


    }


    public void SetIkTarget(Vector3?[] target) {
      //this.ikTarget = target.Where((p, i) => skeleton.HasKeyPoint(i)).ToArray();
      this.ikTarget = skeleton.FilterKeyPoints(target);
      this.allTarget = target;
    }

    private int GetIndexInSourceList(GameObject obj) {

      for (int i = 0; i < ikSource.Length; ++i) {
        if (ikSource[i].transform.gameObject == obj) {
          return i;
        }
      }
      return -1;
    }


  }
}
