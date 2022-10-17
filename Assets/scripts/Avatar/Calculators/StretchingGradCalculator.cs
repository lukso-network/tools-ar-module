using System;

namespace Assets {
    public class StretchingGradCalculator : GradCalculator {
        public enum Axis : int {
            X, Y, Z, PARENT
        }

        private float? initValue;
        private readonly Axis axis;
        public StretchingGradCalculator(Axis axis, Constraint constraint) : base(constraint) {
            this.grad = new float[1];
            this.axis = axis;
        }

        public StretchingGradCalculator(float minX, float maxX, StretchingGradCalculator.Axis axis) : this(axis, new StretchingConstraint(minX, maxX, axis)) {
        }

        private void InitLength(Joint joint) {
            var pos = joint.transform.localPosition;
            if (axis == Axis.PARENT) {
                initValue = pos.magnitude;
            } else {
                initValue = pos[(int)axis];
            }
        }
        public override void apply(Joint joint, float step, float threshold, IkSettings ikSettings) {

            //  return;
            if (!ikSettings.stretchingEnabled) {
                return;
            }
            //   step *= ikSettings.stretchingMoveMultiplier;
            var pos = joint.transform.localPosition;

            if (axis == Axis.PARENT) {
                var l = pos.normalized;
                pos += -grad[0] * l * step * initValue.Value;
            } else {
                pos[(int)axis] -= grad[0] * step * initValue.Value;
            }
            joint.transform.localPosition = pos;
        }

        public override void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            if (!initValue.HasValue) {
                InitLength(joint);
            }


            if (!ikSettings.stretchingEnabled) {
                return;
            }

            var pos = joint.transform.localPosition;

            step *= ikSettings.stretchingMoveMultiplier;

            if (step == 0) {
                grad[0] = 0;
                return;
            }

            if (axis == Axis.PARENT) {
                var l = pos.normalized;
                pos += l * step * initValue.Value;
            } else {
                pos[(int)axis] += step * initValue.Value;
            }

            joint.transform.localPosition = pos;

            var value = targetFunction();
            var res = (value - zeroLevel) / step;
            grad[0] = res;
            zeroLevel = value;

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
