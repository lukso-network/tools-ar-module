

using Assets;
using Assets.Demo.Scripts;
using Assets.PoseEstimator;
using DeepMotion.DMBTDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Joint = Assets.Joint;


public class FPSCounter 
{
    const float fpsMeasurePeriod = 0.5f;
    private int counter = 0;
    private float lastTime = 0;
    private float fps;

    public float UpdateFps() {
        counter++;
        float t = Time.realtimeSinceStartup;
        if (t > lastTime + fpsMeasurePeriod) {
            fps = counter / (t - lastTime);
            counter = 0;
            lastTime = t;
        }
        return fps;
    }
}

public class Skeleton
{
    public const int JOINT_COUNT = 33;
    public class Bone
    {
        public readonly int fromIdx;
        public readonly int toIdx;

        public Bone(int fromIdx, int toIdx) {
            this.fromIdx = fromIdx;
            this.toIdx = toIdx;
        }
    }

    public List<JointDefinition> joints = new List<JointDefinition>();
    private GameObject[] jointBones;
    private int[] keyPointsIds;

    public List<Bone> ScaleBones;

    public enum Point:int
    {
        CHEST = -3,
        SPINE = -2,
        HIPS = -1,

        NOSE = 0,
        LEFT_EYE_INNER = 1,
        LEFT_EYE = 2,
        LEFT_EYE_OUTER = 3,
        RIGHT_EYE_INNER = 4,
        RIGHT_EYE = 5,
        RIGHT_EYE_OUTER = 6,
        LEFT_EAR = 7,
        RIGHT_EAR = 8,
        MOUTH_LEFT = 9,
        MOUTH_RIGHT = 10,
        LEFT_SHOULDER = 11,
        RIGHT_SHOULDER = 12,
        LEFT_ELBOW = 13,
        RIGHT_ELBOW = 14,
        LEFT_WRIST = 15,
        RIGHT_WRIST = 16,
        LEFT_PINKY = 17,
        RIGHT_PINKY = 18,
        LEFT_INDEX = 19,
        RIGHT_INDEX = 20,
        LEFT_THUMB = 21,
        RIGHT_THUMB = 22,
        LEFT_HIP = 23,
        RIGHT_HIP = 24,
        LEFT_KNEE = 25,
        RIGHT_KNEE = 26,
        LEFT_ANKLE = 27,
        RIGHT_ANKLE = 28,
        LEFT_HEEL = 29,
        RIGHT_HEEL = 30,
        LEFT_FOOT_INDEX = 31,
        RIGHT_FOOT_INDEX = 32,

    }

    public JointDefinition GetByName(string name) {
        // used in initialization. Performance is not the matter
        return joints.Where(x => Utils.CompareNodeByName(name, x.name)).FirstOrDefault();
    }

    public JointDefinition GetByPoint(string name) {
        // used in initialization. Performance is not the matter
        return joints.Where(x => x.point.ToString().Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    }

    public bool HasKeyPoint(int id) {
        return jointBones[id] != null;
    }


    internal bool Init(GameObject obj, int[,] scaleBones, SkeletonSet.Skeleton skeletonDescrs) {
        jointBones = new GameObject[JOINT_COUNT];
        var children = obj.GetComponentsInChildren<Transform>();

        List<int> ids = new List<int>();
        
        
        foreach(var skelPoint in skeletonDescrs.description.Where(x => x.node.Length > 0)) {
            var j = GetByPoint(skelPoint.type);
            if (j == null) {
                Debug.LogError("Cant find joint by type specified in skeleton descriptor:" + skelPoint.type);
                return false;
            }

            if (j.pointId >= 0) {
                var node = Array.Find(children, c => c.gameObject.name == j.name)?.gameObject;
                if (node == null) {
                    Debug.LogError("Cant find node:" + j.name);
                    return false;
                }
                this.jointBones[j.pointId] = Array.Find(children, c => Utils.CompareNodeByName(c.gameObject.name, j.name))?.gameObject;
                ids.Add(j.pointId);
            }
        }
        
        /*
        foreach (var j in joints) {
            if (j.pointId >= 0) {
                var node = Array.Find(children, c => c.gameObject.name == j.name)?.gameObject;
                if (node == null) {
                    Debug.LogError("Cant find node:" + j.name);
                    return false;
                }
                this.jointBones[j.pointId] = Array.Find(children, c => Utils.CompareNodeByName(c.gameObject.name, j.name))?.gameObject;
                ids.Add(j.pointId);
            }
        }*/
        ids.Sort();
        this.keyPointsIds = ids.ToArray();

        ScaleBones = new List<Bone>();
        for (var i = 0; i < scaleBones.GetLength(0); ++i) {
            int idx1 = scaleBones[i, 0];
            int idx2 = scaleBones[i, 1];
            ScaleBones.Add(new Bone(idx1, idx2));
        }

        return true;
    }

    // returns only points which corresponds to joint bone
    internal Vector3?[] FilterKeyPoints(Vector3?[] target) {
        return keyPointsIds.Where(id => id < target.Length).Select(id => target[id]).ToArray();
    }

    // returns only joints used for attaching points to skeleton
    internal GameObject[] GetKeyBones() {
        return keyPointsIds.Select(id => jointBones[id]).ToArray();
    }

    public GameObject GetLeftHips() {
        return jointBones[23];
    }

    public GameObject GetRightHips() {
        return jointBones[24];
    }

    public GameObject GetJoint(int idx) {
        return jointBones[idx];
    }

    public GameObject GetJoint(Point pointType) {
        return jointBones[(int)pointType];
    }

}

namespace DeepMotion.DMBTDemo
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using System.IO;
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    //using Newtonsoft.Json.Linq;
    using System.Linq;
    using Assets.Demo.Scripts;
    using Assets.PoseEstimator;
    using System.ComponentModel;
    using UnityWeld.Binding;
    using Assets;
    using Mediapipe;
    using System.Text.RegularExpressions;

