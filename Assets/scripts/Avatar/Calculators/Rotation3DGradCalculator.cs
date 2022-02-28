using System;
using UnityEngine;

namespace Assets
{
    public class Rotation3DGradCalculator : GradCalculator
    {

        public Rotation3DGradCalculator(Constraint constraint) : base(constraint) {
            this.grad = new float[3];
        }        
        
        public Rotation3DGradCalculator(float minX, float maxX, float minY, float maxY, float minZ, float maxZ) : this(new Rotation3DConstraint(minX, maxX, minY, maxY, minZ, maxZ)) {
        }

        public override void apply(Joint joint, float step, float threshold, IkSettings ikSettings) {
            if (!ikSettings.enableRot) {
                return;
            }
            
          //  step *= ikSettings.rotationMoveMultiplier;
            var euler = joint.transform.localEulerAngles;
            if (MoveByV3Gradient(ref euler, step, threshold)) {
                joint.transform.localEulerAngles = euler;
            }
        }

        public override void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            if (!ikSettings.enableRot) {
                grad[0] = grad[1] = grad[2] = 0;
                return;
            }

            step *= ikSettings.rotationMoveMultiplier;
            var euler = joint.transform.localEulerAngles;
            for (int i = 0; i < 3; ++i) {
                euler[i] += step;
                joint.transform.localEulerAngles = euler;

                var value = targetFunction();
                var res = (value - zeroLevel) / step;
                grad[i] = res;
                zeroLevel = value;

            }

            
        }
    }

}
