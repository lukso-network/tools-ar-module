using Assets;
using Assets.Demo.Scripts;
using Assets.scripts.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Joint = Assets.Joint;

namespace Assets
{
    
    public class SkeletonTransform {
        internal Quaternion[] rots;
        internal Vector3[] pos;
        internal Vector3[] scales;

        public void CopyFrom(List<Joint> joints) {
            if (pos == null || pos.Length != joints.Count) {
                rots = new Quaternion[joints.Count];
                pos = new Vector3[joints.Count];
                scales = new Vector3[joints.Count];
            }
            int i = 0;
            foreach(var j in joints) {
                if (j != null) {
                    rots[i] = j.transform.localRotation;
                    pos[i] = j.transform.localPosition;
                    scales[i] = j.transform.localScale;
                }
                ++i;
            }
        }

        public void CopyTo(List<Joint> joints) {
            if (pos == null || pos.Length != joints.Count) {
                Debug.LogError("Incorrect joints count on copy from skeleton transformations");
                return;
            }
            int i = 0;
            foreach (var j in joints) {
                if (j != null) {
                    j.transform.localRotation = rots[i];
                    j.transform.localPosition = pos[i];
                    j.transform.localScale = scales[i];
                }
                ++i;
            }
        }
    }

    public partial class Avatar {

        private GameObject avatar;
        private List<Joint> joints;
        private List<Joint> calculatedJoints;

        private Skeleton skeleton;

        private Vector3?[] ikTarget;
        private Vector3?[] allTarget;


        private Transform[] affectedSource;
        private Vector3[] affectedTarget;

        private Transform[] ikSource;
        private Dictionary<String, Joint> transformByName = new Dictionary<string, Joint>();
        private Joint[] jointByPointId;
        private bool avatarXDirected;

        public List<Joint> Joints {get => joints; }
        public float GradientThreshold;
        public bool useOld;
        //TODO debugging only
        public GradientDrawer gradientDrawer;

        public IkSettings settings;
        private SkeletonTransform gradientSkeletonTransform = new SkeletonTransform();
        private SkeletonTransform initalSkeletonTransform = new SkeletonTransform();

        //alias
        public GameObject obj => avatar;
        
        private CalcParam[] parameters = new CalcParam[] {
            //    new CalcParam("Position", new CalcFilter(typeof(Position3DGradCalculator)), 0.001f, 1, 0.0001f),
            //    new CalcParam("Scaling", new CalcFilter(typeof(ScalingGradCalculator)), 0.0001f, 1, 0.0001f),
                new CalcParam("Rotation", new RotationFilter(), 0.1f, 500, 0.1f),
                new CalcParam("Stretching", new CalcFilter(typeof(StretchingGradCalculator)), 0.0001f, 1, 0.0001f),
        };


        public Avatar(GameObject avatar, Skeleton skeleton) {
            this.avatar = avatar;
            this.skeleton = skeleton;
            InitJoints();
            CopyRotationFromAvatar();

            gradientDrawer = GameObject.FindObjectOfType<GradientDrawer>();
        }

        public Joint GetHips() {
            return transformByName["Hips"];
        }
        public Joint GetChest() {
            return transformByName["Chest"];
        }

        private void InitJoints() {
            joints = new List<Joint>();
            const int MAX_POINT_COUNT = 33;
            jointByPointId = new Joint[MAX_POINT_COUNT];
            foreach (Transform t in avatar.GetComponentsInChildren<Transform>()) {
                var j = new Joint(t, skeleton?.GetByName(t.gameObject.name));
                joints.Add(j);

                var jc = t.gameObject.AddComponent(typeof(JointController)) as JointController;
                jc.joint = j;

                transformByName[t.gameObject.name] = j;
                transformByName[Utils.ReplaceSpace(t.gameObject.name)] = j;

                if (j.definition != null && j.definition.pointId > 0) {
                    jointByPointId[j.definition.pointId] = j;
                }
            }

            foreach (Transform t in avatar.GetComponentsInChildren<Transform>()) {

                var joint = transformByName[Utils.ReplaceSpace(t.gameObject.name)];

                var obj = t;
                while (true) {
                    obj = obj.parent;
                    if (obj == null) {
                        break;
                    } else {
                        Joint parentJoint;
                        if (transformByName.TryGetValue(Utils.ReplaceSpace(obj.gameObject.name), out parentJoint) && parentJoint.definition != null) {// && parentJoint.definition.pointId >= 0) {
                            
                            if (parentJoint.definition.pointId == -28 || parentJoint.definition.pointId == -27) {
                                continue;
                            }
                            joint.parent = parentJoint;
                            break;
                        }
                    }
                }
            }

            initalSkeletonTransform.CopyFrom(this.joints);

            avatarXDirected = GetHips().transform.localEulerAngles.x < 45;

        }

        public void CopyRotationFromAvatar(Avatar avatar) {
            if (avatar.joints.Count != joints.Count) {
                Debug.LogError("Incorrect joints count on copy avatar");
                return;
            }

            for (int i = 0; i < avatar.joints.Count; ++i ) {
                joints[i].ApplyRotation(avatar.joints[i]);
            }
        }