    [Serializable]
    public class AvatarDescription
    {
        public string id;
        public GameObject prefab;
    }

    [System.Serializable]
    public class SkeletonSet
    {
        public Skeleton[] skeletons;



        [System.Serializable]
        public class Skeleton
        {
            [System.Serializable]
            public class SkeletonJointDescriptor
            {
                public string type;
                public int id;
                public string node;

            }

            public string name;
            public SkeletonJointDescriptor[] description;

        }

        public static SkeletonSet CreateFromJSON(string jsonString) {
            return JsonUtility.FromJson<SkeletonSet>(jsonString);
        }

    }

    [Binding]
    public class DMBTDemoManager : MonoBehaviour {

        public string avatarType = "body";
        public AvatarDescription[] avatars = new AvatarDescription[0];

        [HideInInspector]
        public Assets.Avatar controller;

        private Assets.Avatar initialAvatar;

        public StatisticDisplay display;
        private PoseScaler poseScaler;
        public IkSettings ikSettings;
        public FilterSettings scaleFilter;
        public FilterSettings posFilter;
        [Range(0,2)]
        public float scaleDepth = 0.5f;

        public delegate void OnNewPoseHandler(bool skeletonExist);
        public event OnNewPoseHandler newPoseEvent;
        private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };

        private FPSCounter counter = new FPSCounter();
        public Skeleton Skeleton { get; private set; }

        //public GameObject[] JointBones { get; private set; }

        public GameObject GetAvatar() {
            return controller.obj;
        }

        void OnValidate() {
            scaleFilter.SetModified();
            posFilter.SetModified();
        }

        void Start() {
            Init();
        }

        private void Init() {

            var foundedAvatar = Array.Find(avatars, x => x.id == avatarType);
            if (foundedAvatar == null) {
                Debug.LogError("Could not found avatar by id");
            }

            var obj = Instantiate(foundedAvatar.prefab, transform);
            obj.SetActive(false);
            Utils.PreparePivots(obj);
            Skeleton = CreateSkeleton(obj);
            controller = new Assets.Avatar(obj, Skeleton);
            controller.settings = ikSettings;
            controller.SetIkSource();


            // obj.SetActive(false);

            poseScaler = GetComponent<PoseScaler>();
            
            poseScaler.Init();

            obj = Instantiate(foundedAvatar.prefab, transform);
            obj.name = "Initial debug copy";
            obj.SetActive(false);
            Utils.PreparePivots(obj);
            initialAvatar = new Assets.Avatar(obj, CreateSkeleton(obj));

        }

        protected Vector3 ScaleVector(Transform transform) {
            return new Vector3(1 * transform.localScale.x, 1 * transform.localScale.z, transform.localScale.y);
        }

        protected Vector3 GetPositionFromNormalizedPoint(Transform screenTransform, float x, float y, float z, bool isFlipped) {
            var relX = (isFlipped ? -1 : 1) * (x - 0.5f);
            var relY = 0.5f - y;

            return Vector3.Scale(new Vector3(relX, relY, z), ScaleVector(screenTransform)) + screenTransform.position;
        }

