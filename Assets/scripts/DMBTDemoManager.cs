

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
        public class AvatarDescription
        {
            public string id;
            public GameObject prefab;
        }

        public StatisticDisplay display;
        public FilterSettings scaleFilter;
        public SkeletonManager skeletonManager;

        [Range(0,2)]
        public float scaleDepth = 0.5f;

        public delegate void OnNewPoseHandler(bool skeletonExist);
        public event OnNewPoseHandler newPoseEvent;
        private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };

        private FPSCounter counter = new FPSCounter();

        void OnValidate() {
            scaleFilter.SetModified();
        }

        void Start() {
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
            if (!enabled || landmarkList == null) {
                newPoseEvent(false);
                return;
            }

            try {

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

                var t = Time.realtimeSinceStartup;
                skeletonManager.UpdatePose(ps);
                //controller.SetIkTarget(ps);
                //controller.Update(ikSettings.gradientCalcStep, ikSettings.gradientMoveStep, ikSettings.stepCount);
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