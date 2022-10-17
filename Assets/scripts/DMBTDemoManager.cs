using Mediapipe;
using System;
using System.Linq;
using UnityEngine;
using UnityWeld.Binding;
using static Assets.Demo.Scripts.Utils;
using static Lukso.Skeleton;

struct Landmarks {
    public NormalizedLandmarkList lastLandmarks { get; private set; }
    public long lastValidTime { get; private set; }

    public void Set(NormalizedLandmarkList landmarks) {
        if (landmarks != null) {
            lastLandmarks = landmarks;
            lastValidTime = time();
        }
    }

    public NormalizedLandmarkList GetActualIfValid(long durationMS) {
        return IsValid(durationMS) ? lastLandmarks : null;
    }

    public bool IsValid(long durationMs) {
        return time() - lastValidTime < durationMs;
    }

    private long time() {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}


namespace Lukso {


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


        [SerializeField] private Camera screenCamera;
        [SerializeField] private Camera3DController camera3dController;

        private const long VALID_DURATION = 2000;
        private GameObject face;
        private Mesh faceMesh;

        public Mesh FaceMesh => faceMesh;
        private Quaternion faceDirection = Quaternion.identity;
        public Quaternion FaceDirection => faceDirection;

        [Range(0, 4)]
        public float scaleDepth = 0.5f;
        [Range(-20, 20)]
        public float zshift = 0.0f;

        public delegate void OnNewPoseHandler(bool skeletonExist);

        public delegate void OnNewFaceHandler(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped);

        public event OnNewPoseHandler newPoseEvent;
        public event OnNewFaceHandler newFaceEvent;

        private Texture2D lastFrame;
        private bool paused = false;
        private Vector3[] skeletonPoints;

        private Landmarks skeletonLandmarks;
        private Landmarks faceLandmarks;
        private Vector3[] cachedSkeleton;
        private float defaultFaceSize;
        private float[] times = new float[] { 0, 0, 0, 0, 0 };
        //TODO 
        private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };


        [Header("Filter params:")]
        [SerializeField] private OneEuroFilterParams zFilterParams;
        [SerializeField] private OneEuroFilterParams xyFilterParams;
        [SerializeField] private OneEuroFilterParams spineSizeFilterParams;
        [SerializeField] private OneEuroFilterParams movementFactorFilterParams;
        [SerializeField] private bool useSameParams;
        [SerializeField] private bool enableZFilter = true;
        private OneEuroFilter[] posFIlterZ;
        private OneEuroFilter[] posFIlterX;
        private OneEuroFilter[] posFIlterY;
        private OneEuroFilter spineSizeFilter;
        private OneEuroFilter movementFactorFilter;
        private Vector3[] prevPoints = new Vector3[Skeleton.JOINT_COUNT];

        private FPSCounter counter = new FPSCounter();
        private FPSCounter counterSkel = new FPSCounter();
        public bool ShowTransparentFace {
            get => face?.GetComponent<TransparentMaterialRenderer>().enabled ?? true;
            set => face.GetComponent<TransparentMaterialRenderer>().enabled = value;
        }
        public bool ShowFace {
            get => face?.GetComponent<MeshRenderer>().enabled ?? true;
            set => face.GetComponent<MeshRenderer>().enabled = value;
        }

        public bool UsePhysics { get; set; }

        public Texture2D GetLastFrame() {
            return lastFrame;
        }

        private void InitFilter() {
            posFIlterZ = new OneEuroFilter[Skeleton.JOINT_COUNT];
            posFIlterX = new OneEuroFilter[Skeleton.JOINT_COUNT];
            posFIlterY = new OneEuroFilter[Skeleton.JOINT_COUNT];

            var tempFilter = useSameParams ? xyFilterParams : zFilterParams;
            for (int i = 0; i < posFIlterZ.Length; ++i) {
                posFIlterZ[i] = new OneEuroFilter(tempFilter);
                posFIlterX[i] = new OneEuroFilter(xyFilterParams);
                posFIlterY[i] = new OneEuroFilter(xyFilterParams);
            }

            spineSizeFilter = new OneEuroFilter(spineSizeFilterParams);
            movementFactorFilter = new OneEuroFilter(movementFactorFilterParams);
        }

