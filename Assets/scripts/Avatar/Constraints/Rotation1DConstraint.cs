using Assets;
using UnityEngine;
using Joint = Assets.Joint;

public class Rotation1DConstraint : Constraint
{
    public readonly AngleConstraint x;
    public readonly Rotation1DGradCalculator.Axis axis;
    private Vector3 prevAngles;

    public Rotation1DConstraint(float minX, float maxX, Rotation1DGradCalculator.Axis axis) {
        x = new AngleConstraint(minX, maxX);
        this.axis = axis;
    }

    internal override void KeepPrevState(Joint joint) {
        prevAngles = joint.transform.localEulerAngles;
    }

    internal override void Fix(Joint joint) {
        var transform = joint.transform;
        var rot = prevAngles;
        switch (axis) {
            case Rotation1DGradCalculator.Axis.X:
                rot.x = x.Clamp(transform.localEulerAngles.x);
                break;

            case Rotation1DGradCalculator.Axis.Y:
                rot.y = x.Clamp(transform.localEulerAngles.y);
                break;

            case Rotation1DGradCalculator.Axis.Z:
                rot.z = x.Clamp(transform.localEulerAngles.z);
                break;
        }

        transform.localEulerAngles = rot;
    }
}
