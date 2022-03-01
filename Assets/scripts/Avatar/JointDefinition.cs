

using Assets;
using Lukso;


public class JointDefinition
{
    public string name = "";
    public readonly int pointId;
    public readonly Skeleton.Point point;
    public readonly GradCalculator gradCalculator;
    public JointFilter filter;
    public int[] AffectedPoints { get; private set; }

    public JointDefinition(Skeleton.Point point, int[] affectedPoints = null, JointFilter filter = null, params GradCalculator[] gradCalculator) {
        this.AffectedPoints = affectedPoints;
        this.point = point;
        this.pointId = (int)point;
        this.filter = filter;


        this.gradCalculator = gradCalculator.Length == 0 ? null : gradCalculator.Length == 1 ? gradCalculator[0] : new GeneralGradCalculator(gradCalculator);
    }

    public JointDefinition(Skeleton.Point point, int[] affectedPoints, params GradCalculator[] gradCalculator) :
        this(point, affectedPoints, null, gradCalculator) { 
    }

    public void SetName(string name) {
        this.name = name;
    }

}
