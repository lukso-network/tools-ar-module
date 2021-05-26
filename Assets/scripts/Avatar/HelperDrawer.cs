using DeepMotion.DMBTDemo;
using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.scripts.Avatar
{
    public class HelperDrawer : MonoBehaviour
    {
        private DMBTDemoManager skeletonManager;
        private Assets.Avatar avatar;

        private GameObject dotsRoot;
        private GameObject bonesRoot;

        public GameObject dotPrefab;
        public GameObject bonePrefab;
        public bool updateAutomatically = true;

        private bool showBody = true;
        private bool showSkeleton = true;
        private bool showLandmarks = true;
        private List<GameObject> bodies = new List<GameObject>();
        private List<Joint> joints = new List<Joint>();
        private List<Joint[]> bones = new List<Joint[]>();


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
                dotsRoot.SetActive(showSkeleton);
                bonesRoot.SetActive(showSkeleton);
            }
        }

        public bool ShowLandmarks {
            get => showLandmarks;
            set {
                showLandmarks = value;
                var annotations = FindObjectsOfType<AnnotationController>(true);
                foreach(var obj in annotations) {
                    obj.gameObject.SetActive(showLandmarks);
                }
            }
        }


        // Use this for initialization
        void Start() {
            skeletonManager = FindObjectOfType<DMBTDemoManager>();
            Init(skeletonManager.controller);

            if (updateAutomatically) {
                skeletonManager.newPoseEvent += UpdateHelpers;
            }

            //bodies.Add(skeletonManager.transform.GetChild(0).gameObject);

            var avatarManager = FindObjectOfType<AvatarManager>();
            foreach(Transform t in avatarManager.transform) {
                if (t.gameObject.activeSelf) {
                    bodies.Add(t.gameObject);
                }
            }

        }

        public void Init(Assets.Avatar avatar) {
            this.avatar = avatar;
            while(transform.childCount !=0) {
                GameObject.DestroyImmediate(transform.GetChild(0));
            }

            CreateHelpers();
        }

        private void CreateHelpers() {

            dotsRoot = new GameObject();
            bonesRoot = new GameObject();
            dotsRoot.transform.parent = transform;
            bonesRoot.transform.parent = transform;

            joints = avatar.Joints.Where(x => 
                       x.definition != null && (x.definition.pointId >= 0 || x.definition.gradCalculator != null)
            ).ToList();

            foreach(var joint in joints) {

                var obj = GameObject.Instantiate(dotPrefab, dotsRoot.transform);
                obj.name = joint.transform.name;

                var nextPivot = joint.parent;
                if (nextPivot != null && joint.transform.gameObject.name != "Hips") {
                    bones.Add(new Joint[] { joint, nextPivot });
                    var bone = GameObject.Instantiate(bonePrefab, bonesRoot.transform);
                    bone.name = $"{joint.transform.name}-{nextPivot.transform.name}";
                }
            }
        }

        public void UpdateHelpers() {
            int idx = 0;
            foreach (var joint in joints) {
                var obj = dotsRoot.transform.GetChild(idx);
                idx += 1;
                obj.transform.position = joint.transform.position;
            }

            int jdx = 0;
            foreach (var jointPair in bones) { 
                
                var p1 = jointPair[1].transform.position;
                var p2 = jointPair[0].transform.position;

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
            //TODO for debugging only
            // when paused mode is active
            if (updateAutomatically) {
                UpdateHelpers();
            }
            
        }
    }
}