        private Vector3[] TransformPoints(Transform transform, NormalizedLandmarkList landmarkList, bool flipped) {
            int count = landmarkList.Landmark.Count;

            var points = new Vector3[count];

            for (int i = 0; i < count; ++i) {
                var landmark = landmarkList.Landmark[i];

                var p = GetPositionFromNormalizedPoint(transform, landmark.X, landmark.Y, landmark.Z, flipped);
               // p.x *= 3;
                points[i] = p;
            }

            if (flipped) {
                var fPoints = new Vector3[count];
                int maxSize = Math.Min(count, FLIP_POINTS.Length);
                for (int i = 0; i <maxSize; ++i) { 
                    fPoints[i] = points[FLIP_POINTS[i]];
                }
                points = fPoints;
            }

            return points;
        }

        internal void OnNewPose(Transform transform, NormalizedLandmarkList landmarkList, bool flipped) {
            if (!enabled) {
                newPoseEvent(false);
                return;
            }

            if (landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);
                return;
            }

            var fps = counter.UpdateFps();

            var scale = transform.localScale;
            scale.y = scaleDepth;
            transform.localScale = scale;

            var points = TransformPoints(transform, landmarkList, flipped);
            //TODO
            var ps = points.Select(x => new Vector3?(x)).ToArray();
            controller.SetIkTarget(ps);

            var t = Time.realtimeSinceStartup;
            controller.Update(ikSettings.gradientCalcStep, ikSettings.gradientMoveStep, ikSettings.stepCount);
            var dt = Time.realtimeSinceStartup - t;

            display.LogValue($"FPS:{fps:0.0}", dt, 0, 0, 0, 0);

