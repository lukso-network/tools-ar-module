using Joint = Assets.Joint;

public class GeneralConstraint
{
    public GeneralConstraint(params Constraint[] constraints) {

    }
    internal virtual void Fix(Joint joint) {
    }
    internal virtual void KeepPrevState(Joint joint) {
    }
}
