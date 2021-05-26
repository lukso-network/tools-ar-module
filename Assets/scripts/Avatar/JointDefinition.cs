

using Assets;

public class JointDefinition
{
    public readonly string name;
    public readonly int pointId;
    public readonly GradCalculator gradCalculator;
    public JointFilter filter;

    public JointDefinition(string name, int pointId, JointFilter filter = null, params GradCalculator[] gradCalculator) {
        this.name = name;
        this.pointId = pointId;
        this.filter = filter;
        this.gradCalculator = gradCalculator.Length == 0 ? null : gradCalculator.Length == 1 ? gradCalculator[0] : new GeneralGradCalculator(gradCalculator);
    }

    public JointDefinition(string name, int pointId, params GradCalculator[] gradCalculator) :
        this(name, pointId, null, gradCalculator) { 
    }

}
