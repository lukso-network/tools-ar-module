using Assets;

public class RotationFilter : ICalcFilter {
    public bool Filter(object calculator) {
        return calculator is Rotation3DGradCalculator || calculator is Rotation1DGradCalculator;
    }
}
