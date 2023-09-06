using Joint = Lukso.Joint;
using System.Collections.Generic;
using UnityEngine;
using static Lukso.Skeleton;
using System;

namespace Lukso {

    public class PointParameter {
        private float value;
        public readonly float gradScale;
        private readonly (float, float) minMax;

        public ClothAttachementDefinition AssignedObj { get; internal set; }

        public PointParameter(float gradScale) : this(gradScale, (-1e9f, 1e9f)) {
        }

        public PointParameter(float gradScale, (float, float) minMax) {
            this.gradScale = gradScale;
            this.minMax = minMax;
            this.value = 0;
        }

        public float Get() {
            return value;
        }

        public float GetScaled() {
            return value * gradScale;
        }

        public void Set(float v) {
            if (float.IsNaN(v)) {
                Debug.LogError("NAN");
                v = 0;
            }
            value = UnityEngine.Mathf.Clamp(v, minMax.Item1, minMax.Item2);
        }

        public void Reset() {
            value = 0;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachementDefinition {
        public enum Direction {
            NORMAL,
            ALONG
        }

        public enum Axis {

            X,
            Y,
            Z,
            PARENT,
        }

        public Vector3 GetMask(Axis axis) {
            Vector3 mask = Vector3.zero;
            if ((int)axis < 3) {
                mask[(int)axis] = 1;
            }
            return mask;
        }

        public readonly Skeleton.Point point;
        public bool ReinitAlways { get; set; }

        protected List<PointParameter> parameters = new List<PointParameter>();
        //public Vector3 maxShift;
        //public Direction direction;

        public ClothAttachementDefinition(Skeleton.Point point) {
            this.point = point;
            //this.maxShift = maxShift;
            //this.direction = dir;
        }

        public List<PointParameter> GetParameters() {
            return parameters;
        }

        public ClothAttachementDefinition SetReinit(bool value) {
            ReinitAlways = value;
            return this;
        }

        public virtual void Apply(Joint joint, float globalScale) {
        }

        public virtual void Init(Joint joint) { }

        protected Vector3 ParamsToV3Scaled() {
            return new Vector3(parameters[0].GetScaled(), parameters[1].GetScaled(), parameters[2].GetScaled());
        }

        public void AddParameter(PointParameter parameter) {
            parameter.AssignedObj = this;
            parameters.Add(parameter);
        }
    }



    public class ScalingParameter : ClothAttachementDefinition {
        private Vector3 initScale;
        public override void Init(Joint joint) {
            this.initScale = joint.transform.localScale;
        }

        public ScalingParameter(Point point, float scale, (float, float) minMax) : base(point) {

            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
        }

        public override void Apply(Joint joint, float globalScale) {
            joint.transform.localScale = initScale * (1 + parameters[0].GetScaled()) * globalScale;
        }

    }

    public class Position3DParameter : ClothAttachementDefinition {
        private Vector3 initPosition;
        public override void Init(Joint joint) {
            this.initPosition = joint.transform.position;
        }

        public Position3DParameter(Point point) : base(point) {
            AddParameter(new PointParameter(0.4f));
            AddParameter(new PointParameter(0.4f));
            AddParameter(new PointParameter(0.4f));
        }

        public override void Apply(Joint joint, float globalScale) {
            joint.transform.position = initPosition + ParamsToV3Scaled() * globalScale;
        }

    }


    public class Rotation1DParameter : ClothAttachementDefinition {


        private Vector3 localRotation;
        private Vector3 mask;
        private int axisIdx;
        public override void Init(Joint joint) {
            this.localRotation = joint.transform.localEulerAngles;
        }

        public Rotation1DParameter(Point point, Axis axis) : this(point, axis, 90, (-1e9f, 1e9f)) {
        }
        public Rotation1DParameter(Point point, Axis axis, float scale, (float, float) minMax) : base(point) {
            if ((int)axis >= 3) {
                throw new Exception("Incorrect axis: Only X,Y,Z are supported");
            }
            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
            mask = GetMask(axis);
            axisIdx = (int)axis;
        }


        public override void Apply(Joint joint, float globalScale) {
            var v = joint.transform.localEulerAngles;
            v[axisIdx] = localRotation[axisIdx] + parameters[0].GetScaled() * globalScale;
            joint.transform.localEulerAngles = v;
        }
    }


    public class Rotation3DParameter : ClothAttachementDefinition {

        private Vector3 localRotation;
        public override void Init(Joint joint) {
            this.localRotation = joint.transform.localEulerAngles;
        }

        public Rotation3DParameter(Point point) : this(point, 90, (-1e9f, 1e9f)) {
        }
        public Rotation3DParameter(Point point, float scale, (float, float) minMax) : base(point) {
            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
        }



        public override void Apply(Joint joint, float globalScale) {
            joint.transform.localEulerAngles = localRotation + ParamsToV3Scaled() * globalScale;
        }
    }

    public class Stretching3DParameter : ClothAttachementDefinition {

        private Vector3 initPosition;
        private Vector3 dir;
        private Axis axis;
        public override void Init(Joint joint) {
            this.initPosition = joint.transform.localPosition;


            if (axis == Axis.PARENT) {
                this.dir = joint.transform.localPosition; //we don't need normalization here - use relative size of vector
            } else {
                dir = Vector3.zero;
                dir[(int)axis] = joint.transform.localPosition.magnitude;
            }
        }

        public Stretching3DParameter(Point point, Axis axis, float scale, (float, float) minMax) : base(point) {
            AddParameter(new PointParameter(scale, (minMax.Item1 / scale, minMax.Item2 / scale)));
            this.axis = axis;
        }

        public override void Apply(Joint joint, float globalScale) {
            joint.transform.localPosition = initPosition + dir * parameters[0].GetScaled() * globalScale;
        }
    }



    //---------------------------------------------------------------------------------------------------

    public class ClothAttachement1DNormal : ClothAttachementDefinition {

        public ClothAttachement1DNormal(Point point, float maxValue) : base(point) {
            AddParameter(new PointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
            var t = joint.transform;
            var dir = (t.position - t.parent.position).normalized;
            var normal = joint.boneNormalRotation * dir;
            joint.transform.position += normal * parameters[0].Get() * globalScale;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachementMoveAlongAxis : ClothAttachementDefinition {

        public ClothAttachementMoveAlongAxis(Point point, float maxValue) : base(point) {
            AddParameter(new PointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
            var t = joint.transform;
            var dir = (t.position - t.parent.position).normalized;
            joint.transform.position += dir * parameters[0].Get() * globalScale;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachmentScale : ClothAttachementDefinition {

        public ClothAttachmentScale(Point point, float scale, float maxValue) : base(point) {
            AddParameter(new PointParameter(scale, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
            // var s = new Vector3(parameters[0].Get() * 5, 0, 0);
            var p = parameters[0].GetScaled();
            var s = new Vector3(p, 0, p);

            joint.clothScale = s;
        }
    }

    public class ClothAttachmentGlobalScale : ClothAttachementDefinition {
        private AvatarManager manager;
        public ClothAttachmentGlobalScale(Point point, float scale, float maxValue) : base(point) {
            AddParameter(new PointParameter(scale, (-maxValue, maxValue)));

            manager = GameObject.FindObjectOfType<AvatarManager>();
        }

        public override void Apply(Joint joint, float globalScale) {
            // var s = new Vector3(parameters[0].Get() * 5, 0, 0);
            var p = parameters[0].GetScaled();
            manager.skinScaler = new Vector3(1 + p, 1, 1 + p);
        }
    }

}
