using System;

namespace Lukso{
    public class Rotation1DGradCalculator : GradCalculator {
        public enum Axis {
            X, Y, Z
        }

        private Axis axis;
        public Rotation1DGradCalculator(Constraint constraint, Axis axis) : base(constraint) {
            this.grad = new float[1];
            this.axis = axis;
        }

        public Rotation1DGradCalculator(float minX, float maxX, Rotation1DGradCalculator.Axis axis) : this(new Rotation1DConstraint(minX, maxX, axis), axis) {
        }
        public override void apply(Joint joint, float step, float threshold, IkSettings ikSettings) {
            if (!ikSettings.enableRot) {
                return;
            }

            //  step *= ikSettings.rotationMoveMultiplier;
            var euler = joint.transform.localEulerAngles;
            euler[(int)axis] -= step * grad[0];
            joint.transform.localEulerAngles = euler;
        }

        public override void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            if (!ikSettings.enableRot) {
                grad[0] = 0;
                return;
            }

            step *= ikSettings.rotationMoveMultiplier;
            var euler = joint.transform.localEulerAngles;

            euler[(int)axis] += step;
            joint.transform.localEulerAngles = euler;

            var value = targetFunction();
            var res = (value - zeroLevel) / step;
            grad[0] = res;
            zeroLevel = value;
        }

    }

}
