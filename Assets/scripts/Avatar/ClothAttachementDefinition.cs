
using Assets;
using System.Collections.Generic;
using static Lukso.Skeleton;


namespace Lukso {

    public class ClothPointParameter {
        private float value;
        public readonly float gradScale;
        private readonly (float, float) minMax;
        public ClothPointParameter(float gradScale, (float, float) minMax) {
            this.gradScale = gradScale;
            this.minMax = minMax;
            this.value = 0;
        }

        public float Get() {
            return value;
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

        protected List<ClothPointParameter> parameters = new List<ClothPointParameter>();
        //public Vector3 maxShift;
        //public Direction direction;

        public ClothAttachementDefinition(Skeleton.Point point) {
            this.point = point;
            //this.maxShift = maxShift;
            //this.direction = dir;
        }

        public List<ClothPointParameter> GetParameters() {
            return parameters;
        }

        public virtual void Apply(Joint joint, float globalScale) {
        }
    }

    //---------------------------------------------------------------------------------------------------

    public class ClothAttachement1DNormal: ClothAttachementDefinition  {

        public ClothAttachement1DNormal(Point point, float maxValue) : base(point) {
            parameters.Add(new ClothPointParameter(1, (-maxValue, maxValue)));
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
            parameters.Add(new ClothPointParameter(1, (-maxValue, maxValue)));
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
            parameters.Add(new ClothPointParameter(1, (-maxValue, maxValue)));
        }

        public override void Apply(Joint joint, float globalScale) {
           // var s = new Vector3(parameters[0].Get() * 5, 0, 0);
            var s = new UnityEngine.Vector3(parameters[0].Get(), 0, 0);

            joint.clothScale = s;
        }
    }

}
