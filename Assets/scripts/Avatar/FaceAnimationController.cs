using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
using System.Linq;

namespace Lukso {
    public class FaceAnimationController : MonoBehaviour {

        private DMBTDemoManager posManager;
        private Dictionary<string, BlendShapeKey> shapes = new Dictionary<string, BlendShapeKey>();
        private VRMBlendShapeProxy blenderProxy = null;

        private BlendShapeKey leftEyeShape;
        private BlendShapeKey rightEyeShape;
        private BlendShapeKey blinkShape;
        private BlendShapeKey mouthOShape;
        private BlendShapeKey mouthAShape;
        private const string LEFT_EYE_KEY = "Blink_L";
        private const string RIGHT_EYE_KEY = "Blink_R";
        private const string BLINK_KEY = "Blink";
        private const string MOUTH_O_SHAPE = "O";
        private const string MOUTH_A_SHAPE = "A";

        private Dictionary<string, float> initialStates = new Dictionary<string, float>();
        private bool isInInitialState = true;


        private readonly int[] leftEyePointsIdx = { 362, 385, 387, 263, 373, 380 };
        private readonly int[] rightEyePointsIdx = { 33, 160, 158, 133, 153, 144 };
        private readonly int[] mouthPointsIdx = { 13, 14, 62, 308 };
        void Start() {
            posManager = FindObjectOfType<DMBTDemoManager>();
            posManager.newFaceEvent += OnNewFace;

            InitBlendShapes();
        }

        private void OnDestroy() {
            posManager.newFaceEvent -= OnNewFace;
        }

        private void InitBlendShapes() {
            blenderProxy = GetComponent<VRMBlendShapeProxy>();
            shapes = new Dictionary<string, BlendShapeKey>();
            foreach (var i in blenderProxy.GetValues()) {
                //Debug.Log($"{i.Key} - {i.Value}");
                shapes[i.Key.Name] = i.Key;
            }

            leftEyeShape = shapes[LEFT_EYE_KEY];
            rightEyeShape = shapes[RIGHT_EYE_KEY];
            blinkShape = shapes[BLINK_KEY];
            mouthOShape = shapes[MOUTH_O_SHAPE];
            mouthAShape = shapes[MOUTH_A_SHAPE];

            initialStates[BLINK_KEY] = blenderProxy.GetValue(BLINK_KEY);
            initialStates[MOUTH_O_SHAPE] = blenderProxy.GetValue(MOUTH_O_SHAPE);
            initialStates[MOUTH_A_SHAPE] = blenderProxy.GetValue(MOUTH_A_SHAPE);
            
        }
        // Update is called once per frame
        void Update() {

        }

        private Vector3 LandmarkToVector(NormalizedLandmark lnd) {
            return new Vector3(lnd.X, lnd.Y, lnd.Z);
        }

        private float CalculateEyeEAR(Vector3[] points) {
            float v = ((points[1] - points[5]).magnitude + (points[2] - points[4]).magnitude) / 2 / (points[0] - points[3]).magnitude;
            //Debug.Log(v);
            return Mathf.Clamp01(1 - (v - 0.25f) / 0.1f);
        }
        

        public void OnNewFace(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped) {
            
            if (!posManager.enableFaceAnimation) {
                if (!isInInitialState) {
                    blenderProxy.ImmediatelySetValue(blinkShape, initialStates[BLINK_KEY]);
                    blenderProxy.ImmediatelySetValue(mouthOShape, initialStates[MOUTH_O_SHAPE]);
                    blenderProxy.ImmediatelySetValue(mouthAShape, initialStates[MOUTH_A_SHAPE]);
                    isInInitialState = true;
                }
                return;
            }

            isInInitialState = false;
            Vector3[] leftPoints = leftEyePointsIdx.Select(x => { var v = LandmarkToVector(faceLandmarks.Landmark[x]); v.z = 0; return v; }).ToArray();
            Vector3[] rightPoints = rightEyePointsIdx.Select(x => { var v = LandmarkToVector(faceLandmarks.Landmark[x]); v.z = 0; return v; }).ToArray();
            Vector3[] mouthPoints = mouthPointsIdx.Select(x => { var v = LandmarkToVector(faceLandmarks.Landmark[x]); v.z = 0; return v; }).ToArray();

            var leftEAR = CalculateEyeEAR(leftPoints);
            var rightEAR = CalculateEyeEAR(rightPoints);
            var blink = (leftEAR + rightEAR) / 2;

            //Debug.Log("!!" + leftEAR + "  " + rightEAR);
            blenderProxy.ImmediatelySetValue(blinkShape, blink);


            var mouthSize = (mouthPoints[0] - mouthPoints[1]).magnitude / (mouthPoints[2] - mouthPoints[3]).magnitude;
            mouthSize = Mathf.Clamp01(mouthSize * 2);
            blenderProxy.ImmediatelySetValue(mouthOShape, mouthSize);
            blenderProxy.ImmediatelySetValue(mouthAShape, mouthSize);
            //blenderProxy.ImmediatelySetValue(leftEyeShape, leftEAR);
            //blenderProxy.ImmediatelySetValue(rightEyeShape, rightEAR);
            //blenderProxy.SetValue(leftEyeShape, leftEar / 0.28f);
            //blenderProxy.Apply();
        }   
    }
}