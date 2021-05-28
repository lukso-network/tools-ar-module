﻿

using Assets;
using Assets.Demo.Scripts;
using Assets.PoseEstimator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Joint = Assets.Joint;

public class Skeleton
{
    public List<JointDefinition> joints = new List<JointDefinition>();
    private GameObject[] jointBones;
    private int[] keyPointsIds;

    public JointDefinition GetByName(string name) {
        // used in initialization. Performance is not the matter
        return joints.Where(x => Utils.CompareNodeByName(name, x.name)).FirstOrDefault();
    }

    public bool HasKeyPoint(int id) {
        return jointBones[id] != null;
    }

    internal void Init(GameObject obj) {
        jointBones = new GameObject[joints.Count];
        var children = obj.GetComponentsInChildren<Transform>();

        List<int> ids = new List<int>();
        foreach (var j in joints) {
            //if (j.Value >= 0 && (j.Value == 16 || j.Value == 15)) {
            //if (j.Value >= 0 && (j.Value == 16)) {
            if (j.pointId >= 0) {
                this.jointBones[j.pointId] = Array.Find(children, c => Utils.CompareNodeByName(c.gameObject.name, j.name))?.gameObject;
                ids.Add(j.pointId);
            }
        }
        ids.Sort();
        this.keyPointsIds = ids.ToArray();

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

    [Serializable]
    public class AvatarDescription
    {
        public string id;
        public GameObject prefab;
    }

    [Binding]
    public class DMBTDemoManager : MonoBehaviour {
        public string avatarType = "female";
        public AvatarDescription[] avatars = new AvatarDescription[0];

        [HideInInspector]
        public Assets.Avatar controller;

        private Assets.Avatar initialAvatar;

        public StatisticDisplay display;
        private PoseScaler poseScaler;
        public IkSettings ikSettings;
        public FilterSettings scaleFilter;
        public FilterSettings posFilter;

        public delegate void OnNewPoseHandler();
        public event OnNewPoseHandler newPoseEvent;
        private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };

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
            Utils.PreparePivots(obj);
            controller = new Assets.Avatar(obj, CreateSkeleton(obj));
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
            return new Vector3(1 * transform.localScale.x, 1 * transform.localScale.z, 1);
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
                for (int i = 0; i < FLIP_POINTS.Length; ++i) { 
                    fPoints[i] = points[FLIP_POINTS[i]];
                }
                points = fPoints;
            }

            return points;
        }

        internal void OnNewPose(Transform transform, NormalizedLandmarkList landmarkList, bool flipped) {
            if (!enabled) {
                return;
            }

            var points = TransformPoints(transform, landmarkList, flipped);
            //TODO
            var ps = points.Select(x => new Vector3?(x)).ToArray();
            controller.SetIkTarget(ps);

            var t = Time.realtimeSinceStartup;
            controller.Update(ikSettings.gradientCalcStep, ikSettings.gradientMoveStep, ikSettings.stepCount);
            var dt = Time.realtimeSinceStartup - t;

            display.LogValue(dt, 0, 0, 0, 0);

            newPoseEvent();

        }

        private Skeleton CreateSkeleton(GameObject obj) {
            var skeleton = new Skeleton();

            skeleton.joints.Add(new JointDefinition("Hips", -1, new GeneralFilter(new ScaleFilter(scaleFilter), new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(-10, 10, -10, 10, 0, 359.99f), new ScalingGradCalculator()));
            skeleton.joints.Add(new JointDefinition("Left leg", 23, new Rotation3DGradCalculator(-70, 15, -70, 70, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition("Left knee", 25, new Rotation1DGradCalculator(-5, 140, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Left ankle", 27, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition("Left toe", -29));
            skeleton.joints.Add(new JointDefinition("Left toe_end", -31));
            skeleton.joints.Add(new JointDefinition("Right leg", 24, new Rotation3DGradCalculator(-15, 70, -70, 70, -30, 30), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition("Right knee", 26, new Rotation1DGradCalculator(-140, 5, Rotation1DGradCalculator.Axis.Y), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Right ankle", 28, new StretchingGradCalculator(0.7f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
            skeleton.joints.Add(new JointDefinition("Right toe", -30));
            skeleton.joints.Add(new JointDefinition("Right toe_end", -32));
            skeleton.joints.Add(new JointDefinition("Spine", -1, new Rotation3DGradCalculator(-15, 15, -15, 15, -15, 15)));//, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Chest", -1, new Rotation3DGradCalculator(-10, 10, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Left shoulder", -1));//, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Left arm", 11, new Rotation3DGradCalculator(-85, 80, -15, 120, -115, 85), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Left elbow", 13, new Rotation1DGradCalculator(0, 140, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition("Left wrist", 15, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
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
            skeleton.joints.Add(new JointDefinition("Neck", -1, new Rotation3DGradCalculator(-25, 25, -25, 25, -25, 25)));
            skeleton.joints.Add(new JointDefinition("Head", -1));
            skeleton.joints.Add(new JointDefinition("Eye_L", -1));
            skeleton.joints.Add(new JointDefinition("Eye_L_end", -1));
            skeleton.joints.Add(new JointDefinition("Eye_R", -1));
            skeleton.joints.Add(new JointDefinition("Eye_R_end", -1));
            skeleton.joints.Add(new JointDefinition("Right shoulder", -1));//, new Rotation1DGradCalculator(-15, 15, Rotation1DGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Right arm", 12, new Rotation3DGradCalculator(-85, 80, -120, 15, -55, 115), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition("Right elbow", 14, new Rotation1DGradCalculator(-140, 0, Rotation1DGradCalculator.Axis.Z), new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.Y)));
            skeleton.joints.Add(new JointDefinition("Right wrist", 16, new StretchingGradCalculator(0.9f, 1.1f, StretchingGradCalculator.Axis.PARENT)));
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


            skeleton.Init(obj);
         
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