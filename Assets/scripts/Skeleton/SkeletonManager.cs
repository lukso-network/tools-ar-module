using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static Lukso.ClothAttachementDefinition;
using static Lukso.Skeleton;

namespace Lukso{
    public class SkeletonManager : MonoBehaviour {

        public IkSettings ikSettings;
        public FilterSettings posFilter;
        private SkeletonSet supportedSkeletons;
        // main idea
        // different models has its own avatar
        // these avatars can share the same skeleton
        // controllerAvatars actually calculates position and other avatars just copy data from it
        private readonly Dictionary<string, Avatar> contollerAvatars = new Dictionary<string, Avatar>();

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
            foreach (var s in supportedSkeletons.skeletons) {
                foreach (var descr in s.description) {
                    descr.node = descr.node.ToLower();
                }
            }
        }

        public Avatar GetOrCreateControllerAvatar(GameObject obj, bool unique_avatar = false) {
            // find skeleton
            // check if this skeleton is already exist
            // add new avatar or link with existed

            var skeletonDescription = FindSkeletonType(obj);

            if (skeletonDescription == null) {
                return null;
            }


            var name = unique_avatar ? $"{skeletonDescription.name}_{Time.realtimeSinceStartup}" : $"{skeletonDescription.name}";
            if (contollerAvatars.ContainsKey(name)) {
                return contollerAvatars[name];
            }

            obj = Instantiate(obj, transform);
            obj.name = name + ": " + obj.name;
            obj.SetActive(false);
            var skeleton = InitNewSkeleton(skeletonDescription, obj, name);
            var controller = new Avatar(obj, skeleton);
            controller.settings = ikSettings;
            controller.SetIkSource();


            contollerAvatars[name] = controller;
            return controller;
        }

        private Skeleton InitNewSkeleton(SkeletonSet.Skeleton skeletonDescription, GameObject obj, string skeletonName) {
            var children = obj.GetComponentsInChildren<Transform>();

            var scalesBones = new (Point, Point)[] {
               ( Point.LEFT_SHOULDER, Point.LEFT_ELBOW ), 
               ( Point.LEFT_ELBOW, Point.LEFT_WRIST ),
               ( Point.RIGHT_SHOULDER, Point.RIGHT_ELBOW ), 
               ( Point.RIGHT_ELBOW, Point.RIGHT_WRIST ),
               ( Point.LEFT_HIP, Point.LEFT_KNEE ), 
               ( Point.LEFT_KNEE, Point.LEFT_HEEL ),
               ( Point. RIGHT_HIP, Point.RIGHT_KNEE ), 
               ( Point.RIGHT_KNEE, Point.RIGHT_HEEL )
             };

            var attachementBones = new (Point, Point)[] {
              //( Point.NECK, Point.HEAD ),
              ( Point.CENTER_LEFT_SHOULDER, Point.LEFT_SHOULDER ),
              ( Point.LEFT_SHOULDER, Point.LEFT_ELBOW ), 
              ( Point.LEFT_ELBOW, Point.LEFT_WRIST ),
              ( Point.CENTER_RIGHT_SHOULDER, Point.RIGHT_SHOULDER ),
              ( Point.RIGHT_SHOULDER, Point.RIGHT_ELBOW ), 
              ( Point.RIGHT_ELBOW, Point.RIGHT_WRIST ),
              ( Point.LEFT_HIP, Point.LEFT_KNEE ), 
              ( Point.LEFT_KNEE, Point.LEFT_HEEL ),
              ( Point. RIGHT_HIP, Point.RIGHT_KNEE ),
              ( Point.RIGHT_KNEE, Point.RIGHT_HEEL )
              };

            var skeleton = CreateDefaultSkeletoStructure(skeletonName);
            skeleton.Init(obj, scalesBones, attachementBones, skeletonDescription);
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

        private static bool IsRegex(string val) {
            ///(val != Regex.Escape(val));
            return val.Contains(".*") || val.Contains("$");
        }

        private static bool IsSkeletonAppliable(SkeletonSet.Skeleton skeleton, Transform[] nodes) {
            Debug.Log($"=========== Trying {skeleton.name} model ================");
            HashSet<Transform> usedTransforms = new HashSet<Transform>();
            foreach (var j in skeleton.description) {
                if (j.node.Length > 0) {
                    bool isRegexp = IsRegex(j.node);
                    var nodeName = j.node.ToLower();
                    var candidateNodes = Array.FindAll(nodes.ToArray(), c => Skeleton.CompareNodeByNames(c.gameObject.name, nodeName, isRegexp));

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

            Debug.Log($"*************************************\nFound skeleton: {skeleton.name}: ");
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

            //old
            /*
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.CHEST, 0.3f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.SPINE, 0.3f));
            skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_SHOULDER, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.LEFT_SHOULDER, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_ELBOW, 0.1f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.LEFT_ELBOW, 0.5f));
            //skeleton.clothPoints.Add(new ClothAttachmentScale(Point.SPINE, 1, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentGlobalScale(Point.SPINE, 1, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.SPINE, 2, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.CHEST, 2, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_SHOULDER, 2, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_ELBOW, 2, 0.5f));
            //      skeleton.clothPoints.Add(new Rotation3DParameter(Point.LEFT_SHOULDER, 180, (-90, 90)).SetAddRotation(true));

            /*/
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.CHEST, 0.05f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.SPINE, 0.05f));
            skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_SHOULDER, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.LEFT_SHOULDER, 0.25f));
            //skeleton.clothPoints.Add(new ClothAttachement1DNormal(Point.LEFT_ELBOW, 0.1f));
            skeleton.clothPoints.Add(new ClothAttachementMoveAlongAxis(Point.LEFT_ELBOW, 0.25f));
            //skeleton.clothPoints.Add(new ClothAttachmentScale(Point.SPINE, 1, 0.5f));
            skeleton.clothPoints.Add(new ClothAttachmentGlobalScale(Point.SPINE, 1, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.SPINE, 2, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.CHEST, 2, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_SHOULDER, 2, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_ELBOW, 2, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_HIP, 2, 0.25f));
            skeleton.clothPoints.Add(new ClothAttachmentScale(Point.LEFT_KNEE, 2, 0.25f));
            //*/






