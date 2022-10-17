namespace Assets {

    public partial class Avatar {
        class CalcParam {
            public readonly string name;
            public float gradStep;
            public float moveStep;
            public float minStep;
            public ICalcFilter calcFilter;
            public bool calculated;

            public CalcParam(string name, ICalcFilter filter, float gradStep, float moveStep, float minStep = 0) {
                this.name = name;
                calcFilter = filter;
                this.gradStep = gradStep;
                this.moveStep = moveStep;
                this.minStep = minStep;
            }
        }

    }

}