        public void CopyRotationAndPositionFromAvatar(Avatar avatar) {
            if (avatar.joints.Count != joints.Count) {
                Debug.LogWarning("Incorrect joints count on copy avatar");
            }

            foreach(var entry in avatar.transformByName) {
                Joint j;
                if (transformByName.TryGetValue(entry.Key, out j)) {
                    j.CopyRotationAndPosition(entry.Value);
                }

                joints[0].CopyRotationAndPosition(avatar.joints[0]);
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

        public void SetIkSource() {
            this.ikSource = skeleton.GetKeyBones().Select(x => x.transform).ToArray();

            this.joints.ForEach(x => x.gradEnabled = x.definition?.gradCalculator != null);
            //this.joints.ForEach(x => x.gradEnabled = x.transform.name == "Hips");

            this.joints.ForEach(j => j.transform.GetComponent<JointController>().gradientEnabled = j.gradEnabled);

            calculatedJoints = this.joints.Where(x => x.gradEnabled).ToList();

            Debug.Log("Enabled gradient:" + calculatedJoints.Count + " of " + joints.Count);
            foreach(var j in calculatedJoints) {
                Debug.Log("Grad: " + j.transform.gameObject.name + " ");
            }
        }

        public void CopyRotationFromAvatar() {
            CopyRotationFromAvatar(this);
        }
        public void ResetToObject() {
            foreach (var j in joints) {
                j.Reset();
            }
        }

        public List<Joint> GetAllJoints() {
            return joints;
        }

        public Joint GetByName(String name) {
            return transformByName[name];
        }

        public void FindGradients(List<Joint> enabledJoints, ICalcFilter calcFilter, float step, float zeroLevel= -1) {
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

        public float TargetFunctionOld() {
            double s = 0;
            var ln = Math.Min(ikSource.Length, ikTarget.Length);
            for (int i = 0; i < ln; ++i) {
                var p1 = ikSource[i].transform.position;
                var p2 = ikTarget[i];
                if (p2 != null) {
                    s += (p1 - p2).Value.sqrMagnitude;
                }
            }

            /*foreach(var j in joints) {
                s += Dist(j.transform.position, j.initPosition)*0.1f;
            }*/
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


        void KeepJoints(List<Joint> enabledJoints) {
            gradientSkeletonTransform.CopyFrom(enabledJoints);
        }

        void RestoreJoints(List<Joint> enabledJoints) {
            gradientSkeletonTransform.CopyTo(enabledJoints);

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
                if (-minValDistance < delta && delta < minValDistance ) {
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

        private void MoveHipsToCenter() {
            var hips = GetHips();

            var left = allTarget[(int)Skeleton.Point.LEFT_HIP].Value;
            var right = allTarget[(int)Skeleton.Point.RIGHT_HIP].Value;

            var leftArm = allTarget[(int)Skeleton.Point.LEFT_SHOULDER].Value;
            var rightArm = allTarget[(int)Skeleton.Point.RIGHT_SHOULDER].Value;


            var center = (left + right) / 2;
            var hipLen = (left - right).magnitude;

            var dir = ((leftArm + rightArm) / 2 - center).normalized;

            hips.transform.position = center + dir * hipLen * 0.2f;

            if (avatarXDirected) {
                hips.transform.rotation = Quaternion.LookRotation(dir, (right - left).normalized);
            } else {
                var forward = Vector3.Cross((right - left).normalized, dir);
                hips.transform.rotation = Quaternion.LookRotation(forward, dir);
            }
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
                   foreach(var p in parameters) {
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

            foreach (var bone in skeleton.ScaleBones) { 
                var j = skeleton.GetJoint(bone.fromIdx);
                var c = skeleton.GetJoint(bone.toIdx);
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
                    c.transform.position = pt;
                }
            }
          
        }


        public void UpdateFastBySteps(float gradStep, float moveStep, int steps) {

            initalSkeletonTransform.CopyTo(this.joints);

            MoveHipsToCenter();
            ScaleHips();

            var chest = GetChest();
            var hips = GetHips();

            foreach(var j in calculatedJoints) {
                if (settings.chestOnly && (j != chest)) {// && j != hips)) {
                    continue;
                }
                if (j.definition.AffectedPoints != null) {
                    var enabledJoints = new List<Joint>() { j };
                    affectedTarget = (from z in j.definition.AffectedPoints select allTarget[z].Value).ToArray();
                    affectedSource = (from z in j.definition.AffectedPoints select skeleton.GetJoint(z).transform).ToArray();

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

        private void ScaleHips() {
            float l1,l2;
            l1 = l2 = 0;
            foreach (var bone in skeleton.ScaleBones) {
                int idx1 = bone.fromIdx;
                int idx2 = bone.toIdx;

                l1 += (skeleton.GetJoint(idx1).transform.position - skeleton.GetJoint(idx2).transform.position).magnitude;
                l2 += (allTarget[idx1].Value - allTarget[idx2].Value).magnitude;
            }


            float scale = l2 / l1;
            var hips = GetHips();
            hips.transform.localScale = hips.transform.localScale * scale;
        }


        public void Update(float gradStep, float moveStep, int steps) {
            /*if (ikTarget.Length > 0) {
                UpdateBones();
            }

            return;
            */

            if (ikTarget.Length > 0) {
                UpdateFastBySteps(gradStep, moveStep, steps);
            }

            foreach(var j in calculatedJoints) {
                // j.Filter();
            }

            return;
        }

    }

}
