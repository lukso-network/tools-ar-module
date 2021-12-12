

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
        private Vector3[] faceNormals;
        private Vector3[] faceVertices;
        private int [] posFaceIndices;
        private int [] negFaceIndices;
        private Matrix4x4 lightMatrix;
        private Matrix4x4 lightMatrixPositive;
        private Matrix4x4 lightMatrixNegative;
        private Quaternion faceDirection = Quaternion.identity;
        public Light lightSource;

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

        protected Vector3 GetFacePointForRotation(Vector3 scaleVector, NormalizedLandmark landmark, bool isFlipped, float scale) {
            var v = ToVector3(landmark);
            var relX = (isFlipped ? -1 : 1) * (v.x - 0.5f);
            var relY = 0.5f - v.y;
            return Vector3.Scale(new Vector3(relX, relY, v.z), scaleVector);
        }

        protected Vector3 GetPositionFromNormalizedPoint(Vector3 scaleVector, Vector3 v, bool isFlipped, float zShift, float perspectiveScale, bool inZDirection = false) {
            var relX = (isFlipped ? -1 : 1) * (v.x - 0.5f);
            var relY = 0.5f - v.y;

            var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), scaleVector);// + screenTransform.position;
            var dir = (Camera.main.transform.position - pos3d).normalized;
            // dir /= Math.Abs(dir.z);
            pos3d += dir * (-(v.z + zShift)) * scaleVector.z * perspectiveScale;
            return pos3d;
        }

        private float CalculateZShift(Transform screenTransform, Vector3[] skeletonPoints, NormalizedLandmarkList faceLandmarks, bool isFlipped, float perspectiveScale) {

            if (faceLandmarks.Landmark.Count == 0) {
                return 0;
            }
            var scaleVector = ScaleVector(screenTransform);
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

            var relX = (isFlipped ? -1 : 1) * (from.x - 0.5f);
            var relY = 0.5f - from.y;
            var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), scaleVector);
            float delta = -(to.z - pos3d.z) / (Camera.main.transform.position - pos3d).normalized.z / (scaleVector.z * perspectiveScale) - from.z;


            var testP = GetPositionFromNormalizedPoint(scaleVector, from, false, delta, perspectiveScale, false);

            //Debug.Log((testP - to).z);
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
            var scaleVector = ScaleVector(transform);
            var points = new Vector3[count];
            for (int i = 0; i < count; ++i) {
                var landmark = landmarkList.Landmark[i];
                var p = GetPositionFromNormalizedPoint(scaleVector, LandmarkToVector(landmark), flipped, zShift, spineSize);
                points[i] = p;
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

            faceNormals = faceMesh.normals;
            faceVertices = faceMesh.vertices;


            posFaceIndices = faceVertices.Select((x, i) => new { item = x, index = i }).Where(x => x.item.x > 0).Select(x => x.index).ToArray();
            negFaceIndices = faceVertices.Select((x, i) => new { item = x, index = i }).Where(x => x.item.x <= 0).Select(x => x.index).ToArray();

            /*List<int> v1 = new List<int>();
            List<int> v2 = new List<int>();
            for (int i = 0; i < faceVertices.Length; ++i) {
                if (faceVertices[i].x > 0) {
                    v1.Add(i);
                } else {
                    v2.Add(i);
                }
            }*/

            InitLeastSqrMatrix();
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

        private void InitLeastSqrMatrix() {
            lightMatrixPositive = InitLeastSqrMatrix(true);
            lightMatrixNegative = InitLeastSqrMatrix(false);

            /*
            for (int i = 0; i < 4; ++i) {
                for (int j = i; j < 4; ++j) {
                    float s = 0;
                    foreach(var p in faceNormals) {
                        var n1 = i < 3 ? p[i] : 1;
                        var n2 = j < 3 ? p[j] : 1;
                        s += n1 * n2;
                    }
                    lightMatrix[i, j] = lightMatrix[j, i] = s;
                }
            }

            lightMatrix = lightMatrix.inverse;
            */
        }

        private Matrix4x4 InitLeastSqrMatrix( bool positiveSide) {

            var indices = positiveSide ? posFaceIndices : negFaceIndices;
            Matrix4x4 mt = Matrix4x4.zero;
            for (int i = 0; i < 4; ++i) {
                for (int j = i; j < 4; ++j) {
                    float s = 0;
                    foreach(var k in indices) {
                        var p = faceNormals[k];
                        var n1 = i < 3 ? p[i] : 1;
                        var n2 = j < 3 ? p[j] : 1;
                        s += n1 * n2;
                    }
                    mt[i, j] = mt[j, i] = s;
                }
            }

            mt = mt.inverse;
            return mt;
        }

        private float [] times = new float[] { 0, 0, 0, 0, 0 };

 		private Vector3[] UpdateSkeleton(Transform screenTransform, NormalizedLandmarkList landmarkList, bool flipped) {
            var t0 = Time.realtimeSinceStartup;
            var spineSize = GetSpineSize(landmarkList);
            float scale = Camera.main.aspect > 1 ? Camera.main.aspect * Camera.main.aspect : 1;
            scale /= 2.8f;
            var points = TransformPoints(screenTransform, landmarkList, flipped, 0, scale);

            //TODO make it faster
            if (flipped) {
                var fPoints = new Vector3[points.Length];
                int maxSize = Math.Min(points.Length, FLIP_POINTS.Length);
                for (int i = 0; i < maxSize; ++i) {
                    fPoints[i] = points[FLIP_POINTS[i]];
                }
                points = fPoints;
            }

            var ps = points.Select(x => new Vector3?(x)).ToArray();

            var t = Time.realtimeSinceStartup;
            skeletonManager.UpdatePose(ps);
            var dt = Time.realtimeSinceStartup - t;



            times[0] = dt;
            times[1] = t - t0;


            return points;
        }

        private void UpdateFace(Transform screenTransform, NormalizedLandmarkList faceLandmarks, bool flipped, Vector3 [] skelPoints) {

            float faceScale = Camera.main.aspect > 1 ? Camera.main.aspect * Camera.main.aspect : 1;
            var faceNoseShift = CalculateZShift(screenTransform, skelPoints, faceLandmarks, flipped, faceScale);
        
           //faceMesh.vertices = points;
            //TOFO


            var points = TransformPoints(screenTransform, faceLandmarks, flipped, faceNoseShift, faceScale);
            if (points.Length == 0) {
                return;
            }

            faceMesh.vertices = points;

            var nose = points[4];
            var t = points[10];
            var b = points[152];
            var r = points[33];
            var l = points[263];

            //Debug.Log("face:" + (l - r) * 100 + " " + (nose - (l + r) / 2)*100);

            var center = (t + b + r + l) / 4;
           // Debug.Log("Magn:" + (t - b).magnitude + " " + (l - r).magnitude);
            var scale = Mathf.Sqrt(((t - b).magnitude * (l - r).magnitude)  / defaultFaceSize);

            var up = (t - b).normalized;
            var left = (l - r).normalized;
            var front = Vector3.Cross(left, up);
            if (flipped) {
                front = -front;
            }

            faceDirection = Quaternion.LookRotation(front, up);
            hat.transform.localScale = new Vector3(scale, scale, scale);
            hat.transform.rotation = faceDirection;
            hat.transform.localPosition = nose;
        }


        internal void OnNewPose(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped, Texture2D texture) {
            face.SetActive(faceLandmarks.Landmark.Count > 0);
            var t = Time.realtimeSinceStartup;
            float t2 = 0;
            float t3 = 0;
            float t4 = 0;

            if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);

                var fps0 = counter.UpdateFps();
                display.LogValue($"FPS:{fps0:0.0}", times[0], times[1], 0,0,0);
                return;
            }
            var t1 = Time.realtimeSinceStartup;

            try {
                var scale = screenTransform.localScale;
                scale.y = scaleDepth;
                screenTransform.localScale = scale;

                var skelPoints = UpdateSkeleton(screenTransform, landmarkList, flipped);
                t2 = Time.realtimeSinceStartup;
                UpdateFace(screenTransform, faceLandmarks, flipped, skelPoints);
                t3 = Time.realtimeSinceStartup;
                CalculateLight(faceLandmarks, texture, flipped);

                t4 = Time.realtimeSinceStartup;


              //  Debug.Log("light:" + (t4 - t3));

            } catch (Exception ex) {
                Debug.LogError("DMBTManage new pose failed");
                Debug.LogException(ex);
            }

            newPoseEvent(true);
            var fps = counter.UpdateFps();
            display.LogValue($"FPS:{fps:0.0}", times[0], times[1], t1-t, t2-t1, t3-t2, t4-t3);

        }

        private void CalculateLight(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped) {
            float posMeanIntencity, negMeanIntencity;
            Vector4 res = SolveLightEquation(faceLandmarks, texture, flipped, true, out posMeanIntencity);

            var dir = new Vector3(res.x, res.y, res.z).normalized;
            Debug.Log("Face dir:" + dir + " " );
            //dir = new Vector3(0, 0, -1);

            dir = faceDirection* dir;
            lightSource.transform.rotation = Quaternion.FromToRotation(-Vector3.forward, dir);

        }




        private Vector4 SolveLightEquation(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped, bool positiveSide, out float meanIntencity) {
            int w = texture.width;
            int h = texture.height;

            //TODO
            float[] intencities = new float[faceVertices.Length];
            for (int i = 0; i < faceVertices.Length; ++i) {
                var p = faceLandmarks.Landmark[i];
                var x = (int)Mathf.Clamp((flipped ? p.X : (1 - p.X)) * w, 1, w - 1.1f);
                var y = (int)Mathf.Clamp((1 - p.Y) * h, 1, h - 1.1f);

                var c = texture.GetPixel(x, y) + texture.GetPixel(x-2, y) + texture.GetPixel(x+2, y) + texture.GetPixel(x, y-2) + texture.GetPixel(x, y+2);
                

                //  texture.SetPixel(x, y, new Color(1, 0, 0, 1));

                float intencity = (c[0] + c[1] + c[2]) / 3;
                intencities[i] = intencity;
            }

            var sorted = intencities
                        .Select((x, i) => new { Value = x, OriginalIndex = i })
                        .OrderBy(x => -x.Value)
                        .ToList();

            int count = 300;


            var indices = positiveSide ? posFaceIndices : negFaceIndices;
            Matrix4x4 mt = Matrix4x4.zero;
            for (int i = 0; i < 4; ++i) {
                for (int j = i; j < 4; ++j) {
                    float s = 0;
                    for(int k = 0; k < count; ++k) {
                        var p = faceNormals[sorted[k].OriginalIndex];
                        var n1 = i < 3 ? p[i] : 1;
                        var n2 = j < 3 ? p[j] : 1;
                        if (i==3 && j == 3) {
                          //  n1 = 1.1f;
                        }
                        s += n1 * n2;
                    }
                    mt[i, j] = mt[j, i] = s;
                }
            }

            mt = mt.inverse;
            Vector4 b = Vector4.zero;
            for (int k = 0; k < count; ++k) {
                var intencity = sorted[k].Value;

                var n = faceNormals[sorted[k].OriginalIndex];
                b.x += n.x * intencity;
                b.y += n.y * intencity;
                b.z += n.z * intencity;
                b.w += intencity;
            }

            var res = mt * b;
            meanIntencity = 0;
            return res;

        }
        private Vector4 SolveLightEquation2(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped, bool positiveSide, out float meanIntencity) {
            int count = faceLandmarks.Landmark.Count;

            int w = texture.width;
            int h = texture.height;

            Vector4 b = Vector4.zero;
            int pointCount = 0;
            float intSum = 0;
            var indices = positiveSide ? posFaceIndices : negFaceIndices;
            foreach (var i in indices) {
                var p = faceLandmarks.Landmark[i];
                var x = (int)Mathf.Clamp((flipped ? p.X : (1 - p.X)) * w, 0, w - 0.1f);
                var y = (int)Mathf.Clamp((1 - p.Y) * h, 0, h - 0.1f);

                var c = texture.GetPixel(x, y);

                //  texture.SetPixel(x, y, new Color(1, 0, 0, 1));

                float intencity = (c[0] + c[1] + c[2]) / 3;
                intSum += intencity;
                var n = faceNormals[i];
                pointCount++;
                b.x += n.x * intencity;
                b.y += n.y * intencity;
                b.z += n.z * intencity;
                b.w += intencity;
            }
            // texture.Apply();
            meanIntencity = intSum / pointCount;
            var res = positiveSide ? (lightMatrixPositive * b) : (lightMatrixNegative * b);

            var lightDir = new Vector3(res.x, res.y, res.z);

            float scale = lightDir.magnitude;
            lightDir /= scale;
            res /= scale;

            int incorrectLightCount = 0;
            foreach (var i in indices) {
                if (Vector3.Dot(faceNormals[i], lightDir ) < 0) {
                    incorrectLightCount++;
                }
            }

            
            Debug.Log("Intencity:" + meanIntencity + " " + positiveSide + " " + res + " neg:" + incorrectLightCount/(float)pointCount);
            meanIntencity = 1 - incorrectLightCount / (float)pointCount;
            return res;
        }

        internal void ResetAvatar() {
 //           controller.CopyRotationAndPositionFromAvatar(initialAvatar);
        }


    }
}