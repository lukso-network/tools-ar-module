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


        public bool enableFaceAnimation = false;
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

        private float CalculateZShift(Vector3 pointScaler, Vector3[] skeletonPoints, Vector3 [] facePoints, bool isFlipped, float perspectiveScale) {

            if (facePoints == null) {
                return 0;
            }
            const int FACE_NOSE_ID = 4;
            var from = facePoints[FACE_NOSE_ID];

            var to = skeletonPoints[(int)Point.NOSE];
            //var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), ScaleVector(screenTransform)) + screenTransform.position;
            //pos3d += (screenCamera.transform.position - pos3d).normalized * (-z) * screenTransform.localScale.y * perspectiveScale;

            var relX = (isFlipped ? -1 : 1) * (from.x - 0.5f);
            var relY = 0.5f - from.y;
            var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), pointScaler);
            float delta = -(to.z - pos3d.z) / (screenCamera.transform.position - pos3d).normalized.z / (pointScaler.z * perspectiveScale) - from.z;
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

        private Vector3[] TransformPoints(Vector3 pointScaler, Vector3[] points, bool flipped, float zShift = 0, float spineSize = -1) {
            if (spineSize < 0) {
                spineSize = 1;
            }

            for (int i = 0; i < points.Length; ++i) {
                var p = GetPositionFromNormalizedPoint(pointScaler, points[i], flipped, zShift, spineSize);
                points[i] = p;
            }

            return points;
        }

        private (Vector3, Vector3, Vector3, Vector3, Vector3) GetFaceMainPoints(Vector3 []points){
            var nose = points[4];
            var t = points[10];
            var b = points[152];
            var r = points[33];
            var l = points[263];
            return (nose, l, r, t, b);
        }

        private void InitFace() {
            face = Instantiate(facePrefab, transform);
            face.SetActive(false);
            var tr = face.AddComponent<TransparentMaterialRenderer>();
            tr.Init();

            faceMesh = face.GetComponent<MeshFilter>().mesh;
            faceMesh.RecalculateNormals();

            var points = faceMesh.vertices;

            var (nose, l, r, t, b) = GetFaceMainPoints(points);
            defaultFaceSize = ((t - b).magnitude * (l - r).magnitude);

            var c0 = (r + l) / 2;
            var d1 = nose - c0;
            var d2 = (l - r);

            //faceGeomCoef = Vector3.Dot(d1, d1) / Vector3.Dot(d2, d2);
        }

        private static string V2S(Vector3 v) {
            return $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";
        }

        private Vector3 CalculatePointScaler() {
            var pointScaler = camera3dController.GetScreenTransform().localScale;
            pointScaler.z = scaleDepth * pointScaler.y;
            return pointScaler;

        }

        private Vector3 RecalculateCameraPosition(Vector3 pointScaler, Vector3[] points, float aspectScale) {
            var lsh = GetPositionFromNormalizedPoint(pointScaler, points[(int)Point.LEFT_SHOULDER], false, 0, aspectScale, true);
            var rsh = GetPositionFromNormalizedPoint(pointScaler, points[(int)Point.RIGHT_SHOULDER], false, 0, aspectScale, true);
            var lh = GetPositionFromNormalizedPoint(pointScaler, points[(int)Point.LEFT_HIP], false, 0, aspectScale, true);
            var rh = GetPositionFromNormalizedPoint(pointScaler, points[(int)Point.RIGHT_HIP], false, 0, aspectScale, true);
            var l = ((lsh - lh).magnitude + (rsh - rh).magnitude) / 2;


            float targetLength = 0.4f;

            float scale = targetLength / l;
            camera3dController.CameraScale *= scale;

            return CalculatePointScaler();
        }

        private float GetOrientationCorrectionScale() {
            var texAspect = camera3dController.TextureAspect;
            float scale = texAspect / 1.7f;
            return scale;
        }

        private Vector3[] UpdateSkeleton(ref Vector3 pointScaler, Vector3 [] points, bool[] presence, Vector3[] facePoints, bool flipped) {


            var spineSize = GetSpineSize(points);
            var timestamp = Time.realtimeSinceStartup;

            //filtering depends on size of objecs
            float filterScale = 1.15f / spineSize;
            filterScale = this.spineSizeFilter.Filter(filterScale, timestamp);

            CalculateTotalMovement(spineSize, points, presence, timestamp, filterScale);
            FilterPointPositions(points, presence, timestamp, filterScale);


            pointScaler = RecalculateCameraPosition(pointScaler, points, GetOrientationCorrectionScale());
            TransformPoints(pointScaler, points, flipped, zshift, GetOrientationCorrectionScale());

            //TODO make it faster
            if (flipped) {
                //TODO
                var fPoints = new Vector3[points.Length];
                int maxSize = Math.Min(points.Length, FLIP_POINTS.Length);
                for (int i = 0; i < maxSize; ++i) {
                    fPoints[i] = points[FLIP_POINTS[i]];
                }
                points = fPoints;
            }

            return points;
        }

        private Quaternion? CalculateHeadPoint(Vector3[] points, bool[] presence, Vector3 [] facePoints, bool flipped) {
                
            if (facePoints == null) {
                return null;
            }

            var (faceNose, l, r, t, b) = GetFaceMainPoints(facePoints);

            if (!flipped) {
                (l, r) = (r, l);
            }

            // As we see mirrored image we need to transform to mirrored coordinates (or we need to recalculate quaternion from global to local coordinates relative to neck
            l.x = -l.x;
            r.x = -r.x;
            t.x = -t.x;
            b.x = -b.x;

            //swing z depth scale
            var s = -2;
            l.z *= s;
            r.z *= s;
            t.z *= s;
            b.z *= s;
            

            

            var dirZ = (t - b).normalized;
            var q1 = Quaternion.FromToRotation(Vector3.up, dirZ);

            var dirXTransformed = q1 * (Vector3.right);
            var dirX = (l - r).normalized;
            var q2 = Quaternion.FromToRotation(dirXTransformed, dirX); // mirrored transofrmation
            return q2*q1;
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

        private void UpdateFace(Vector3 pointScaler, Vector3 [] facePoints, bool flipped, Vector3[] skelPoints) {
            if (facePoints == null) {
                return;
            }

            var faceNoseShift = CalculateZShift(pointScaler, skelPoints, facePoints, flipped, GetOrientationCorrectionScale());

            TransformPoints(pointScaler, facePoints, flipped, faceNoseShift, GetOrientationCorrectionScale());


            faceMesh.vertices = facePoints;

            var (nose, l, r, t, b) = GetFaceMainPoints(facePoints);

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

            OnNewPose( landmarkList, faceLandmarks, flipped);
        }

        private void OnNewPose(NormalizedLandmarkList landmarkListOrig, NormalizedLandmarkList faceLandmarksOrig, bool flipped) {

            var pointScaler = CalculatePointScaler();
            bool skelModified = landmarkListOrig != null;
            bool faceModified = faceLandmarksOrig != null;
            this.skeletonLandmarks.Set(landmarkListOrig);
            this.faceLandmarks.Set(faceLandmarksOrig);

            var landmarkList = this.skeletonLandmarks.GetActualIfValid(VALID_DURATION);
            var faceLandmarks = this.faceLandmarks.GetActualIfValid(VALID_DURATION);

            face.SetActive(faceLandmarks != null);

            if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);
                face.SetActive(false);

                counter.UpdateFps();
                display.LogValue($"FPS0:{counter.GetFps():0.0} {counterSkel.GetFps():0.0}", 0, 0, 0, 0, 0);
                return;
            }

            if (!skelModified) {
                return;
            }


            var t = Time.realtimeSinceStartup;
            float t2 = 0;
            float t3 = 0;
            float t4 = 0;
            float t5 = 0;
            var t1 = Time.realtimeSinceStartup;

            try {

                var points = Enumerable.Range(0, landmarkList.Landmark.Count).Select(i => LandmarkToVector(landmarkList.Landmark[i])).ToArray();
                var presence = Enumerable.Range(0, landmarkList.Landmark.Count).Select(i => landmarkList.Landmark[i].Presence > 0.3f).ToArray();
                var facePoints = faceLandmarks == null ? null : Enumerable.Range(0, faceLandmarks.Landmark.Count).Select(i => LandmarkToVector(faceLandmarks.Landmark[i])).ToArray();

                var skelPoints = UpdateSkeleton(ref pointScaler, points, presence, facePoints, flipped);
                cachedSkeleton = skelPoints;
                t2 = Time.realtimeSinceStartup;

                if (skelModified) {
                    counterSkel.UpdateFps();
                }

                if (faceModified || skelModified) {
                    UpdateFace(pointScaler, facePoints, flipped, skelPoints);
                }

                t3 = Time.realtimeSinceStartup;

                var headRotation = CalculateHeadPoint(skelPoints, presence, facePoints, flipped);
                var ps = skelPoints.Select(x => new Vector3?(x)).ToArray();
                skeletonManager.UpdatePose(ps, headRotation);

                t4 = Time.realtimeSinceStartup;

                if (faceModified || skelModified) {
                    newFaceEvent(faceLandmarks, lastFrame, flipped);
                }

                t5 = Time.realtimeSinceStartup;


                //  Debug.Log("light:" + (t4 - t3));

            } catch (Exception ex) {
                Debug.LogError("DMBTManage new pose failed");
                Debug.LogException(ex);
            }

            if (skelModified) {
                newPoseEvent(true);
            }
            var fps = counter.UpdateFps();
            display.LogValue($"FPS:{counter.GetFps():0.0} {counterSkel.GetFps():0.0}", t1 - t, t2 - t1, t3 - t2, t4 - t3, t5-t4, Time.realtimeSinceStartup - t);
            //Debug.Log("!!!" + xyFilterParams.movementFactor);
        }

        internal void ResetAvatar() {
            //           controller.CopyRotationAndPositionFromAvatar(initialAvatar);
        }

    }
}