            newPoseEvent(true);

        }

        private Skeleton CreateDefaultSkeletoStructure() {
            var skeleton = new Skeleton();

            //            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Hips, new int[] {23,24 }, new GeneralFilter(new ScaleFilter(scaleFilter), new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(-10, 10, -10, 10, 0, 359.99f), new ScalingGradCalculator()));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.HIPS, new int[] { 23, 24, 11, 12 }, new GeneralFilter(new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(0, 359.99f, 0, 359.99f, 0, 359.99f), new ScalingGradCalculator()));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_HIP, new int[] { 23, 25, 29 }, new Rotation3DGradCalculator(-70, 15, -120, 70, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_KNEE, new int[] { 25, 29 }, new Rotation1DGradCalculator(-5, 140, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_HEEL, new int[] { 29 }, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            //skeleton.joints.Add(new JointDefinition("LEFT_FOOT_INDEX", -31));
            //skeleton.joints.Add(new JointDefinition("Left toe_end", -31));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_HIP, new int[] { 24, 26, 30 }, new Rotation3DGradCalculator(-15, 70, -70, 120, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_KNEE, new int[] { 26, 30 }, new Rotation1DGradCalculator(-140, 5, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            //TODO ankle in blender == heel in mediapipe
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_HEEL, new int[] { 30 }, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            //skeleton.joints.Add(new JointDefinition("RIGHT_FOOT_INDEX", -32));
            //skeleton.joints.Add(new JointDefinition("Right toe_end", -32));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.SPINE, null, new Rotation3DGradCalculator(-15, 15, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.Z)));
            // skeleton.joints.Add(new JointDefinition(Skeleton.Point.Chest, new int[] { 11, 12 }, new Rotation3DGradCalculator(-10, 10, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.5f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CHEST, new int[] { 11, 12 }, new StretchingGradCalculator(0.5f, 1.5f, StretchingGradCalculator.Axis.PARENT)));
            //skeleton.joints.Add(new JointDefinition(Skeleton.Point.Left shoulder, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_SHOULDER, new int[] { 11, 13, 15 }, new Rotation3DGradCalculator(-85, 80, -15, 120, -115, 85), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_ELBOW, new int[] { 13, 15 }, new Rotation1DGradCalculator(0, 140, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_WRIST, new int[] { 15 }, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            /*skeleton.joints.Add(new JointDefinition("IndexFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("Thumb0_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb1_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_L_end", -1));
            */
            //skeleton.joints.Add(new JointDefinition(Skeleton.Point.Neck, null, new Rotation3DGradCalculator(-25, 25, -25, 25, -25, 25)));
            //skeleton.joints.Add(new JointDefinition("Head", -1));
            //skeleton.joints.Add(new JointDefinition("Eye_L", -1));
            //skeleton.joints.Add(new JointDefinition("Eye_L_end", -1));
            //skeleton.joints.Add(new JointDefinition("Eye_R", -1));
            //skeleton.joints.Add(new JointDefinition("Eye_R_end", -1));
            //skeleton.joints.Add(new JointDefinition(Skeleton.Point.Right shoulder, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_SHOULDER, new int[] { 12, 14, 16 }, new Rotation3DGradCalculator(-85, 80, -120, 15, -55, 115), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_ELBOW, new int[] { 14, 16 }, new Rotation1DGradCalculator(-140, 0, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_WRIST, new int[] { 16 }, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            /* skeleton.joints.Add(new JointDefinition("IndexFinger1_R", -1));
             skeleton.joints.Add(new JointDefinition("IndexFinger2_R", -1));
             skeleton.joints.Add(new JointDefinition("IndexFinger3_R", -1));
             skeleton.joints.Add(new JointDefinition("IndexFinger3_R_end", -1));
             skeleton.joints.Add(new JointDefinition("LittleFinger1_R", -1));
             skeleton.joints.Add(new JointDefinition("LittleFinger2_R", -1));
             skeleton.joints.Add(new JointDefinition("LittleFinger3_R", -1));
             skeleton.joints.Add(new JointDefinition("LittleFinger3_R_end", -1));
             skeleton.joints.Add(new JointDefinition("MiddleFinger1_R", -1));
             skeleton.joints.Add(new JointDefinition("MiddleFinger2_R", -1));
             skeleton.joints.Add(new JointDefinition("MiddleFinger3_R", -1));
             skeleton.joints.Add(new JointDefinition("MiddleFinger3_R_end", -1));
             skeleton.joints.Add(new JointDefinition("RingFinger1_R", -1));
             skeleton.joints.Add(new JointDefinition("RingFinger2_R", -1));
             skeleton.joints.Add(new JointDefinition("RingFinger3_R", -1));
             skeleton.joints.Add(new JointDefinition("RingFinger3_R_end", -1));
             skeleton.joints.Add(new JointDefinition("Thumb0_R", -1));
             skeleton.joints.Add(new JointDefinition("Thumb1_R", -1));
             skeleton.joints.Add(new JointDefinition("Thumb2_R", -1));
             skeleton.joints.Add(new JointDefinition("Thumb2_R_end", -1));
             skeleton.joints.Add(new JointDefinition("Body", -1));
            */
            return skeleton;
        }


        private static bool CompareNodeNames(string objName, string rexExp) {
            objName = Utils.ReplaceSpace(objName.ToLower());
            return Regex.Match(objName, rexExp).Success;
        }

        private static bool IsSkeletonAppliable(SkeletonSet.Skeleton skeleton, Transform[] nodes) {
            List<int> ids = new List<int>();
            foreach (var j in skeleton.description) {
                if (j.id >= 0 && j.node.Length > 0) {

                    var candidateNodes = Array.FindAll(nodes.ToArray(), c => CompareNodeNames(c.gameObject.name, j.node));
                    if (candidateNodes.Length != 1) {
                        return false;
                    }
                }
            }

            return true;
        }

        private Skeleton CreateSkeleton(GameObject obj) {

            var jsonDescr = Resources.Load<TextAsset>("skeletons").text;
            var supportedSkeletons = SkeletonSet.CreateFromJSON(jsonDescr);
            var children = obj.GetComponentsInChildren<Transform>();
            var scalesBones = new int[,] { { 11, 13 }, { 13, 15 }, { 12, 14 }, { 14, 16 }, { 23, 25 }, { 25, 29 }, { 24, 26 }, { 26, 30 } };

            var skeleton = CreateDefaultSkeletoStructure();
            foreach (var descr in supportedSkeletons.skeletons) {
                if (IsSkeletonAppliable(descr, children)) {

                    skeleton.Init(obj, scalesBones, descr);
                    return skeleton;
                    //foreach (var j in descr.description) {
                    //var definition = skeleton.GetByPoint(j.type);
                    //if (definition != null) {
                    //definition.
                    //}
                    //}
                }
            }
            Debug.LogError("Could not find supported skeleton");
            return skeleton;
        }

        private Skeleton CreateSkeleton2(GameObject obj) {

            var supportedSkeletons = Resources.Load<TextAsset>("skeletons");

            var temp = SkeletonSet.CreateFromJSON(supportedSkeletons.text);
            Debug.Log(supportedSkeletons.text);

            
            var skeleton = new Skeleton();
            /*
            //            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Hips, new int[] {23,24 }, new GeneralFilter(new ScaleFilter(scaleFilter), new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(-10, 10, -10, 10, 0, 359.99f), new ScalingGradCalculator()));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_Hip, new int[] { 23, 24, 11, 12 }, new GeneralFilter(new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(0, 359.99f, 0, 359.99f, 0, 359.99f), new ScalingGradCalculator()));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Thigh, new int[] { 23, 25, 29 }, new Rotation3DGradCalculator(-70, 15, -120, 70, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Calf, new int[] { 25, 29 }, new Rotation1DGradCalculator(-5, 140, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Foot, new int[] { 29 }, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));

            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Thigh, new int[] { 24, 26, 30 }, new Rotation3DGradCalculator(-15, 70, -70, 120, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Calf, new int[] { 26, 30 }, new Rotation1DGradCalculator(-140, 5, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            //TODO ankle in blender == heel in mediapipe
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Foot, new int[] { 30 }, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));

            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_Spine01, null, new Rotation3DGradCalculator(-15, 15, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 5.0f, StretchingGradCalculator.Axis.Z)));
            // skeleton.joints.Add(new JointDefinition(Skeleton.Point.Chest, new int[] { 11, 12 }, new Rotation3DGradCalculator(-10, 10, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.5f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Chest, new int[] { 11, 12 }, new StretchingGradCalculator(0.5f, 30.5f, StretchingGradCalculator.Axis.PARENT)));


            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Upperarm, new int[] { 11, 13, 15 }, new Rotation3DGradCalculator(-85, 80, -15, 120, -115, 85), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Forearm, new int[] { 13, 15 }, new Rotation1DGradCalculator(0, 140, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_L_Hand, new int[] { 15 }, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Upperarm, new int[] { 12, 14, 16 }, new Rotation3DGradCalculator(-85, 80, -120, 15, -55, 115), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Forearm, new int[] { 14, 16 }, new Rotation1DGradCalculator(-140, 0, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CC_Base_R_Hand, new int[] { 16 }, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            //skeleton.joints.Add(new JointDefinition(Skeleton.Point.Neck, null, new Rotation3DGradCalculator(-25, 25, -25, 25, -25, 25)));

            /*
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Neck, null, new Rotation3DGradCalculator(-25, 25, -25, 25, -25, 25)));
            skeleton.joints.Add(new JointDefinition("Left toe", -31));
            skeleton.joints.Add(new JointDefinition("Left toe_end", -31));
            skeleton.joints.Add(new JointDefinition("Right toe", -32));
            skeleton.joints.Add(new JointDefinition("Right toe_end", -32));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Left shoulder, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("IndexFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger1_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger2_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_L", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_L_end", -1));
            skeleton.joints.Add(new JointDefinition("Thumb0_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb1_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_L", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_L_end", -1));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Neck, null, new Rotation3DGradCalculator(-25, 25, -25, 25, -25, 25)));
            skeleton.joints.Add(new JointDefinition("Head", -1));
            skeleton.joints.Add(new JointDefinition("Eye_L", -1));
            skeleton.joints.Add(new JointDefinition("Eye_L_end", -1));
            skeleton.joints.Add(new JointDefinition("Eye_R", -1));
            skeleton.joints.Add(new JointDefinition("Eye_R_end", -1));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Right shoulder, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));

            skeleton.joints.Add(new JointDefinition("IndexFinger1_R", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger2_R", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_R", -1));
            skeleton.joints.Add(new JointDefinition("IndexFinger3_R_end", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger1_R", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger2_R", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_R", -1));
            skeleton.joints.Add(new JointDefinition("LittleFinger3_R_end", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger1_R", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger2_R", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_R", -1));
            skeleton.joints.Add(new JointDefinition("MiddleFinger3_R_end", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger1_R", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger2_R", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_R", -1));
            skeleton.joints.Add(new JointDefinition("RingFinger3_R_end", -1));
            skeleton.joints.Add(new JointDefinition("Thumb0_R", -1));
            skeleton.joints.Add(new JointDefinition("Thumb1_R", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_R", -1));
            skeleton.joints.Add(new JointDefinition("Thumb2_R_end", -1));
            skeleton.joints.Add(new JointDefinition("Body", -1));
            */


            var scalesBones = new int[,] { { 11, 13 }, { 13, 15 }, { 12, 14 }, { 14, 16 }, { 23, 25 }, { 25, 29 }, { 24, 26 }, { 26, 30 } };
      //      skeleton.Init(obj, scalesBones);

            return skeleton;
        }

        internal void ResetAvatar() {
            controller.CopyRotationAndPositionFromAvatar(initialAvatar);
        }

        /*     private Transform[] FindIKSource() {
                 return JointBones.Where(x => x != null).Select(x => x.transform).ToArray();

             }

             private Vector3?[] FindIKTarget(Vector3?[] targetPoints) {
                 return targetPoints.Where((p, i) => JointBones[i] != null).ToArray();
             }*/
    }
}