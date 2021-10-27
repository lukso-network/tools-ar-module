

using Assets;
using Assets.Demo.Scripts;
using Assets.PoseEstimator;
using DeepMotion.DMBTDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Joint = Assets.Joint;
using Assets.scripts.Avatar;


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

    [Binding]
    public class DMBTDemoManager : MonoBehaviour {


        [Serializable]
        public class AvatarDescription {
            public string id;
            public GameObject prefab;
        }

        public StatisticDisplay display;
        public FilterSettings scaleFilter;
        public SkeletonManager skeletonManager;
        public GameObject facePrefab;
        public GameObject hat;

        private GameObject face;
        private Mesh faceMesh;

        [Range(0, 2)]
        public float scaleDepth = 0.5f;

        public delegate void OnNewPoseHandler(bool skeletonExist);
        public event OnNewPoseHandler newPoseEvent;
        private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };

        private FPSCounter counter = new FPSCounter();
        public bool ShowTransparentFace {
            get =>face.GetComponent<TransparentMaterialRenderer>().enabled;
            set => face.GetComponent<TransparentMaterialRenderer>().enabled = value;
            }

        void OnValidate() {
            scaleFilter.SetModified();
        }

        void Start() {
            InitFace();
            Init();
        }

        private void Init() {

            try {

           /*     var foundedAvatar = Array.Find(avatars, x => x.id == avatarType);
                if (foundedAvatar == null) {
                    Debug.LogError("Could not found avatar by id");
                }

                var obj = Instantiate(foundedAvatar.prefab, transform);
                obj.SetActive(false);
                Utils.PreparePivots(obj);
                /*Skeleton = CreateSkeleton(obj);
                controller = new Assets.Avatar(obj, Skeleton);
                controller.settings = ikSettings;
                controller.SetIkSource();*/


                // obj.SetActive(false);

                /*poseScaler = GetComponent<PoseScaler>();

                poseScaler.Init();

                obj = Instantiate(foundedAvatar.prefab, transform);
                obj.name = "Initial debug copy";
                obj.SetActive(false);
                Utils.PreparePivots(obj);
                initialAvatar = new Assets.Avatar(obj, CreateSkeleton(obj));
                */
            } catch (Exception ex) {
                Debug.LogError("DMBTManage failed");
                Debug.LogException(ex);
            }

        }

        protected Vector3 ScaleVector(Transform transform) {
            return new Vector3(1 * transform.localScale.x, 1 * transform.localScale.z, transform.localScale.y);
        }

        protected Vector3 GetPositionFromNormalizedPoint(Transform screenTransform, Vector3 v, bool isFlipped, float zShift, float perspectiveScale) {
            var relX = (isFlipped ? -1 : 1) * (v.x - 0.5f);
            var relY = 0.5f - v.y;

            //return Vector3.Scale(new Vector3(relX, relY, z), ScaleVector(screenTransform)) + screenTransform.position;
            var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), ScaleVector(screenTransform)) + screenTransform.position;
            pos3d += (Camera.main.transform.position - pos3d).normalized * (-(v.z+zShift)) * screenTransform.localScale.y * perspectiveScale;
            return pos3d;
        }

        private float CalculateZShift(Transform screenTransform, Vector3[] skeletonPoints, NormalizedLandmarkList faceLandmarks, float perspectiveScale) {

            if (faceLandmarks.Landmark.Count == 0) {
                return 0;
            }
            var from = LandmarkToVector(faceLandmarks.Landmark[4]); //nose

            var l = skeletonPoints[(int)Skeleton.Point.LEFT_SHOULDER];
            var r = skeletonPoints[(int)Skeleton.Point.RIGHT_SHOULDER];
            var lh = skeletonPoints[(int)Skeleton.Point.LEFT_HIP];
            var rh = skeletonPoints[(int)Skeleton.Point.RIGHT_HIP];

            var rlDir = (l - r).normalized;
            var upDir = ((l + r) / 2 - (lh + rh) / 2).normalized;
            var len = (l - r).magnitude;

            var nosePose = (l + r) / 2 + len * upDir * 0.5f + Vector3.Cross(upDir, rlDir) * len * 0.2f;

            var to = nosePose;
            //var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), ScaleVector(screenTransform)) + screenTransform.position;
            //pos3d += (Camera.main.transform.position - pos3d).normalized * (-z) * screenTransform.localScale.y * perspectiveScale;

            var pos3d = screenTransform.position;
            float delta = -(to.z - pos3d.z) / (Camera.main.transform.position - pos3d).normalized.z / (screenTransform.localScale.y * perspectiveScale) - from.z;


            var testP = GetPositionFromNormalizedPoint(screenTransform, from, false, delta, perspectiveScale);

            Debug.Log((testP - to).z);
            return delta;
        }

        private Vector3 ToVector3(NormalizedLandmark landmark) {
            return new Vector3(landmark.X, landmark.Y, landmark.Z);
        }

        private float GetSpineSize(NormalizedLandmarkList landmarklist) {

            var left = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.LEFT_HIP]);
            var right = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.RIGHT_HIP]);

            var leftArm = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.LEFT_SHOULDER]);
            var rightArm = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.RIGHT_SHOULDER]);

            //probably Vector2 needed
            var l = ((left + right) / 2 - (leftArm + rightArm) / 2).magnitude;

            return l;
        }

        private Vector3 LandmarkToVector(NormalizedLandmark lnd) {
            return new Vector3(lnd.X, lnd.Y, lnd.Z);
        }

        private Vector3[] TransformPoints(Transform transform, NormalizedLandmarkList landmarkList, bool flipped, float zShift = 0, float spineSize = -1) {
            if (spineSize < 0) {
                spineSize = 1;
            }
            int count = landmarkList.Landmark.Count;

            var points = new Vector3[count];
            var minV = new Vector3(100, 100, 100);
            var maxV = new Vector3(-100, -100, -100);
            for (int i = 0; i < count; ++i) {
                var landmark = landmarkList.Landmark[i];

                var l = new Vector3(landmark.X, landmark.Y, landmark.Z);
                minV = Vector3.Min(minV, l);
                maxV = Vector3.Max(maxV, l);
                var p = GetPositionFromNormalizedPoint(transform, LandmarkToVector(landmark), flipped, zShift, spineSize);
                points[i] = p;
            }

            Debug.Log("min/max:" + minV.z + " " + maxV.z);
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


        private float defaultFaceSize;
        private float faceGeomCoef;
        private void InitFace() {
            face = Instantiate(facePrefab, transform);
            face.AddComponent<TransparentMaterialRenderer>();
            faceMesh = face.GetComponent<MeshFilter>().mesh;
            faceMesh.RecalculateNormals();

            var points = faceMesh.vertices;

            var t = points[10];
            var b = points[152];
            var r = points[33];
            var l = points[263];
            defaultFaceSize = ((t - b).magnitude * (l - r).magnitude);

            var nose = points[4];
            var c0 = (r + l) / 2;
            var d1 = nose - c0;
            var d2 = (l - r);

            faceGeomCoef = Vector3.Dot(d1, d1) / Vector3.Dot(d2, d2);
        }


        private void UpdateFace(Vector3 [] points) {
            //TOFO
            if (points.Length == 0) {
                return;
            }

            faceMesh.vertices = points;

            var nose = points[4];
            var t = points[10];
            var b = points[152];
            var r = points[33];
            var l = points[263];

            var center = (t + b + r + l) / 4;
           // Debug.Log("Magn:" + (t - b).magnitude + " " + (l - r).magnitude);
            var scale = Mathf.Sqrt(((t - b).magnitude * (l - r).magnitude)  / defaultFaceSize);

            var up = (t - b).normalized;
            var left = (l - r).normalized;
            var front = Vector3.Cross(left, up);

            hat.transform.localScale = new Vector3(scale, scale, scale);
            hat.transform.rotation = Quaternion.LookRotation(front, up);
            hat.transform.localPosition = nose;
        }


        internal void OnNewPose(Transform transform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped) {

            face.SetActive(faceLandmarks.Landmark.Count > 0);

            if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);
                return;
            }

            try {
                var fps = counter.UpdateFps();

                var scale = transform.localScale;
                scale.y = scaleDepth;
                transform.localScale = scale;

                var spineSize = GetSpineSize(landmarkList);
                var points = TransformPoints(transform, landmarkList, flipped, 0, 1);


                foreach(var l in faceLandmarks.Landmark) {
                    //l.Z -= 0.3f;
                }
                float faceScale = 2;
                var faceNoseShift = CalculateZShift(transform, points, faceLandmarks, faceScale);
                var facePoints = TransformPoints(transform, faceLandmarks, flipped, faceNoseShift, faceScale);
                UpdateFace(facePoints);
                //TODO
                var ps = points.Select(x => new Vector3?(x)).ToArray();

                var t = Time.realtimeSinceStartup;
                skeletonManager.UpdatePose(ps);
                var dt = Time.realtimeSinceStartup - t;

                display.LogValue($"FPS:{fps:0.0}", dt, 0, 0, 0, 0);
            } catch (Exception ex) {
                Debug.LogError("DMBTManage new pose failed");
                Debug.LogException(ex);
            }

            newPoseEvent(true);

        }


        internal void ResetAvatar() {
 //           controller.CopyRotationAndPositionFromAvatar(initialAvatar);
        }


    }
}