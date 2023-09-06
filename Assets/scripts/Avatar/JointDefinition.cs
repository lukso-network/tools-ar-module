using Lukso;
using System.Linq;

public class JointDefinition {
    public string name = "";
    public readonly int pointId;
    public readonly Skeleton.Point point;
    public readonly GradCalculator gradCalculator;
    public JointFilter filter;
    public int[] AffectedPoints { get; private set; }

    public JointDefinition(Skeleton.Point point, params Skeleton.Point [] points) {
        this.AffectedPoints = points.Select(x => (int)x).ToArray();
        this.point = point;
        this.pointId = (int)point;
    }

    public void SetName(string name) {
        this.name = name;
    }

}
