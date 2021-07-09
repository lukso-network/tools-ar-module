using Joint = Assets.Joint;

public class Rotation3DConstraint
    : Constraint
{
    public readonly AngleConstraint x;
    public readonly AngleConstraint y;
    public readonly AngleConstraint z;

    public Rotation3DConstraint(float minX, float maxX, float minY, float maxY, float minZ, float maxZ) {
        x = new AngleConstraint(minX, maxX);
        y = new AngleConstraint(minY, maxY);
        z = new AngleConstraint(minZ, maxZ);
    }

    internal override void Fix(Joint joint) {
        var rot = joint.transform.localEulerAngles;
        rot.x = x.Clamp(rot.x);
        rot.y = y.Clamp(rot.y);
        rot.z = z.Clamp(rot.z);

        joint.transform.localEulerAngles = rot;
    }
}
