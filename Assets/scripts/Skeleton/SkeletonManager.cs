using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets;
using System;
using System.Linq;
using DeepMotion.DMBTDemo;
using Assets.PoseEstimator;
using Lukso;
using static Lukso.ClothAttachementDefinition;
using static Lukso.Skeleton;

namespace Assets
{
    public class SkeletonManager : MonoBehaviour
    {

        public IkSettings ikSettings;
        public FilterSettings posFilter;
        private SkeletonSet supportedSkeletons;
        // main idea
        // different models has its own avatar
        // these avatars can share the same skeleton
        // controllerAvatars actually calculates position and other avatars just copy data from it
        private readonly Dictionary<string, Assets.Avatar> contollerAvatars = new Dictionary<string, Assets.Avatar>();
        
        public Vector3?[] RawSkeletonPoints { get; private set; }

        // Use this for initialization
        void Start() {
            Init();
        }

        // Update is called once per frame
        void Update() {

        }
        void OnValidate() {
            posFilter.SetModified();
        }


        private void Init() {
            var jsonDescr = Resources.Load<TextAsset>("skeletons").text;
            supportedSkeletons = SkeletonSet.CreateFromJSON(jsonDescr);
        }

        public Avatar GetOrCreateControllerAvatar(GameObject obj) {
            // find skeleton
            // check if this skeleton is already exist
            // add new avatar or link with existed

            var skeletonDescription = FindSkeletonType(obj);

            if (skeletonDescription == null) {
                return null;
            }

            if (contollerAvatars.ContainsKey(skeletonDescription.name)) {
                return contollerAvatars[skeletonDescription.name];
            }

            obj = Instantiate(obj, transform);
            obj.name = skeletonDescription.name + ": " + obj.name;
            obj.SetActive(false);
            var skeleton = InitNewSkeleton(skeletonDescription, obj);
            var controller = new Avatar(obj, skeleton);
            controller.settings = ikSettings;
            controller.SetIkSource();

            contollerAvatars[skeletonDescription.name] = controller;
            return controller;
        }

        private Skeleton InitNewSkeleton(SkeletonSet.Skeleton skeletonDescription, GameObject obj) {
            var children = obj.GetComponentsInChildren<Transform>();
            var scalesBones = new int[,] { { 11, 13 }, { 13, 15 }, { 12, 14 }, { 14, 16 }, { 23, 25 }, { 25, 29 }, { 24, 26 }, { 26, 30 } };
            var skeleton = CreateDefaultSkeletoStructure(skeletonDescription.name);
            skeleton.Init(obj, scalesBones, skeletonDescription);
            return skeleton;
        }

        private SkeletonSet.Skeleton FindSkeletonType(GameObject obj) {
            var children = obj.GetComponentsInChildren<Transform>();
            foreach (var descr in supportedSkeletons.skeletons) {
                if (IsSkeletonAppliable(descr, children)) {
                    return descr;
                }
            }
            Debug.LogError("Could not find supported skeleton");
            return null;
        }

        internal void RemoveNotInList(string[] usedSkeletonTypes) {
            foreach (var skeleton in contollerAvatars.Keys.ToArray()) {
                if (!usedSkeletonTypes.Contains(skeleton)) {
                    Destroy(contollerAvatars[skeleton].obj);
                    contollerAvatars[skeleton].Destroyed = true;
                    contollerAvatars.Remove(skeleton);
                }
            }
        }


        internal Avatar GetController(Skeleton skeleton) {
            Avatar res;
            if (!contollerAvatars.TryGetValue(skeleton.Name, out res)) {
                Debug.LogError("Can't find skeleton by name");
            }
            return res;
        }

        private static bool IsSkeletonAppliable(SkeletonSet.Skeleton skeleton, Transform[] nodes) {
            HashSet<Transform> usedTransforms = new HashSet<Transform>();
            foreach (var j in skeleton.description) {
                if (j.node.Length > 0) {

                    var candidateNodes = Array.FindAll(nodes.ToArray(), c => Skeleton.CompareNodeByNames(c.gameObject.name, j.node));

                    if (candidateNodes.Length == 1 || (candidateNodes.Length > 1 && j.allowMultiple)) {
                        if (usedTransforms.Contains(candidateNodes[0])) {
                            Debug.LogError($"{skeleton.name}: Found transform is already assigned to another node: node={j.node}, transform: {candidateNodes[0].name}");
                            return false;
                        }
                        usedTransforms.Add(candidateNodes[0]);

                    } else {
                        // incorrect case
                        if (candidateNodes.Length > 1) {
                            Debug.LogError($"{skeleton.name}: Too much similar nodes found for node {j.node}. Returned: {String.Join(",", candidateNodes.Select(x => x.gameObject.name))}");
                        } else {
                            Debug.LogError($"Can't find {j.node} for skeleton {skeleton.name}");
                        }

                        return false;
                    }
                }
            }

            Debug.Log("Found skeleton: {skeleton.name}: ");
            return true;
        }

