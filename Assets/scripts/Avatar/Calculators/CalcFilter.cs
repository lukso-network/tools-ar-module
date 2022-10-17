using System;

public class CalcFilter : ICalcFilter {
    private readonly Type type;

    public CalcFilter(Type type) {
        this.type = type;
    }

    public bool Filter(object calculator) {
        return calculator.GetType() == type;
    }
}
