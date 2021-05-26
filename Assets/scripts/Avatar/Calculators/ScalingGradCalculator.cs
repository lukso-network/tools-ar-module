using System;
using UnityEngine;

namespace Assets
{
    public class ScalingGradCalculator : GradCalculator
    {
        private float? initValue;
        public ScalingGradCalculator() {
            this.grad = new float[1];

        }


        

        public override void apply(Joint joint, float step, float threshold, IkSettings ikSettings) {

            var pos = joint.transform.localScale;
            var p = pos.x - grad[0] * step * initValue.Value ;
            p = p < 0.5f ? 0.5f : p;
            pos.x = pos.y = pos.z = p;

            joint.transform.localScale = pos;
        }

        public override void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            if (!initValue.HasValue) {
                InitLength(joint);
            }

            step *= ikSettings.scaleMoveMultiplier;

            if (step == 0) {
                grad[0] = 0;
                return;
            }

            joint.transform.localScale += Vector3.one * step * initValue.Value;

            var value = targetFunction();
            var res = (value - zeroLevel) / step;
            grad[0] = res;

            zeroLevel = value;

        }

        private void InitLength(Joint joint) {
            initValue = joint.transform.localScale.x;
        }
    }
    /*
    public class RTJoint : Joint
    {
        public RTJoint(Transform tr) : base(tr) {
        }

        public override void InitGradient() {
            this.grad = new float[6];
        }


        internal override float CalcGradients(float zeroLevel, Func<float> targetFunction, float step, float posStep, float gradThreshold) {
            var rotation = transform.localRotation;
          
            posStep = 0.00001f;
            int i = 0;
            CalcRotGradient(ref zeroLevel, targetFunction, transform, step, ref i);
            CalcPositionGradient(ref zeroLevel, targetFunction, transform, posStep, ref i);

            return zeroLevel;
        }

        internal override void MoveByGradients(float step, float posStep, float threshold = 0) {
            var euler = transform.localRotation.eulerAngles;
            var pos = transform.localPosition;
            int j = 0;
            var minPos = initLocalPosition.y * 0.8f;
            var maxPos = initLocalPosition.y * 1.2f;
            for (int i = 0; i < grad.Length; ++i) {

                var currentStep = i < 3 ? step : posStep;
                float delta = grad[i] * currentStep;
                float absDelta = delta > 0 ? delta : -delta;
                if (absDelta > threshold) {
                    if (i < 3) {
                        euler[i] -= delta;
                        ++j;
                    } else {
                        pos.y -= delta;
                        //   pos.y = Mathf.Clamp(pos.y, minPos, maxPos);
                        ++j;
                    }
                }
            }

            if (j > 0) {
                transform.localRotation = Quaternion.Euler(euler);
                transform.localPosition = pos;
            }
        }
    }
    */
}