        internal bool HasAnyAvatar() {
            return contollerAvatars.Count > 0;
        }

        internal Avatar GetAnyAvatar() {
            return contollerAvatars.Values.FirstOrDefault();
        }

        private Skeleton CreateDefaultSkeletoStructure(string name) {
            var skeleton = new Skeleton(name);

            
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.CHEST, 0.3f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.SPINE, 0.3f));
            skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_SHOULDER, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.LEFT_SHOULDER, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_ELBOW, 0.5f));




      skeleton.ikCalculator.Add(new Position3DParameter(Point.HIPS).SetReinit(true));
      skeleton.ikCalculator.Add(new ScalingParameter(Point.HIPS, 0.25f, (0, 0.2f)).SetReinit(true));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.HIPS, 10, (-20, 20)).SetReinit(true));
    
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.CHEST, 90, (-40, 40)));
      //skeleton.ikCalculator.Add(new Rotation3DParameter(Point.SPINE, 90, (-20, 20)));
      skeleton.ikCalculator.Add(new Stretching3DParameter(Point.CHEST, Axis.PARENT, 0.2f, (-0.1f, 0.1f)));
      //skeleton.ikCalculator.Add(new Stretching3DParameter(Point.SPINE, StretchingGradCalculator.Axis.PARENT, 0.1f, (-0.1f, 0.1f)));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.SPINE, 90f, (-20f, 20f)));

      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_HIP, 90, (-90,90)));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_KNEE));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_SHOULDER, 90, (-90, 90)));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_ELBOW, 90, (-90, 90)));

      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_HIP));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_KNEE));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_SHOULDER, 90, (-90, 90)));
      skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_ELBOW, 90, (-90, 90)));

      //  skeleton.ikCalculator.Add(new Rotation1DParameter(Point.RIGHT_KNEE, StretchingGradCalculator.Axis.Z));


      // skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_HIP));
      //skeleton.ikCalculator.Add(new Rotation1DParameter(Point.LEFT_KNEE,StretchingGradCalculator.Axis.Y));
      //skeleton.ikCalculator.Add(new Stretching3DParameter(Point.LEFT_KNEE, StretchingGradCalculator.Axis.Z));







      //            skeleton.joints.Add(new JointDefinition(Skeleton.Point.Hips, new int[] {23,24 }, new GeneralFilter(new ScaleFilter(scaleFilter), new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(-10, 10, -10, 10, 0, 359.99f), new ScalingGradCalculator()));
      skeleton.joints.Add(new JointDefinition(Skeleton.Point.HIPS, new int[] { 23, 24, 11, 12,13,14 }, new GeneralFilter(new PositionFilter(posFilter)), new Position3DGradCalculator(), new Rotation3DGradCalculator(0, 359.99f, 0, 359.99f, 0, 359.99f), new ScalingGradCalculator()));
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
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.SPINE, new int[] { 11, 12, 13, 14 }, new Rotation3DGradCalculator(-15, 15, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.3f, StretchingGradCalculator.Axis.Z)));
            // skeleton.joints.Add(new JointDefinition(Skeleton.Point.Chest, new int[] { 11, 12 }, new Rotation3DGradCalculator(-10, 10, -15, 15, -15, 15), new StretchingGradCalculator(0.9f, 1.5f, StretchingGradCalculator.Axis.Z)));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CHEST, new int[] { 11, 12,13,14 }, new StretchingGradCalculator(0.5f, 1.5f, StretchingGradCalculator.Axis.PARENT)));
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

        internal void UpdatePose(Vector3?[] ps) {
            this.RawSkeletonPoints = ps ;
            foreach (var controller in contollerAvatars.Values) {
                controller.SetIkTarget(ps);
                controller.Update(ikSettings.gradientCalcStep, ikSettings.gradientMoveStep, ikSettings.stepCount);
            }
        }

        public void PrepareClothCalculation() {
            foreach (var controller in contollerAvatars.Values) {
                controller.Update(ikSettings.gradientCalcStep, ikSettings.gradientMoveStep, ikSettings.stepCount);
            }
        }

        public void ApplyClothShift() {
            foreach (var controller in contollerAvatars.Values) {
                controller.ApplyClothShift(true);
            }
        }

        public Avatar GetClothController() {
          return contollerAvatars.Values.FirstOrDefault();

        }
    }

}
