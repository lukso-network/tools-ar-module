//using GD.MinMaxSlider;
using System;
using UnityEngine;

namespace Assets
{

    [Serializable]
    public class IkSettings
    {

        [Range(0, 5)]
        public float gradientCalcStep = 0.1f;

        [Range(0, 5)]
        public float gradientMoveStep = 5f;

        [Range(0, 1f)]
        public float rotationMoveMultiplier = 1f;

        [Range(0, 1f)]
        public float posMoveMultiplier = 0.001f;

        [Range(0, 1f)]
        public float stretchingMoveMultiplier = 1e-3f;

        [Range(0, 1f)]
        public float scaleMoveMultiplier = 0.001f;

        [Range(0, 0.5f)]
        public float clothLambdaGradient = 1f;

        [Range(0, 0.02f)]
        public float clothStepGradient = 0.1f;

        [Range(0, 2f)]
        public float clothRegularization = 0.001f;

        [Range(0, 100f)]
        public float clothPenalty = 1;

        public bool clothDemoMode = false;


        public bool resizeBones = true;
        public bool enableRot;
        public bool enableMoveHips = true;
        public bool enableAttaching = true;
        public bool chestOnly = false;

        [Range(0, 0.0005f)]
        public float levelChangeThreshold = 0.001f;

        [Range(0, 1)]
        public float gradStepScale = 0.5f;

        [Range(0, 100)]
        public float gradStepMin = 1f;

        [Range(0, 200)]
        public int stepCount = 1;

        [Range(0, 2f)]
        public float gradientThreshold = 0.001f;

        public bool stretchingEnabled = true;

        public bool useConstraints = true;

        [Header("GradientDescent")]
        [Range(0.001f, 0.2f)]
        public float gradDescentStep = 0.1f;

        [Range(0, 0.4f)]
        public float ikRegularization = 1f;

        [Range(0, 0.1f)]
        public float ikGradDescentLambda = 0.1f;

    [Range(0,2)]
    public float targetFunctionZScale = 1;
    public bool useOldIk = false;
    public bool manualIk = false;

    [Range(-2,2)]
    public float[] parametersTest;
  }


    [Serializable]
    public class BoneValidator
    {

     //   [MinMaxSlider(0.1f, 1.5f)]
        public Vector2 arm1to2 = new Vector2(0.64f, 1.3f);

       // [MinMaxSlider(0.4f, 0.8f)]
        public Vector2 arm2toSpine = new Vector2(0.4f, 0.7f);

     //   [MinMaxSlider(0.1f, 1.3f)]
        public Vector2 leg1to2 = new Vector2(0.75f, 1.05f);

     //   [MinMaxSlider(0.25f, 1.6f)]
        public Vector2 leg2toSpine = new Vector2(0.34f, 0.9f);

    }

}
