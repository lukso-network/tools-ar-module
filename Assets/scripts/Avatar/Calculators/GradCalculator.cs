using System;
using System.Linq;
using UnityEngine;

namespace Assets
{
    public abstract class GradCalculator
    {
        protected float[] grad;
        protected readonly Constraint constraint;

        public abstract void calc(ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings);
      
        public virtual void calc(ICalcFilter calcFilter, ref float zeroLevel, Func<float> targetFunction, Joint joint, float step, IkSettings ikSettings) {
            if (calcFilter.Filter(this)) {
                calc(ref zeroLevel, targetFunction, joint, step, ikSettings);
            }
        }

        public abstract void apply(Joint joint, float step, float threshold, IkSettings ikSettings);
        public virtual void apply(ICalcFilter calcFilter, Joint joint, float step, float threshold, IkSettings ikSettings) {

            if (calcFilter.Filter(this)) {
                apply(joint, step, threshold, ikSettings);
            }

        }

        public GradCalculator(Constraint constraint = null) {
            this.constraint = constraint;
        }

        internal virtual void FixConstraint(Joint joint) {
            if (constraint != null) {
                constraint.Fix(joint);
            }
        }

        internal virtual void BeforeApply(Joint joint) {
            if (constraint != null) {
                constraint.KeepPrevState(joint);
            }
        }

        public virtual float FindMaxGrad(ICalcFilter calcFilter) {
            if (calcFilter.Filter(this)) {
                return grad.Length == 1 ? Mathf.Abs(grad[0]) : grad.Select(x => Mathf.Abs(x)).Max();
            }
            return 0;
        }

        public void ApplyWithConstraint(ICalcFilter calcFilter, Joint joint, float step, float threshold, IkSettings ikSettings) {
            BeforeApply(joint);
            apply(calcFilter, joint, step, threshold, ikSettings);
            FixConstraint(joint);
        }

        internal virtual bool MoveByV3Gradient(ref Vector3 v, float step, float threshold) {
            bool found = false;

            for (int i = 0; i < 3; ++i) {
                v[i] -= grad[i] * step;
            }

            return true;
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
