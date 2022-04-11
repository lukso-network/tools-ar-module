using DeepMotion.DMBTDemo;
using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Skeleton = Lukso.Skeleton;

namespace Assets.scripts.Avatar
{
    public class HelperDrawer : MonoBehaviour
    {
        private Assets.Avatar avatar;
        private SkeletonManager skeletonManager;

        private GameObject dotsRoot;
        private GameObject rawPointsRoot;
        private GameObject rawPointBonesRoot;
        private GameObject bonesRoot;

        public GameObject dotPrefab;
        public GameObject bonePrefab;
        public GameObject axis;
        public bool updateAutomatically = true;
        public bool showRawPoints;

        public bool showBody = true;
        public bool showSkeleton = false;
        public bool showLandmarks = false;
        private List<GameObject> bodies = new List<GameObject>();
        private List<Joint> joints = new List<Joint>();
        private List<Joint[]> bones = new List<Joint[]>();

        private readonly Skeleton.Point[,] SKELETON_BONES = new Skeleton.Point[,] {
                {Skeleton.Point.HIPS, Skeleton.Point.LEFT_HIP },
                {Skeleton.Point.HIPS, Skeleton.Point.RIGHT_HIP },
                {Skeleton.Point.LEFT_HIP, Skeleton.Point.LEFT_KNEE },
                {Skeleton.Point.RIGHT_HIP, Skeleton.Point.RIGHT_KNEE },
                {Skeleton.Point.LEFT_KNEE, Skeleton.Point.LEFT_HEEL },
                {Skeleton.Point.RIGHT_KNEE, Skeleton.Point.RIGHT_HEEL },

                {Skeleton.Point.HIPS, Skeleton.Point.SPINE },
                {Skeleton.Point.SPINE, Skeleton.Point.CHEST },
                {Skeleton.Point.CHEST, Skeleton.Point.LEFT_SHOULDER },
                {Skeleton.Point.CHEST, Skeleton.Point.RIGHT_SHOULDER },
                {Skeleton.Point.LEFT_SHOULDER, Skeleton.Point.LEFT_ELBOW },
                {Skeleton.Point.RIGHT_SHOULDER, Skeleton.Point.RIGHT_ELBOW },
                {Skeleton.Point.LEFT_ELBOW, Skeleton.Point.LEFT_WRIST },
                {Skeleton.Point.RIGHT_ELBOW, Skeleton.Point.RIGHT_WRIST },
            };

        public bool ShowBody { 
            get => showBody; 
            set { 
                foreach(var b in bodies) {
                    b.SetActive(value);
                }
                showBody = value;
           } 
        }

        public bool ShowSkeleton {
            get => showSkeleton;
            set {
				        showSkeleton = value;
                gameObject.SetActive(showSkeleton);
            }
        }

        public bool ShowLandmarks {
            get => showLandmarks;
            set {
                showLandmarks = value;
                //TODOLKvar annotations = FindObjectsOfType<AnnotationController>(true);
                //TODOLKforeach(var obj in annotations) {
                    //TODOLKobj.gameObject.SetActive(showLandmarks);
                //TODOLK}
            }
        }


        // Use this for initialization
        void Start() {
            skeletonManager = FindObjectOfType<SkeletonManager>();

            if (updateAutomatically) {
                var poseManager = FindObjectOfType<DMBTDemoManager>();
                poseManager.newPoseEvent += UpdateHelpers;
            }
            ShowSkeleton = false;
        }

        private void InitAvatar() { 
            
            Init(skeletonManager.GetAnyAvatar());

            //bodies.Add(skeletonManager.transform.GetChild(0).gameObject);

            var avatarManager = FindObjectOfType<AvatarManager>();
            foreach(Transform t in avatarManager.transform) {
                if (t.gameObject.activeSelf) {
                    bodies.Add(t.gameObject);
                }
            }

            ShowLandmarks = showLandmarks;
            ShowSkeleton = showSkeleton;
            ShowBody = showBody;

        }

        public void Init(Assets.Avatar avatar) {
            this.avatar = avatar;

            foreach(Transform t in transform) {
                GameObject.Destroy(t.gameObject);
            }
            
            CreateHelpers();
        }

