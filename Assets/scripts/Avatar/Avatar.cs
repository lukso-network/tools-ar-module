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

    public partial class Avatar {
        private GameObject avatar;
        private List<Joint> joints;
        private List<Joint> enabledJoints;

        private Skeleton skeleton;

        private Vector3?[] ikTarget;
        private Transform[] ikSource;
        private int leftHipSourceIndex;
        private int rightHipSourceIndex;
        private Dictionary<String, Joint> transformByName = new Dictionary<string, Joint>();
        private Joint[] jointByPointId;

        public List<Joint> Joints {get => joints; }
        public float GradientThreshold;
        public bool useOld;
        //TODO debugging only
        public GradientDrawer gradientDrawer;

        public IkSettings settings;
        private Quaternion[] rots;
        private Vector3[] pos;
        private Vector3[] scales;

        //alias
        public GameObject obj => avatar;
        
        private CalcParam[] parameters = new CalcParam[] {
                new CalcParam("Position", new CalcFilter(typeof(Position3DGradCalculator)), 0.001f, 1, 0.0001f),
                new CalcParam("Scaling", new CalcFilter(typeof(ScalingGradCalculator)), 0.0001f, 1, 0.0001f),
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
                var p = t.parent;
                if (p) {
                    var joint = transformByName[Utils.ReplaceSpace(t.gameObject.name)];
                    Joint parentJoint;
                    if (transformByName.TryGetValue(Utils.ReplaceSpace(p.gameObject.name), out parentJoint)) {
                        joint.parent = parentJoint;
                    }

                }
            }

          //  var hip = transformByName["Hips"];
           // hip.gradCalculator = new GeneralGradCalculator(new Position3DGradCalculator(), new RotationGradCalculator(), new ScalingGradCalculator());
           // hip.gradCalculator =  new RotationGradCalculator();
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

            leftHipSourceIndex = GetIndexInSourceList(skeleton.GetLeftHips());
            rightHipSourceIndex = GetIndexInSourceList(skeleton.GetRightHips());


            //this.joints.ForEach(x => x.gradEnabled = false);

            this.joints.ForEach(x => x.gradEnabled = x.definition?.gradCalculator != null);
            //this.joints.ForEach(x => x.gradEnabled = x.transform.name == "Hips");

            this.joints.ForEach(j => j.transform.GetComponent<JointController>().gradientEnabled = j.gradEnabled);
            /*
            foreach (var t in ikSource) {
                var j = transformByName[Utils.ReplaceSpace(t.gameObject.name)];

				//TODO < 10
                while (j != null && !j.gradEnabled && j.parent != null && j.transform.localScale.x < 10 ) {
                    j.gradEnabled = j.definition?.gradCalculator != null;
                    j = j.parent;
                }
            }*/

            enabledJoints = this.joints.Where(x => x.gradEnabled).ToList();

            rots = new Quaternion[joints.Count];
            pos = new Vector3[joints.Count];
            scales = new Vector3[joints.Count];

            Debug.Log("Enabled gradient:" + enabledJoints.Count + " of " + joints.Count);
            foreach(var j in enabledJoints) {
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

        public void FindGradients(ICalcFilter calcFilter, float step, float zeroLevel= -1) {
            zeroLevel = zeroLevel > 0 ? zeroLevel : TargetFunction();
            foreach (var j in enabledJoints) {
                zeroLevel = j.CalcGradients(calcFilter, zeroLevel, TargetFunction, step, step * settings.posMoveMultiplier, GradientThreshold, settings);
            }
        }
        private float FindMaxGradient(ICalcFilter calcFilter) {
            float maxRotGrad = 0;
            foreach (var j in enabledJoints) {
                maxRotGrad = Mathf.Max(maxRotGrad, j.FindMaxGradient(calcFilter));
            }
            return maxRotGrad;
        }

        private void MoveByGradients(ICalcFilter calcFilter, float step) {
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

        public float TargetFunction() {
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


        void KeepJoints(Quaternion[] rotations, Vector3[] pos, Vector3[] scales) {
            for (int i = 0; i < enabledJoints.Count; ++i) {
                var j = enabledJoints[i];
                rotations[i] = j.transform.localRotation;
                pos[i] = j.transform.localPosition;
                scales[i] = j.transform.localScale;
            }
        }

        void RestoreJoints(Quaternion[] rotations, Vector3[] pos, Vector3[] scales) {
            for (int i = 0; i < enabledJoints.Count; ++i) {
                var j = enabledJoints[i];
                j.transform.localRotation = rotations[i];
                j.transform.localPosition = pos[i];
                j.transform.localScale = scales[i];
            }
        }

        private bool SolveIk(ICalcFilter calcFilter, float gradStep, ref float moveStep, float minStep, float minValDistance = 1e-5f) {

            var zeroLevel = TargetFunction();
            KeepJoints(rots, pos, scales);
            FindGradients(calcFilter, gradStep, zeroLevel);
            RestoreJoints(rots, pos, scales);

            var maxGrad = FindMaxGradient(calcFilter);
            int moved = 0;
            var val = zeroLevel;
            var stopCalc = false;
            //for (int j = 0; j < 30 && moveStep > minStep; ++j) {
            for (int j = 0; j < 30; ++j) {
                KeepJoints(rots, pos, scales);
                MoveByGradients(calcFilter, moveStep);

                var maxDelta = maxGrad * moveStep;
                if (maxGrad == 0) {
                    RestoreJoints(rots, pos, scales);
                    return false;
                }

                if (maxDelta < minStep) {
                    RestoreJoints(rots, pos, scales);
                    if (moveStep > 0) {
                        moveStep = moveStep / settings.gradStepScale;
                    }
                    break;
                }

                var cur = TargetFunction();
                var delta = cur - val;
                if (-minValDistance < delta && delta < minValDistance ) {
                    RestoreJoints(rots, pos, scales);
                    return false;
                } else if (cur > val) {
                    RestoreJoints(rots, pos, scales);
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
          //  return;
            var hips = GetHips();

            if (leftHipSourceIndex < 0 || rightHipSourceIndex < 0) {
                return;
            }
            var left = ikTarget[leftHipSourceIndex].Value;
            var right = ikTarget[rightHipSourceIndex].Value;

            var center = (left + right) / 2;
            var c = hips.transform.position;
            c.x = center.x;
            c.z = center.z;
            c.y = center.y;

            hips.transform.position = c;
        }

        public void UpdateFast(float gradStep, float moveStep, int steps) {
            var constraints = settings.useConstraints;

            if (settings.enableMoveHips) {
                MoveHipsToCenter();
            }

            foreach (var p in parameters) {
                p.calculated = false;
            }

            try {
                for (int i = 0; i < steps; ++i) {
                   foreach(var p in parameters) {
                        if (!p.calculated) {
                            var res = SolveIk(p.calcFilter, p.gradStep * settings.gradientCalcStep, ref p.moveStep, p.minStep);
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
    
        public void Update(float gradStep, float moveStep, int steps) {
            if (ikTarget.Length > 0) {
                UpdateFast(gradStep, moveStep, steps);
            }

            foreach(var j in enabledJoints) {
                j.Filter();
            }

            return;
        }

    }

}
