using System;
using UnityEngine;

namespace Assets
{
    public class Position3DGradCalculator : GradCalculator
    {
        public Position3DGradCalculator() {
            this.grad = new float[3];
        }
        public override void apply(Joint joint, float step, float threshold, IkSettings ikSettings) {
            if (step == 0) {
                return;
            }
        
            var pos = joint.transform.localPosition;
        //    step *= ikSettings.posMoveMultiplier;
            if (MoveByV3Gradient(ref pos, step, threshold)) {
                joint.transform.localPosition = pos;
            }
        }

        public override void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            var pos = joint.transform.localPosition;
            step *= ikSettings.posMoveMultiplier;

            if (step == 0) {
                grad[0] = grad[1] = grad[2] = 0;
                return;
            }

            for (int i = 0; i < 3; ++i) {
                pos[i] += step;
                joint.transform.localPosition = pos;

                var value = targetFunction();
                var res = (value - zeroLevel) / step;
                grad[i] = res;
                zeroLevel = value;
            }
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