            skeleton.ikCalculator.Add(new Position3DParameter(Point.HIPS).SetReinit(true));
            skeleton.ikCalculator.Add(new ScalingParameter(Point.HIPS, 0.25f, (0, 0.2f)).SetReinit(true));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.HIPS, 10, (-20, 20)).SetReinit(true));

            //skeleton.ikCalculator.Add(new Rotation3DParameter(Point.CHEST, 90, (-40, 40)));


            // USE X axis scale small to force regularization more
            skeleton.ikCalculator.Add(new Rotation1DParameter(Point.CHEST, Axis.X, 5, (-40, 40)));
            skeleton.ikCalculator.Add(new Rotation1DParameter(Point.CHEST, Axis.Y, 40, (-40, 40)));
            skeleton.ikCalculator.Add(new Rotation1DParameter(Point.CHEST, Axis.Z, 40, (-40, 40)));
            //skeleton.ikCalculator.Add(new Rotation3DParameter(Point.SPINE, 90, (-20, 20)));
            skeleton.ikCalculator.Add(new Stretching3DParameter(Point.CHEST, Axis.PARENT, 0.2f, (-0.1f, 0.1f)));
            //skeleton.ikCalculator.Add(new Stretching3DParameter(Point.SPINE, StretchingGradCalculator.Axis.PARENT, 0.1f, (-0.1f, 0.1f)));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.SPINE, 90f, (-20f, 20f)));

            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_HIP, 90, (-90, 90)));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_KNEE));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_SHOULDER, 90, (-90, 90)));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.LEFT_ELBOW, 90, (-90, 90)));

            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_HIP));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_KNEE));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_SHOULDER, 90, (-90, 90)));
            skeleton.ikCalculator.Add(new Rotation3DParameter(Point.RIGHT_ELBOW, 90, (-90, 90)));


            skeleton.filterPoints = new Point[] { Point.HIPS, Point.CHEST, Point.SPINE };


            skeleton.joints.Add(new JointDefinition(Skeleton.Point.HIPS, Point.LEFT_HIP, Point.RIGHT_HIP, Point.LEFT_SHOULDER, Point.RIGHT_SHOULDER, Point.LEFT_ELBOW, Point.RIGHT_ELBOW));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CENTER_LEFT_SHOULDER));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CENTER_RIGHT_SHOULDER));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.NECK));
            //skeleton.joints.Add(new JointDefinition(Skeleton.Point.HEAD));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_HIP, Point.LEFT_HIP, Point.LEFT_KNEE, Point.LEFT_HEEL));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_KNEE, Point.LEFT_KNEE, Point.LEFT_HEEL));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_HEEL, Point.LEFT_HEEL));

            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_HIP, Point.RIGHT_HIP, Point.RIGHT_KNEE, Point.RIGHT_HEEL));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_KNEE, Point.RIGHT_KNEE, Point.RIGHT_HEEL));
            //TODO ankle in blender == heel in mediapipe
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_HEEL, Point.RIGHT_HEEL));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.SPINE, Point.LEFT_SHOULDER, Point.RIGHT_SHOULDER, Point.LEFT_ELBOW, Point.RIGHT_ELBOW));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.CHEST, Point.LEFT_SHOULDER, Point.RIGHT_SHOULDER, Point.LEFT_ELBOW, Point.RIGHT_ELBOW));

            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_SHOULDER, Point.LEFT_SHOULDER, Point.LEFT_ELBOW, Point.LEFT_WRIST));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_ELBOW, Point.LEFT_ELBOW, Point.LEFT_WRIST));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.LEFT_WRIST, Point.LEFT_WRIST));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_SHOULDER, Point.RIGHT_SHOULDER, Point.RIGHT_ELBOW, Point.RIGHT_WRIST));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_ELBOW, Point.RIGHT_ELBOW, Point.RIGHT_WRIST));
            skeleton.joints.Add(new JointDefinition(Skeleton.Point.RIGHT_WRIST, Point.RIGHT_WRIST));//?
            return skeleton;
        }

        internal void UpdatePose(Vector3?[] ps, Quaternion? headRotation) {
            this.RawSkeletonPoints = ps;

            foreach (var controller in contollerAvatars.Values) {
                controller.SetIkTarget(ps, headRotation);
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
