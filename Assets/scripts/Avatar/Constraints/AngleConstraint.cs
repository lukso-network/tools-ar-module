using UnityEngine;

public class AngleConstraint : Constraint
{
    public readonly float minAngle;
    public readonly float maxAngle;


    public AngleConstraint(float minAngle, float maxAngle) {
        this.minAngle = ((minAngle % 360) + 360) % 360;
        this.maxAngle = ((maxAngle % 360) + 360) % 360;
    }

    public bool IsIn(float angle) {
        return true;
    }

    public float Clamp(float angle) {
        angle = ((angle % 360) + 360) % 360;
        if (maxAngle > minAngle) {
            return Mathf.Clamp(angle, minAngle, maxAngle);
        } else {
            if (angle >= maxAngle && angle <= minAngle) {
                return (angle - maxAngle < minAngle - angle) ? maxAngle : minAngle;
            } else {
                return angle;
            }
        }
    }
}
