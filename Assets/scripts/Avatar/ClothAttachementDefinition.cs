
using Assets;
using Joint = Assets.Joint;
using System.Collections.Generic;
using UnityEngine;
using static Lukso.Skeleton;
using static Assets.StretchingGradCalculator;

namespace Lukso {

    public class PointParameter {
        private float value;
        public readonly float gradScale;
        private readonly (float, float) minMax;

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
            value = UnityEngine.Mathf.Clamp(v, minMax.Item1, minMax.Item2);
        }

        public void Reset() {
            value = 0;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachementDefinition
    {
        public enum Direction
        {
            NORMAL,
            ALONG
        }
        public readonly Skeleton.Point point;

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

        public virtual void Apply(Joint joint, float globalScale) {
        }

       public virtual void Init(Joint joint) { }

        protected Vector3 ParamsToV3Scaled() {
          return new Vector3(parameters[0].GetScaled(), parameters[1].GetScaled(), parameters[2].GetScaled());
        }
    }

   public class Position3DParameter : ClothAttachementDefinition
  {
    private Vector3 initPosition;
    public override void Init(Joint joint) {
      this.initPosition = joint.transform.position;
    }

    public Position3DParameter(Point point) : base(point) {
      parameters.Add(new PointParameter(0.1f));
      parameters.Add(new PointParameter(0.1f));
      parameters.Add(new PointParameter(0.1f));
    }

    public override void Apply(Joint joint, float globalScale) {
      joint.transform.position = initPosition + ParamsToV3Scaled() * globalScale;
    }

  }

  public class Rotation3DParameter : ClothAttachementDefinition
  {

    private Vector3 localRotation;
    public override void Init(Joint joint) {
      this.localRotation = joint.transform.localEulerAngles;
    }

    public Rotation3DParameter(Point point) : base(point) {
      parameters.Add(new PointParameter(90));
      parameters.Add(new PointParameter(90));
      parameters.Add(new PointParameter(90));
    }

    public override void Apply(Joint joint, float globalScale) {
      joint.transform.localEulerAngles = localRotation + ParamsToV3Scaled() * globalScale;
    }
  }

  public class Stretching3DParameter : ClothAttachementDefinition
  {

    private Vector3 initPosition;
    private Vector3 dir;
    private StretchingGradCalculator.Axis axis;
    public override void Init(Joint joint) {
      this.initPosition = joint.transform.localPosition;
      

      if (axis == Axis.PARENT) {
        this.dir = joint.transform.localPosition; //we don't need normalization here - use relative size of vector
      } else {
        dir = Vector3.zero;
        dir[(int)axis] = joint.transform.localPosition.magnitude;
      }
    }

    public Stretching3DParameter(Point point, StretchingGradCalculator.Axis axis) : base(point) {
      parameters.Add(new PointParameter(1));
      this.axis = axis;
    }

    public override void Apply(Joint joint, float globalScale) {
      joint.transform.localPosition = initPosition + dir * parameters[0].GetScaled() * globalScale;
    }
  }

  public class Rotation1DParameter : ClothAttachementDefinition
  {

    private StretchingGradCalculator.Axis axis;
    private Vector3 localEulerAngles;
    public override void Init(Joint joint) {
      this.localEulerAngles = joint.transform.localEulerAngles ;
    }

    public Rotation1DParameter(Point point, StretchingGradCalculator.Axis axis) : base(point) {
      parameters.Add(new PointParameter(90));
      this.axis = axis;
    }

    public override void Apply(Joint joint, float globalScale) {
      var v = localEulerAngles;
      v[(int)axis] = parameters[0].Get() * globalScale;
      joint.transform.localEulerAngles = localEulerAngles + ParamsToV3Scaled() * globalScale;
    }
  }


  //---------------------------------------------------------------------------------------------------

  public class ClothAttachement1DNormal: ClothAttachementDefinition  {

        public ClothAttachement1DNormal(Point point, float maxValue) : base(point) {
            parameters.Add(new PointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
            var t = joint.transform;
            var dir = (t.position - t.parent.position).normalized;
                var normal = joint.boneNormalRotation * dir;
                joint.transform.position += normal * parameters[0].Get() * globalScale;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachementMoveAlongAxis : ClothAttachementDefinition
    {

        public ClothAttachementMoveAlongAxis(Point point, float maxValue) : base(point) {
            parameters.Add(new PointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
            var t = joint.transform;
            var dir = (t.position - t.parent.position).normalized;
            joint.transform.position += dir * parameters[0].Get() * globalScale;
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachmentScale : ClothAttachementDefinition
    {

        public ClothAttachmentScale(Point point, float maxValue) : base(point) {
            parameters.Add(new PointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
           // var s = new Vector3(parameters[0].Get() * 5, 0, 0);
            var s = new Vector3(parameters[0].Get(), 0, 0);

            joint.clothScale = s;
        }
    }

}
