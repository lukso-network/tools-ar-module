using Assets.PoseEstimator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets
{

    public class Joint
    {
        public Transform transform;

        public Vector3 initPosition;
        public Vector3 initLocalPosition;
        public Vector3 initLossyScale;

        public JointFilter filter;
        public Joint parent;
        public List<Joint> children = new List<Joint>();
        public bool gradEnabled;
        private readonly GradCalculator gradCalculator;
        public JointDefinition definition;
        public float lengthScale = 1;

        public Joint(Transform tr, JointDefinition definition) {
            this.filter = definition?.filter;
            this.definition = definition;
            this.gradCalculator = definition?.gradCalculator;// ??  new RotationGradCalculator();
            InitByTransform(tr);
        }

        public void InitByTransform(Transform tr) {
            this.transform = tr;
            this.initPosition = tr.position;
            this.initLocalPosition = tr.localPosition;
            this.initLossyScale = tr.lossyScale;
        }

        public void Reset() {
            InitByTransform(transform);
        }

        public void Filter() {
            if (filter != null) {
                filter.Filter(this);
            }

        }

        internal virtual float CalcGradients(ICalcFilter calcFilter, float zeroLevel, Func<float> targetFunction, float step, float posStep, float gradThreshold, IkSettings ikSettings) {
            posStep = 0.00001f;

            int i = 0;

            if (!gradEnabled) {
                return zeroLevel;
            }
            gradCalculator.calc(calcFilter, ref zeroLevel, targetFunction, this, step, ikSettings);
         //   CalcRotGradient(ref zeroLevel, targetFunction, transform, step, ref i);
           // CalcStretchingGradient(ref zeroLevel, targetFunction, transform, posStep, ref i);
         
            return zeroLevel;
            //transform.localRotation = rotation;
        }
  
        internal virtual void FixConstraints() {
            gradCalculator.FixConstraint(this);
        }

        internal float FindMaxGradient(ICalcFilter calcFilter) {
            return gradCalculator.FindMaxGrad(calcFilter);
        }

        internal virtual void MoveByGradients(ICalcFilter calcFilter, float step, float posStep, float threshold, IkSettings ikSettings) {

            if (!gradEnabled) {
                return;
            }

            if (ikSettings.useConstraints) {
                gradCalculator.ApplyWithConstraint(calcFilter, this, step, threshold, ikSettings);
            } else {
                gradCalculator.apply(calcFilter, this, step, threshold, ikSettings);
            }
        
        }

        internal void ApplyRotation(Joint joint) {
            transform.localRotation = joint.transform.localRotation;
            transform.localScale = joint.transform.localScale;
            transform.localPosition = joint.transform.localPosition;
        }       
        
        internal void CopyRotationAndPosition(Joint joint) {
            transform.localPosition = joint.transform.localPosition;
            transform.localRotation = joint.transform.localRotation;
            transform.localScale = joint.transform.localScale;
            initPosition = joint.transform.position;
            // transform.rotation = joint.transform.rotation;

        }

        internal void CopyToLocalFromGlobal(Joint joint) {
            transform.localPosition = joint.transform.position;
            transform.localRotation = joint.transform.rotation;


            transform.localScale = new Vector3(initLossyScale.x * joint.transform.lossyScale.x / joint.initLossyScale.x,
                initLossyScale.y * joint.transform.lossyScale.y / joint.initLossyScale.y,
                initLossyScale.z * joint.transform.lossyScale.z / joint.initLossyScale.z);

            //TODO


            if (joint.transform.lossyScale.x < 50) {
                //transform.localScale = joint.transform.lossyScale;
            } else {
                //transform.localScale = joint.transform.lossyScale;
            }
            //initPosition = joint.transform.position;
            // transform.rotation = joint.transform.rotation;

        }

    }
  
}