        private void CreateHelpers() {

            dotsRoot = new GameObject("dots root");
            bonesRoot = new GameObject("bones root");
            rawPointsRoot = new GameObject("rawPoints root");
            rawPointBonesRoot = new GameObject("rawPointBones root");
            dotsRoot.transform.parent = transform;
            bonesRoot.transform.parent = transform;
            rawPointsRoot.transform.parent = transform;
            rawPointBonesRoot.transform.parent = transform;

            joints = avatar.Joints.Where(x => 
                       x.definition != null && (x.definition.pointId >= 0 || x.definition.gradCalculator != null)
            ).ToList();

            for (int i = 0; i < Skeleton.POINT_COUNT;++i) {
              var obj = GameObject.Instantiate(dotPrefab, rawPointsRoot.transform);
              var type = (Skeleton.Point)i;
              obj.name = $"{type}-{i}";
            }

            for (int i = 0; i < SKELETON_BONES.GetLength(0); ++i) {
              var from = SKELETON_BONES[i, 0];
              var to = SKELETON_BONES[i, 1];
              if ((int)from < 0 || (int)to < 0) {
                continue;
              }
              var bone = GameObject.Instantiate(bonePrefab, rawPointBonesRoot.transform);
              bone.name = $"{from}-{to}";
            }



      foreach (var joint in joints) {

                var obj = GameObject.Instantiate(dotPrefab, dotsRoot.transform);
                obj.name = $"{joint.transform.name}({joint.definition.point})";
                bones.Clear();
                var nextPivot = joint.parent;
                if (nextPivot != null && joint.transform.gameObject.name != "Hips") {
                    bones.Add(new Joint[] { joint, nextPivot });
                    var bone = GameObject.Instantiate(bonePrefab, bonesRoot.transform);
                    bone.name = $"{joint.transform.name}-{nextPivot.transform.name}";
                }
            }

            for(int i = 0; i < SKELETON_BONES.GetLength(0); ++i) {
                var from = SKELETON_BONES[i, 0];
                var to = SKELETON_BONES[i, 1];
                var jointFrom = joints.Where(x => x.definition.point == from).FirstOrDefault();
                var jointTo = joints.Where(x => x.definition.point == to).FirstOrDefault();

                if (jointFrom != null && jointTo != null) {
                    bones.Add(new Joint[] { jointFrom, jointTo });
                    var bone = GameObject.Instantiate(bonePrefab, bonesRoot.transform);
                    bone.name = $"{jointFrom.transform.name}-{jointTo.transform.name}";
                }
            }
        }

        public void UpdateHelpers(bool skeletonExist) {

            if (avatar == null) {
                return;
            }

            if (avatar.Destroyed) {
                avatar = null;
                foreach (Transform t in transform) {
                    GameObject.Destroy(t.gameObject);
                }
                return;
            }


            for (int i = 0; i < Skeleton.POINT_COUNT; ++i) {
              var obj = rawPointsRoot.transform.GetChild(i);
              obj.transform.position = skeletonManager.RawSkeletonPoints[i] ?? Vector3.zero;
            }

      int jdx = 0;
            for (int i = 0; i < SKELETON_BONES.GetLength(0); ++i) {
              var from = SKELETON_BONES[i, 0];
              var to = SKELETON_BONES[i, 1];
              if ((int)from < 0 || (int)to < 0) {
                continue;
              }
              var p1 = skeletonManager.RawSkeletonPoints[(int)from] ?? Vector3.zero;
              var p2 = skeletonManager.RawSkeletonPoints[(int)to] ?? Vector3.zero;
              var bone = rawPointBonesRoot.transform.GetChild(jdx++);

              var rot = Quaternion.FromToRotation(Vector3.up, (p2 - p1));
              var scale = (p2 - p1).magnitude;

              bone.transform.rotation = rot;
              bone.transform.localScale = new Vector3(scale, scale, scale);
              bone.transform.position = p1;
            }


      int idx = 0;
            foreach (var joint in joints) {
                var obj = dotsRoot.transform.GetChild(idx);
                idx += 1;
                obj.transform.position = joint.transform.position;
                obj.transform.rotation = joint.transform.rotation;
            }

            jdx = 0;
            foreach (var jointPair in bones) {
                var p1 = jointPair[0].transform.position;
                var p2 = jointPair[1].transform.position;
                

                var rot = Quaternion.FromToRotation(Vector3.up, (p2 - p1));
                var scale = (p2 - p1).magnitude;
                    
                var bone = bonesRoot.transform.GetChild(jdx);
                ++jdx;
                bone.transform.rotation = rot;
                bone.transform.localScale = new Vector3(scale, scale, scale);
                bone.transform.position = p1;

            }
        }

        // Update is called once per frame
        void Update() {
            
            if (avatar == null && skeletonManager.HasAnyAvatar()) {
                InitAvatar();
            }

            if (avatar != null) {
                rawPointsRoot.SetActive(showRawPoints);
                rawPointBonesRoot.SetActive(showRawPoints);
                //TODO for debugging only
                // when paused mode is active
                if (updateAutomatically) {
                    UpdateHelpers(true);
                }
            }
            
        }
    }
}
