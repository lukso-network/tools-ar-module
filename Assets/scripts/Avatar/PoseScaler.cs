using Assets.Demo.Scripts;
using DeepMotion.DMBTDemo;
using Kalman;
using LinearAlgebra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.PoseEstimator
{
    class Bone
    {
        int startIdx;
        int endIdx;
        public float length;
        float [] lenHist;
        float[] len2Hist;
        int histIndex;

        public float aver;
        public float sqAver;
        public float Disp => (sqAver - aver * aver) / HIST_SIZE;
        const int HIST_SIZE = 10;

        public Bone(int start, int end) {
            startIdx = start;
            endIdx = end;
            lenHist = new float[HIST_SIZE];
            len2Hist = new float[HIST_SIZE];
            length = 0;
            histIndex = 0;
            aver = 0;
            sqAver = 0;
        }

        public void Update(Vector3[] points) {
            var s2 = (points[startIdx] - points[endIdx]).sqrMagnitude;
            var s = Mathf.Sqrt(s2);
            length = s;

            aver += s - lenHist[histIndex];
            sqAver += s2 - len2Hist[histIndex];
            lenHist[histIndex] = s;
            len2Hist[histIndex] = s2;
            histIndex = (histIndex + 1) % HIST_SIZE;

        }
    }

    class BoneTracker
    {
        enum Type : int {
            RIGHT_SHOLDER_1 = 0,
            RIGHT_ARM2,
            RIGHT_ARM1,
            LEFT_SHOLDER_1,
            LEFT_ARM2,
            LEFT_ARM1,
            SPINE,

            RIGHT_HIP,
            RIGHT_LEG2,
            RIGHT_LEG1,

            LEFT_HIP,
            LEFT_LEG2,
            LEFT_LEG1
        }

        private Vector3 DOWN_DIR = new Vector3(0, -1, -0.2f).normalized;
        private Bone[] bones;
        //TODO
        public bool[] correctPoints = new bool[(int)RawPoint.COUNT];

        private BoneValidator parameters;

        public BoneTracker() {
            var pts = new int[,] { { 8, 12 }, { 12, 11 }, { 11, 10 }, { 8, 13 }, { 13, 14 }, { 14, 15 }, { 6, 8 }, { 6, 2 }, { 2, 1 }, { 1, 0 }, { 6, 3 }, { 3, 4 }, { 4, 5 } };
            bones = new Bone[pts.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i) {
                bones[i] = new Bone(pts[i, 0], pts[i, 1]);
            }

           // validators
        }

        public void Update(Vector3[]points, bool lowQuality) {
            foreach(var bone in bones) {
                bone.Update(points);
            }

            for(int i = 0; i < correctPoints.Length; ++i) {
                correctPoints[i] = true;
            }

            FixIncorrectPoints(points, lowQuality);
        }

        private void FixIncorrectPoints(Vector3 [] points, bool lowQuality) {

            float leftArm1to2 = GetRelLength(Type.LEFT_ARM1, Type.LEFT_ARM2);
            float leftArm2toSpine = GetRelLength(Type.LEFT_ARM2, Type.SPINE);

            float leftLeg1to2 = GetRelLength(Type.LEFT_LEG1, Type.LEFT_LEG2);
            float leftLeg2toSpine = GetRelLength(Type.LEFT_LEG2, Type.SPINE);

            float rightArm1to2 = GetRelLength(Type.RIGHT_ARM1, Type.RIGHT_ARM2);
            float rightArm2toSpine = GetRelLength(Type.RIGHT_ARM2, Type.SPINE);

            float rightLeg1to2 = GetRelLength(Type.RIGHT_LEG1, Type.RIGHT_LEG2);
            float rightLeg2toSpine = GetRelLength(Type.RIGHT_LEG2, Type.SPINE);

            if (!IsCorrectValue(rightArm1to2, parameters.arm1to2)) {
                CorrectLowerHand(points, true);
            }

            if (!IsCorrectValue(leftArm1to2, parameters.arm1to2)) {
                CorrectLowerHand(points, false);
            }

            if (lowQuality || !IsCorrectValue(leftLeg1to2, parameters.leg1to2) || !IsCorrectValue(rightLeg1to2, parameters.leg1to2)
               // || !IsCorrectValue(leftLeg2toSpine * leftLeg1to2, parameters.leg2toSpine) || !IsCorrectValue(rightLeg2toSpine * rightLeg1to2, parameters.leg2toSpine)) {
                || !IsCorrectValue(leftLeg2toSpine, parameters.leg2toSpine) || !IsCorrectValue(rightLeg2toSpine, parameters.leg2toSpine)) {
                //selfie mode

                CorrectForSelfie(points);

            } 


            //  Debug.Log($"arms: {leftArm1to2} {leftArm2toSpine}, legs: {leftLeg1to2}, {leftLeg2toSpine}");
            //Debug.Log($"{rightArm1to2} {rightArm2toSpine} {rightLeg1to2} {rightLeg2toSpine}");
            Debug.Log($"ar:{rightArm1to2} al:{leftArm1to2} lr:{rightLeg1to2} ll{leftLeg1to2}, {lowQuality}");
        }

        private void CorrectLowerHand(Vector3[] points, bool isRight) {

            var sh = (int)(isRight ? RawPoint.RSHOULDER : RawPoint.LSHOULDER);
            var elb = (int)(isRight ? RawPoint.RELBOW : RawPoint.LELBOW);
            var wr = (int)(isRight ? RawPoint.RWRIST : RawPoint.LWRIST);
            
            var shoulder = points[sh];
            var elbow = points[elb];

            elbow.z = shoulder.z;
            points[elb] = elbow;

           // points[wr] = elbow + DOWN_DIR * (elbow - shoulder).magnitude * 0.8f;
            points[wr] = elbow + DOWN_DIR * GetSpineLength(points) * 0.5f;
            correctPoints[wr] = false;
            Debug.Log("Fix lower hand");
        }

        private float GetSpineLength(Vector3[]points) {

            var neck = points[(int)RawPoint.NECK0];
            var root = points[(int)RawPoint.ROOT];
            var spineLen = (neck - root).magnitude;

            var w0 = (points[(int)RawPoint.LSHOULDER] - points[(int)RawPoint.RSHOULDER]).magnitude / 2;

            var minSpineRelShoulders = 1.7f;

            return Math.Max(spineLen, w0 * 2 * minSpineRelShoulders);
        }


        private void CorrectForSelfie(Vector3 [] points) {
         


            var neck = points[(int)RawPoint.NECK0];
            var root = points[(int)RawPoint.ROOT];
            var spineLen = (neck - root).magnitude;

            var w0 = (points[(int)RawPoint.LSHOULDER] - points[(int)RawPoint.RSHOULDER]).magnitude / 2;

            var minSpineRelShoulders = 1.7f;

            spineLen = Math.Max(spineLen, w0 * 2 * minSpineRelShoulders);


            float h3 = neck.y - 1.2f;
            float h2 = neck.y - 0.9f;
            float h1 = neck.y - 0.5f;
            float h0 = neck.y - 0.4f;

            float hipSize = 1.05f;

            var rHip = neck + spineLen * DOWN_DIR * hipSize - Vector3.right * w0;
            var lHip = neck + spineLen * DOWN_DIR * hipSize + Vector3.right * w0;
            var rRoot = neck + spineLen * DOWN_DIR;
            var l1 = 1.2f;
            var l2 = 0.7f;
            var kneeSize = Vector3.forward * l1 * spineLen * 0.05f;

            SetManualPoint(points, RawPoint.RHIP, rHip);
            SetManualPoint(points, RawPoint.RANKLE, rHip + DOWN_DIR * spineLen * l1 );
            SetManualPoint(points, RawPoint.RKNEE, rHip + DOWN_DIR * spineLen * l2 + kneeSize);
            SetManualPoint(points, RawPoint.ROOT, rRoot);

            SetManualPoint(points, RawPoint.LHIP, lHip);
            SetManualPoint(points, RawPoint.LANKLE, lHip + DOWN_DIR * spineLen * l1);
            SetManualPoint(points, RawPoint.LKNEE, lHip + DOWN_DIR * spineLen * l2 + kneeSize);

            /*
            Vector3[] defPoints = new Vector3[] { new Vector3(neck.x-w0, h3, neck.z), new Vector3(neck.x-w0, h2, neck.z), new Vector3(neck.x-w0, h1, neck.z),
                                                    new Vector3(neck.x+w0, h1, neck.z),new Vector3(neck.x+w0, h2, neck.z), new Vector3(neck.x + w0, h3, neck.z),
            new Vector3(neck.x, h0, neck.z), };



            for (int i = 0; i < defPoints.Length; ++i) {
                points[i] = defPoints[i];
                correctPoints[i] = false;
            }*/
            
        }

        private void SetManualPoint(Vector3 [] points, RawPoint type, Vector3 p) {
            points[(int)type] = p;
            correctPoints[(int)type] = false;
        }


        private bool IsCorrectValue(float value, Vector2 minMax) {
            return value >= minMax.x && value <= minMax.y;
        }

        private float GetRelLength(Type bone1, Type bone2) {
            return bones[(int)bone1].length / (bones[(int)bone2].length + 0.0001f);
        }

        internal void SetParameters(BoneValidator boneValidatorParams) {
            this.parameters = boneValidatorParams;
        }
    }


    public class PoseScaler : MonoBehaviour {
        //private GameObject[] jointBones;
        private Vector3 initialScale;
        private Vector3 initialPosition;
        private Vector3 defaultCamPosition;
        public Camera rawCamera;
        public GameObject testDotPrefab;
        private DMBTDemoManager manager;
        private GameObject avatar;
        private BoneTracker boneTracker = new BoneTracker();

        public Camera mainCamera;

        //debugging point
        public GameObject pointRoot;
        public GameObject bonesRoot;
        public GameObject initBones;
        public GameObject skeletonRoot;
        public GameObject bonePrefab;

        //[Range(1, 50)]
        // kalman dt param for scaling and moving camera
        [Range(0, 0.3f)]
        public float cameraScaleKalmanDt = 0.1f;
        [Range(0, 2)]
        public float cameraShiftKalmanDt = 1;
        [Range(0, 1)]
        public float discardPointThreshold = 1;
        [Range(1, 200)]
        public int discardPointStep = 1;

        // sigma param for scaling and moving camera
        [Range(0, 0.1f)]
        public float cameraScaleSigma = 0.01f;

        [Range(0, 0.1f)]
        public float cameraShiftSigma = 0.01f;

        [Range(0, 5)]
        public float kalmanDt = 0.1f;

        [Header("Selfie")]
        [Range(0, 1)]
        public float selfiConfidence = 0.4f;
        public bool selfieEnabled = true;

        public BoneValidator boneValidator;

        private Filter<float> scaleFilter;
        private Filter<float> scaleXFilter;
        private Filter<float> scaleYFilter;

        public Filter<Vector3[]> pointFilter;
        public FilterType filterType = FilterType.XV;

        private int scrWidth;
        private int scrHeight;

        private float[] upperWeights;
        private float[] wholeWeights;
        private float[] lowerWeights;

        private int[,] skeletonPoints;

        // points by deepmotion
        private Vector3[] rawPoints;

        // points transformed to correct camera sizes
        private Vector3[] targetPoints = new Vector3[(int)RawPoint.COUNT];

        void Awake() {
            InitFilters();
        }

        // Use this for initialization
        void Start() {

            boneTracker.SetParameters(boneValidator);
            manager = GetComponentInParent<DMBTDemoManager>();
            defaultCamPosition = Camera.main.transform.localPosition;
            OnValidate();

            CreateSkeleton();
        }

        private void CreateSkeleton() {
            skeletonPoints = new int[,] { { 8, 12 }, { 12, 11 }, { 11, 10 }, { 8, 13 }, { 13, 14 }, { 14, 15 }, { 6, 8 }, { 6, 2 }, { 2, 1 }, { 1, 0 }, { 6, 3 }, { 3, 4 }, { 4, 5 } };

            var left_leg = new int[,] { { 6, 3 }, { 3, 4 }, { 4, 5 } };
            var right_leg = new int[,] { { 2, 1 }, { 1, 0 } };
            var left_arm = new int[,] { };
            var right_arm = new int[,] { };
        }

        public void OnValidate() {
            InitFilters();
        }

        void InitFilters() {
            // scaleFilter = new LowPassFilter(scalerSmoothStep);
            scaleFilter = new KalmanPointFilter(cameraScaleSigma, cameraScaleKalmanDt, FilterType.XV);
            scaleXFilter = new KalmanPointFilter(cameraShiftSigma, cameraShiftKalmanDt, FilterType.XV);
            scaleYFilter = new KalmanPointFilter(cameraShiftSigma, cameraShiftKalmanDt, FilterType.XV);
            // scaleYFilter = new LowPassFilter(scalerShiftSmoothStep);

            if (filterType == FilterType.LOW_PASS) {
                pointFilter = new RawPointFilter(discardPointThreshold, discardPointStep);
            } else if (filterType == FilterType.None) {
                pointFilter = new NoneFilter();
            } else {
                pointFilter = new RawPointFilterKalman(discardPointThreshold, discardPointStep, kalmanDt, filterType);
            }
        }

        void OnEnable() {

            // required for debugging purposes
            // allows to change code fastly without Unity reloading
           // Init();
        }

        // Update is called once per frame
        void Update() {
            //!!! FOR SOME REASONS Screen.width and height can be incorrect outside of update function when called from frame update function (for example it can be 2428x30)
            scrWidth = Screen.width;
            scrHeight = Screen.height;

            //rawCamera.orthographicSize = Screen.height / (float)(Screen.width) / 2;

        }

        public void Init() {
            manager = GetComponentInParent<DMBTDemoManager>();
         //   avatar = manager.GetAvatar();
            //this.jointBones = manager.JointBones;
            initialScale = avatar.transform.localScale;
            initialPosition = avatar.transform.position;

            InitWeights();
        }

        public Vector3[] SetFrameRawPoints(Vector3[] points, float quality) {
            //Debug.Log("Quality:" + quality);
            rawPoints = pointFilter.filter(points);

            boneTracker.Update(rawPoints, selfieEnabled && quality < selfiConfidence);

            return rawPoints;
        }

        //TODO
        // returns scaled points
        public Vector3?[] TransformCameraToPoints(int height, int width) {

            //TODO
            for (int j = 0; j < rawPoints.Length; ++j) {
                rawPoints[j].x = -rawPoints[j].x;
            }

            // TODO make rawPoints immutable
            if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown) {
                float s = width / (float)height;
                if (width > height) {
                    for (int j = 0; j < rawPoints.Length; ++j) {
                        var p = rawPoints[j];
                        p.x *= s;
                        p.y *= s;
                        rawPoints[j] = p;
                    }
                }
            }

            float kx, mx, my;
            CalculateScales(rawPoints, out kx, out mx, out my, width, height);

            for (int j = 0; j < rawPoints.Length; ++j) {
                var point = rawPoints[j];
                targetPoints[j] = new Vector3((point.x - mx) / kx, (point.y - my) / kx, point.z / kx);
            }

            var ikPoints = targetPoints.Select(x => (Vector3?)x).ToArray();

            for(int i = 0; i < boneTracker.correctPoints.Length; ++i) {
                if (!boneTracker.correctPoints[i]) {
                    ikPoints[i] = null;
                }
            }
          /*  if (selfieActive) {
                //TODO
                for (int j = 0; j < 7; ++j) {
                    ikPoints[j] = null;
                }
            }
           */

            return ikPoints;
        }

        private void CorrectForSelfie() {
            const int R_FOOT = 0;
            const int R_LEG = 1;
            const int R_UP_LEG = 2;

            const int L_FOOT = 5;
            const int L_LEG = 4;
            const int L_UP_LEG = 3;

            const float w0 = 0.1f;
            var z = rawPoints[8].z;
            var y = rawPoints[8].y;
            var xc = rawPoints[8].x;

            float h3 =y - 1.2f;
            float h2 =y - 0.9f;
            float h1 =y - 0.5f;
            float h0 =y - 0.4f;

            Vector3[] defPoints = new Vector3[] { new Vector3(xc-w0, h3, z), new Vector3(xc-w0, h2, z), new Vector3(xc-w0, h1, z),
                                                    new Vector3(xc+w0, h1, z),new Vector3(xc+w0, h2, z), new Vector3(xc + w0, h3, z),
            new Vector3(xc, h0, z), };


            for (int i = 0; i < defPoints.Length; ++i) {
                rawPoints[i] = defPoints[i];
            }


            //rawPoints[R_FOOT].y = 

            /* for (int pointIndex = 0; pointIndex < rawPoints.Length; ++pointIndex) {
                 var deltaY = (pointIndex < 6) ? -1 : -1;
                 deltaY = 0;
                 rawPointsCloudPositions[pointIndex] = DmbtConverters.ConvertToUnity(poseEstimatorPointCloudOutput.m_points[pointIndex]) - new Vector3(this.offset, 0.5f + deltaY);

                 if (pointIndex == 0 || pointIndex == 5) {
                     rawPointsCloudPositions[pointIndex].y = -1.2f - 1;
                     rawPointsCloudPositions[pointIndex].z = 0f;
                 }

                 if (pointIndex == 1 || pointIndex == 4) {
                     rawPointsCloudPositions[pointIndex].y = -0.9f - 1;
                     rawPointsCloudPositions[pointIndex].z = 0f;
                 }

                 if (pointIndex == 2 || pointIndex == 3) {
                     rawPointsCloudPositions[pointIndex].y = -0.7f - 1;
                     rawPointsCloudPositions[pointIndex].z = 0f;
                 }



             }*/
        }

        internal void InitWeights() {
            //TODO
            int LOW_INDEX = 7;
          /*  upperWeights = jointBones.Select((j, idx) => (j != null && idx >= LOW_INDEX) ? 1f : 0f).ToArray();
            lowerWeights = jointBones.Select((j, idx) => (j != null && idx < LOW_INDEX) ? 1f : 0f).ToArray();
            wholeWeights = jointBones.Select((j, idx) => (j != null) ? 1f : 0f).ToArray();*/
        }

       /* public void ApplyPoseIK(Vector3[] targetPoints, out Transform[] ikSrc, out Vector3[] ikTgt) {

            var ikSource = new List<Transform>();
            var ikTarget = new List<Vector3>();
            for (int i = 0; i < targetPoints.Length; ++i) {
                var bone = this.jointBones[i];

                if (bone != null) {
                    ikSource.Add(bone.transform);
                    ikTarget.Add(targetPoints[i]);
                }
                ++i;
            }

            ikSrc = ikSource.ToArray();
            ikTgt = ikTarget.ToArray();
        }
       */
        /*
        public void DisplayHelpers(Assets.Avatar ikAvatar) {

            pointRoot.SetActive(manager.renderBoneDots);
            bonesRoot.SetActive(manager.renderBoneDots);
            initBones.SetActive(manager.renderBoneDots);
            skeletonRoot.SetActive(manager.renderBoneDots);
            if (!manager.renderBoneDots) {
                return;
            }


            CreateHelpers(pointRoot, rawPoints.Length, testDotPrefab, new Color(0, 0, 1), 0.03f);
            CreateHelpers(bonesRoot, rawPoints.Length, testDotPrefab, new Color(1, 0, 0), 0.05f);
            CreateHelpers(initBones, rawPoints.Length, testDotPrefab, new Color(0, 1, 0), 0.03f);
            CreateHelpers(skeletonRoot, skeletonPoints.GetLength(0), bonePrefab, new Color(1f, 1f,1f), 0.05f);

            for (int i = 0; i < rawPoints.Length; ++i) {
                // raw point
                var targetPoint = pointRoot.transform.GetChild(i);

                // result bone point
                var bonePoint = bonesRoot.transform.GetChild(i);

                // init bone point generated by pose estimator
                var initBonePoint = initBones.transform.GetChild(i);
                var bone = this.jointBones[i];

                targetPoint.transform.localPosition = new Vector3(targetPoints[i].x, targetPoints[i].y, targetPoints[i].z);
                if (bone != null) {
                    bonePoint.transform.localPosition = bone.transform.position;
                    

                    var joint = ikAvatar.GetByName(bone.name);
                    if (joint != null) {
                        initBonePoint.localPosition = joint.initPosition;
                    }

                } else {
                    //targetPoint.gameObject.SetActive(false);
                    initBonePoint.gameObject.SetActive(false);
                    bonePoint.gameObject.SetActive(false);
                }
            }


            for (int i = 0; i < skeletonPoints.GetLength(0); ++i) {
                var p1 = targetPoints[skeletonPoints[i,0]];
                var p2 = targetPoints[skeletonPoints[i,1]];

                var rot = Quaternion.FromToRotation(Vector3.up, (p2 - p1));
                var scale = (p2 - p1).magnitude;

                var bone = skeletonRoot.transform.GetChild(i);
                bone.transform.rotation = rot;
                bone.transform.localScale = new Vector3(scale, scale, scale);
                bone.transform.position = p1;


            }
        }
        */

        private void CreateHelpers(GameObject parent, int count, GameObject prefab, Color color, float size, String layer = null) {
            if (parent.transform.childCount == count) {
                return;
            }
            for (int i = 0; i < count; ++i) {
                var cube = Instantiate(prefab, parent.transform);

                var r = cube.GetComponent<Renderer>();
                if (r != null) { 
                    r.material.color = color;
                }
                cube.transform.localScale = Vector3.one * size;
                if (layer != null) {
                    cube.layer = LayerMask.NameToLayer(layer);
                }
            //    cube.name = $"{i} { jointBones[i].name }";
            }
        }


        private void CalculateScales(Vector3 []rawPoints, out float kx, out float mx, out float my, float width, float height) {

            var weights = boneTracker.correctPoints.Select((x,i) => x ? wholeWeights[i]: 0f).ToArray();
            avatar.transform.localScale = initialScale;
            avatar.transform.localPosition = initialPosition;
            float MAX = 100;
            /* Vector2 minJoint = new Vector3(MAX, MAX);
             Vector2 maxJoint = new Vector3(-MAX, -MAX);
             Vector2 minBone =  new Vector3(MAX, MAX);
             Vector2 maxBone = new Vector3(-MAX, -MAX);
            */


            // rawPoints = rawPoints.Where((p, i) => jointBones[i] != null).ToArray();
            // var bonesPoints = jointBones.Where(p => p != null).Select(p => p.transform.position).ToList();

            var bonesPoints = new Vector3[] { };// jointBones.Select(p => p != null ? p.transform.position : Vector3.zero).ToList();


            //Least squeare

            float ky;

            if (false) {
                Utils.LeastSquares(bonesPoints.Select(p => p.x).ToArray(), rawPoints.Select(p => p.x).ToArray(), out kx, out mx);
                Utils.LeastSquares(bonesPoints.Select(p => p.y).ToArray(), rawPoints.Select(p => p.y).ToArray(), out ky, out my);

                kx = (kx + ky) / 2; //IT SHOULD BE MORE CORRECT
            } else {

                Utils.LeastSquaresUniform(bonesPoints.Select(p => p.x).ToArray(),
                    bonesPoints.Select(p => p.y).ToArray(),
                    rawPoints.Select(p => p.x).ToArray(),
                    rawPoints.Select(p => p.y).ToArray(),
                    weights,
                    out kx, out mx, out my);

/*
                float kx2, mx2, my2;
                Utils.LeastSquaresUniform(bonesPoints.Select(p => p.x).ToArray(),
                    bonesPoints.Select(p => p.y).ToArray(),
                    rawPoints.Select(p => p.x).ToArray(),
                    rawPoints.Select(p => p.y).ToArray(),
                    upperWeights,
                    out kx2, out mx2, out my2);
*/
               // Debug.Log($"kx={Math.Abs(kx-kx2)/kx}: kx={kx} {mx} {my}, kx2={kx2} {mx2} {my2}");


                // Debug.Log($"kx={kx} {mx} {my}");
            }

            //Debug.Log($"kx={kx} {mx} {my} ");
            kx = scaleFilter.filter(kx);
            mx = scaleXFilter.filter(mx);
            my = scaleYFilter.filter(my);

            float aspectScr = scrHeight / (float)(scrWidth) / 2;
            float aspectImg = height / (float)width / 2;

            // works for FIT_IN_PARENT_MODE:
            if (height < width) {
                mainCamera.orthographicSize = Mathf.Max(aspectScr, aspectImg) / kx;
            } else {
                mainCamera.orthographicSize = Mathf.Max(aspectScr / aspectImg, 1) / kx / 2;
            }

            // current sdk translates model already
            mainCamera.transform.localPosition = defaultCamPosition + new Vector3(-mx / kx, -my / kx, 0);




        }


    }
}