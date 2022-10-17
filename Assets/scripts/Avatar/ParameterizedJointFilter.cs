namespace Lukso{
    public abstract class ParameterizedJointFilter : JointFilter {
        protected readonly FilterSettings settigs;

        public ParameterizedJointFilter(FilterSettings settings) {
            this.settigs = settings;
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
