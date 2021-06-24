

using Assets;

public class JointDefinition
{
    public readonly string name;
    public readonly int pointId;
    public readonly GradCalculator gradCalculator;
    public JointFilter filter;
    public int[] AffectedPoints { get; private set; }

    public JointDefinition(string name, int pointId, int[] affectedPoints = null, JointFilter filter = null, params GradCalculator[] gradCalculator) {
        this.AffectedPoints = affectedPoints;
        this.name = name;
        this.pointId = pointId;
        this.filter = filter;
        this.gradCalculator = gradCalculator.Length == 0 ? null : gradCalculator.Length == 1 ? gradCalculator[0] : new GeneralGradCalculator(gradCalculator);
    }

    public JointDefinition(string name, int pointId, int[] affectedPoints, params GradCalculator[] gradCalculator) :
        this(name, pointId, affectedPoints, null, gradCalculator) { 
    }

}
