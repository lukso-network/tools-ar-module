using Assets.Demo.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Lukso.Skeleton;
//using Skeleton = Lukso.Skeleton;
using Joint = Lukso.Joint;

namespace Lukso{

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
            foreach (var j in joints) {
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
                if (j != null && j.definition != null) {
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
        public Skeleton Skeleton => skeleton;

        private Vector3?[] ikTarget;
        private Vector3?[] allTarget;
        private Quaternion headRotation = Quaternion.identity;

        public bool Destroyed { get; set; }

        private Transform[] ikSource;
        private Dictionary<String, Joint> transformByName = new Dictionary<string, Joint>();
        private Dictionary<Point, Joint> jointMap = new Dictionary<Point, Joint>();
        private Joint[] jointByPointId;

        public List<Joint> Joints { get => joints; }
        public float GradientThreshold;
        public bool useOld;
        //TODO debugging only
        public GradientDrawer gradientDrawer;

        public IkSettings settings;

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
            InitCloth();
            CopyRotationFromAvatar();


            gradientDrawer = GameObject.FindObjectOfType<GradientDrawer>();
        }
        private Joint GetHips() {
            return GetJointByPoint(Skeleton.Point.HIPS);
        }

        public float GetScale() {
            return GetHips().transform.localScale.x;
        }

        private Joint GetChest() {
            return GetJointByPoint(Skeleton.Point.CHEST);
        }

        public Joint GetSpine() {
            return GetJointByPoint(Skeleton.Point.SPINE);
        }
        private Joint GetJointByPoint(Skeleton.Point point) {

            if ((int)point >= 0) {
                return jointByPointId[(int)point];
            }
            Joint joint;
            if (jointMap.TryGetValue(point, out joint)) {
                return joint;
            }

            //Debug.LogError("Joint is not found:" + point);
            return null;
            //return transformByName[skeleton.GetBoneName(point)];
        }


        public void InitJoints() {
            joints = new List<Joint>();
            jointByPointId = new Joint[Skeleton.POINT_COUNT];
            int idx = 0;
            foreach (Transform t in avatar.GetComponentsInChildren<Transform>()) {
                if (idx++ == 0) {
                    continue;
                }
                var j = new Joint(t, skeleton?.GetByName(t.gameObject.name));
                j.boneNormalRotation = CalculateBoneNormalRotation(t);
                joints.Add(j);

                var jc = t.gameObject.GetComponent<JointController>();
                if (jc == null) {
                    jc = t.gameObject.AddComponent(typeof(JointController)) as JointController;
                }

                jc.joint = j;

                transformByName[t.gameObject.name] = j;
                transformByName[Utils.ReplaceSpace(t.gameObject.name)] = j;

                if (j.definition != null) {
                    jointMap[j.definition.point] = j;
                }

                if (j.definition != null && j.definition.pointId >= 0) {
                    jointByPointId[j.definition.pointId] = j;
                }
            }

            if (jointByPointId.All(x => x == null)) {
                Debug.LogError("Null joints");
            }

            initalSkeletonTransform.CopyFrom(this.joints);
        }

        private Quaternion CalculateBoneNormalRotation(Transform t) {
            var p = t.parent;
            if (p == null) {
                return Quaternion.identity;
            }

            var dir = (t.position - t.parent.position).normalized;
            var frontDir = Vector3.forward;

            var norm = Vector3.Cross(dir, frontDir);
            if (norm.y < 0) {
                norm = -norm;
            }


            var q = Quaternion.FromToRotation(dir, norm);

            var d = q * dir;
            return q;
        }

        public void CopyRotationFromAvatar(Avatar avatar) {
            if (avatar.joints.Count != joints.Count) {
                Debug.LogError("Incorrect joints count on copy avatar");
                return;
            }

            for (int i = 0; i < avatar.joints.Count; ++i) {
                joints[i].ApplyRotation(avatar.joints[i]);
            }
        }

        public void CopyRotationAndPositionFromAvatar(Avatar avatar) {
            if (avatar.joints.Count != joints.Count) {
                Debug.LogWarning("Incorrect joints count on copy avatar");
            }

            foreach (var entry in avatar.transformByName) {
                Joint j;


                //TODO check copy unnecessary like arm
                if (transformByName.TryGetValue(entry.Key, out j)) {
                    j.CopyRotationAndPosition(entry.Value);
                }
            }

            joints[0].CopyRotationAndPosition(avatar.joints[0]);
        }

        public void CopyToLocalFromGlobal(Avatar avatar, Vector3 scaleVector, bool resizeBones) {
            if (avatar.joints.Count != joints.Count) {
                // Debug.LogWarning("Incorrect joints count on copy avatar");
                //return;
            }

            Vector3 s;
            foreach (var entry in avatar.transformByName) {
                Joint j;
                if (transformByName.TryGetValue(entry.Key, out j)) {
                    j.CopyToLocalFromGlobal(entry.Value);
                    s = scaleVector;
                    //s = Vector3.Scale(s, entry.Value.clothScale);
                    s += entry.Value.clothScale;
                    if (resizeBones) {
                        s.y *= entry.Value.lengthScale;
                    }
                    j.transform.localScale = Vector3.Scale(j.transform.localScale, s);
                }
            }
            /*
            foreach (var entry in avatar.jointMap) {
                Joint j;
                if (jointMap.TryGetValue(entry.Key, out j)) {
                    j.CopyToLocalFromGlobal(entry.Value);
                    s = scaleVector;
                    //s = Vector3.Scale(s, entry.Value.clothScale);
                    s += entry.Value.clothScale;
                    if (resizeBones) {
                        s.y *= entry.Value.lengthScale;
                    }
                    j.transform.localScale = Vector3.Scale(j.transform.localScale, s);
                }
            }*/



            s = scaleVector;
            if (resizeBones) {
                s.y *= avatar.joints[0].lengthScale;
            }
            joints[0].CopyToLocalFromGlobal(avatar.joints[0]);
            joints[0].transform.localScale = Vector3.Scale(joints[0].transform.localScale, s);

            /*
            int i = 0;
            foreach (var j in joints) {
                j.CopyToLocalFromGlobal(avatar.joints[i]);
                ++i;
            }*/
        }

        /*  public void ScaleEveryJoint(Vector3 scaleVector) {

               foreach(var j in joints) {
                   j.transform.localScale = Vector3.Scale(j.transform.localScale, scaleVector);
               }

           }*/


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

        //public Joint GetByName(String name) {
        //return transformByName[name];
        //}




        private GameObject GetJoint(int point) {
            return jointByPointId[(int)point].transform.gameObject;
        }


        public void RestoreSkeleton() {
            initalSkeletonTransform.CopyTo(this.joints);
        }


        private float GetScaleBonesLength(Skeleton skeleton) {
            float l1 = 0;
            foreach (var bone in skeleton.ScaleBones) {
                var idx1 = bone.fromIdx;
                int idx2 = bone.toIdx;

                if (idx1 >= jointByPointId.Length || idx2 >= jointByPointId.Length) {
                    continue;
                }
                var v = (jointByPointId[idx1].transform.position - jointByPointId[idx2].transform.position);
                //v.z = 0;
                l1 += v.magnitude;
            }
            return l1;
        }


        public float GetRelativeBonesScale(Avatar avatar) {
            float l0 = GetScaleBonesLength(skeleton);
            // l0 = GetScaleBonesLength(skeleton);
            float l1 = avatar.GetScaleBonesLength(skeleton);

            return l1 / l0;

        }








    }

}
