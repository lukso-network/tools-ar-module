namespace Assets.PoseEstimator
{
    class LowPassFilter : Filter<float>
    {
        private readonly float smoothStep;

        public LowPassFilter(float step) {
            smoothStep = step;
        }

        protected override float filterInternal(float v) {
            prevValue += (v - prevValue) / smoothStep;
            return prevValue;
        }
    }
}