using Assets;
using UnityEngine;
using Joint = Assets.Joint;

public class StretchingConstraint : Constraint {
    public readonly StretchingGradCalculator.Axis axis;
    private float? initialPosition = null;
    private Vector3 initialVector;
    private float minRelative;
    private float maxRelative;

    private float minX;
    private float maxX;

    public StretchingConstraint(float minX, float maxX, StretchingGradCalculator.Axis axis) {
        this.minRelative = minX;
        this.maxRelative = maxX;
        this.axis = axis;
    }

    internal override void KeepPrevState(Joint joint) {
        if (initialPosition == null) {
            if (axis == StretchingGradCalculator.Axis.PARENT) {
                initialPosition = (joint.transform.localPosition).magnitude;
                initialVector = joint.transform.localPosition.normalized;
            } else {
                initialPosition = joint.transform.localPosition[(int)axis];
            }

            minX = minRelative * initialPosition.Value;
            maxX = maxRelative * initialPosition.Value;
            if (minX > maxX) {
                // negative values
                var t = minX;
                minX = maxX;
                maxX = t;
            }

        }

    }

    internal override void Fix(Joint joint) {
        var transform = joint.transform;
        if (axis == StretchingGradCalculator.Axis.PARENT) {
            var dir = joint.transform.localPosition;
            var len = dir.magnitude;
            var newLength = Mathf.Clamp(len, minX, maxX);
            if (newLength != len) {
                transform.localPosition = initialVector * newLength;
            }
        } else {
            var pos = transform.localPosition;
            pos[(int)axis] = Mathf.Clamp(pos[(int)axis], minX, maxX);
            transform.localPosition = pos;
        }
    }
}