        void OnValidate() {
            scaleFilter.SetModified();
            InitFilter();

            Debug.Log("New filter");
        }

        void Start() {
            InitFilter();
            InitFace();
        }

        protected Vector3 ScaleVector(Transform transform) {
            return new Vector3(1 * transform.localScale.x, 1 * transform.localScale.y, transform.localScale.z);
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
            var dir = (screenCamera.transform.position - pos3d).normalized;
            if (inZDirection) {
                dir = -Vector3.forward;
            }
            // dir = -Vector3.forward;
            // dir /= Math.Abs(dir.z);
            pos3d += dir * (-(v.z + zShift)) * scaleVector.z * perspectiveScale;
            //pos3d = Vector3.Scale(new Vector3(relX, relY, v.z), scaleVector);
            return pos3d;
        }

        private float CalculateZShift(Transform screenTransform, Vector3[] skeletonPoints, NormalizedLandmarkList faceLandmarks, bool isFlipped, float perspectiveScale) {

            if (faceLandmarks == null || faceLandmarks.Landmark.Count == 0) {
                return 0;
            }
            var scaleVector = ScaleVector(screenTransform);
            var from = LandmarkToVector(faceLandmarks.Landmark[4]); //nose

            var to = skeletonPoints[0];
            //var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), ScaleVector(screenTransform)) + screenTransform.position;
            //pos3d += (screenCamera.transform.position - pos3d).normalized * (-z) * screenTransform.localScale.y * perspectiveScale;

            var relX = (isFlipped ? -1 : 1) * (from.x - 0.5f);
            var relY = 0.5f - from.y;
            var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), scaleVector);
            float delta = -(to.z - pos3d.z) / (screenCamera.transform.position - pos3d).normalized.z / (scaleVector.z * perspectiveScale) - from.z;
            return delta;
        }

        private Vector3 ToVector3(NormalizedLandmark landmark) {
            return new Vector3(landmark.X, landmark.Y, landmark.Z);
        }

        private float GetSpineSize(Vector3[] points) {

            var left = points[(int)Skeleton.Point.LEFT_HIP];
            var right = points[(int)Skeleton.Point.RIGHT_HIP];

            var leftArm = points[(int)Skeleton.Point.LEFT_SHOULDER];
            var rightArm = points[(int)Skeleton.Point.RIGHT_SHOULDER];

            //probably Vector2 needed
            var l = ((left + right) / 2 - (leftArm + rightArm) / 2);
            l.z = 0;

            return l.magnitude;
        }

        private Vector3 LandmarkToVector(NormalizedLandmark lnd) {
            return new Vector3(lnd.X, lnd.Y, lnd.Z);
        }

        public void PauseProcessing(bool pause) {
            this.paused = pause;
        }

        private Vector3[] TransformPoints(Transform transform, Vector3[] points, bool flipped, float zShift = 0, float spineSize = -1) {
            if (spineSize < 0) {
                spineSize = 1;
            }

            var scaleVector = ScaleVector(transform);

            for (int i = 0; i < points.Length; ++i) {
                var p = GetPositionFromNormalizedPoint(scaleVector, points[i], flipped, zShift, spineSize);
                points[i] = p;
            }

            return points;
        }

        private void InitFace() {
            face = Instantiate(facePrefab, transform);
            face.SetActive(false);
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

            //faceGeomCoef = Vector3.Dot(d1, d1) / Vector3.Dot(d2, d2);
        }

        private static string V2S(Vector3 v) {
            return $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";
        }

        private void RecalculateCameraPosition(Transform screenTransform, Vector3[] points, float aspectScale) {
            var scaleVector = ScaleVector(screenTransform);
            var lsh = GetPositionFromNormalizedPoint(scaleVector, points[(int)Point.LEFT_SHOULDER], false, 0, aspectScale, true);
            var rsh = GetPositionFromNormalizedPoint(scaleVector, points[(int)Point.RIGHT_SHOULDER], false, 0, aspectScale, true);
            var lh = GetPositionFromNormalizedPoint(scaleVector, points[(int)Point.LEFT_HIP], false, 0, aspectScale, true);
            var rh = GetPositionFromNormalizedPoint(scaleVector, points[(int)Point.RIGHT_HIP], false, 0, aspectScale, true);
            var l = ((lsh - lh).magnitude + (rsh - rh).magnitude) / 2;


            float targetLength = 0.4f;

            float scale = targetLength / l;
            camera3dController.CameraScale *= scale;

            var scrScale = screenTransform.localScale;
            scrScale.z = scaleDepth * scrScale.y;
            screenTransform.localScale = scrScale;
        }

        private Vector3[] UpdateSkeleton(Transform screenTransform, NormalizedLandmarkList landmarkList, bool flipped) {

            if (landmarkList == null) {
                return null;
            }

            var points = Enumerable.Range(0, landmarkList.Landmark.Count).Select(i => LandmarkToVector(landmarkList.Landmark[i])).ToArray();
            var presence = Enumerable.Range(0, landmarkList.Landmark.Count).Select(i => landmarkList.Landmark[i].Presence > 0.3f).ToArray();
            var spineSize = GetSpineSize(points);
            var timestamp = Time.realtimeSinceStartup;


            //filtering depends on size of objecs
            float filterScale = 1.15f / spineSize;
            filterScale = this.spineSizeFilter.Filter(filterScale, timestamp);

            CalculateTotalMovement(spineSize, points, presence, timestamp, filterScale);
            FilterPointPositions(points, presence, timestamp, filterScale);

            var texAspect = camera3dController.TextureAspect;
            float scale = texAspect / 1.7f;


            RecalculateCameraPosition(screenTransform, points, scale);
            TransformPoints(screenTransform, points, flipped, zshift, scale);

            //TODO make it faster
            if (flipped) {
                //TODOLK
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
            //camera3dController.SetCameraScale(1/skeletonManager.GetMainAvatarScale());

            var dt = Time.realtimeSinceStartup - t;

            times[0] = dt;
            times[1] = t - timestamp;

            return points;
        }

        private void FilterPointPositions(Vector3[] points, bool[] presence, float timestamp, float filterScale) {
            Vector3 mn = new Vector3(100, 100, 100);
            Vector3 mx = new Vector3(-100, -100, -100);
            if (enableZFilter) {
                for (int i = 0; i < points.Length; ++i) {
                    //  mx = Vector3.Max(mx, points[i]);
                    //mn = Vector3.Min(mn, points[i]);
                    mx = Vector3.Max(mx, points[i] * filterScale);
                    mn = Vector3.Min(mn, points[i] * filterScale);
                    float presenceFactor = presence[i] ? 1 : 0.01f;
                    points[i].z = posFIlterZ[i].Filter(points[i].z * filterScale, timestamp) / filterScale;
                    points[i].x = posFIlterX[i].Filter(points[i].x * filterScale, timestamp) / filterScale;
                    points[i].y = posFIlterY[i].Filter(points[i].y * filterScale, timestamp) / filterScale;
                }
            }

            //      Debug.Log("mn/mx:" + V2S(mn) + " " + V2S(mx) + "|   " + V2S(mx - mn) + " " + V2S(points[16]));
        }

        private float Squared2V(Vector3 v) {
            return v.x * v.x + v.y * v.y;
        }

        private void CalculateTotalMovement(float spineSize, Vector3[] points, bool[] presence, float timestamp, float filterScale) {
            int count = presence.Count(x => x);
            if (count == 0) {
                xyFilterParams.movementFactor = movementFactorFilter.Filter(1, timestamp);
                return;
            }

            var ds = Enumerable.Range(0, points.Length).Aggregate(Vector3.zero, (v, i) => v + (presence[i] ? (points[i] - prevPoints[i]) : Vector3.zero));

            ds.z = 0;
            ds /= count;

            var dl = Enumerable.Range(0, points.Length).Aggregate(0.0f, (v, i) => v + (presence[i] ? Mathf.Sqrt(Squared2V(points[i] - prevPoints[i])) : 0));
            dl /= count;

            ds *= filterScale;
            dl *= filterScale;

            xyFilterParams.movementFactor = movementFactorFilter.Filter(Mathf.Lerp(1, 10, dl / 0.1f / 1.5f), timestamp);
            prevPoints = (Vector3[])points.Clone();
        }

        private void UpdateFace(Transform screenTransform, NormalizedLandmarkList faceLandmarks, bool flipped, Vector3[] skelPoints) {
            if (faceLandmarks == null) {
                return;
            }

            var texAspect = camera3dController.TextureAspect;
            float faceScale = texAspect / 1.7f;
            var faceNoseShift = CalculateZShift(screenTransform, skelPoints, faceLandmarks, flipped, faceScale);

            var points = Enumerable.Range(0, faceLandmarks.Landmark.Count).Select(i => LandmarkToVector(faceLandmarks.Landmark[i])).ToArray();
            if (points.Length == 0) {
                return;
            }

            TransformPoints(screenTransform, points, flipped, faceNoseShift, faceScale);


            faceMesh.vertices = points;

            var nose = points[4];
            var t = points[10];
            var b = points[152];
            var r = points[33];
            var l = points[263];


            var center = (t + b + r + l) / 4;
            var scale = Mathf.Sqrt(((t - b).magnitude * (l - r).magnitude) / defaultFaceSize);

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
            lastFrame = texture;
            if (paused) {
                return;
            }

            if (!enabled) {
                return;
            }

            OnNewPose(screenTransform, landmarkList, faceLandmarks, flipped);
        }

        private void OnNewPose(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped) {

            bool skelModified = landmarkList != null;
            bool faceModified = faceLandmarks != null;
            this.skeletonLandmarks.Set(landmarkList);
            this.faceLandmarks.Set(faceLandmarks);

            landmarkList = this.skeletonLandmarks.GetActualIfValid(VALID_DURATION);
            faceLandmarks = this.faceLandmarks.GetActualIfValid(VALID_DURATION);

            face.SetActive(faceLandmarks != null);
            var t = Time.realtimeSinceStartup;
            float t2 = 0;
            float t3 = 0;
            float t4 = 0;

            if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);
                face.SetActive(false);

                counter.UpdateFps();
                display.LogValue($"FPS0:{counter.GetFps():0.0} {counterSkel.GetFps():0.0}", times[0], times[1], 0, 0, 0);
                return;
            }

            if (!skelModified) {
                return;
            }

            var t1 = Time.realtimeSinceStartup;

            try {
                var scale = screenTransform.localScale;
                scale.z = scaleDepth * scale.y;
                screenTransform.localScale = scale;

                var skelPoints = skelModified ? UpdateSkeleton(screenTransform, landmarkList, flipped) : cachedSkeleton;
                cachedSkeleton = skelPoints;
                t2 = Time.realtimeSinceStartup;

                if (skelModified) {
                    counterSkel.UpdateFps();
                }

                if (faceModified || skelModified) {
                    UpdateFace(screenTransform, faceLandmarks, flipped, skelPoints);
                }
                t3 = Time.realtimeSinceStartup;

                if (faceModified || skelModified) {
                    newFaceEvent(faceLandmarks, lastFrame, flipped);
                }

                t4 = Time.realtimeSinceStartup;


                //  Debug.Log("light:" + (t4 - t3));

            } catch (Exception ex) {
                Debug.LogError("DMBTManage new pose failed");
                Debug.LogException(ex);
            }

            if (skelModified) {
                newPoseEvent(true);
            }
            var fps = counter.UpdateFps();
            display.LogValue($"FPS:{counter.GetFps():0.0} {counterSkel.GetFps():0.0}", times[0], times[1], t1 - t, t2 - t1, t3 - t2, t4 - t3, Time.realtimeSinceStartup - t);
            //Debug.Log("!!!" + xyFilterParams.movementFactor);
        }

        internal void ResetAvatar() {
            //           controller.CopyRotationAndPositionFromAvatar(initialAvatar);
        }

    }
}
