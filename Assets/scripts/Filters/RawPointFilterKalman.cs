using UnityEngine;

namespace Assets.PoseEstimator {
    public class RawPointFilterKalman : Filter<Vector3[]> {
        private readonly float sigma;
        private readonly bool enabled;
        private KalmanVectorFilter[] filters = new KalmanVectorFilter[17];

        private KalmanVectorFilter centerFilter;
        private float dt;


        public RawPointFilterKalman(float sigma, int step, float dt, FilterType filterType) {
            this.sigma = sigma;
            this.enabled = step > 1;
            this.dt = dt;

            for (int i = 0; i < filters.Length; ++i) {
                filters[i] = new KalmanVectorFilter(sigma, dt, filterType);
            }

            centerFilter = new KalmanVectorFilter(sigma, dt, filterType);

        }

        protected override Vector3[] filterInternal(Vector3[] v) {
            if (!enabled) {
                return v;
            }

            Vector3 c = Vector3.zero;
            foreach (var p in v) {
                c += p;
            }
            c /= v.Length;

            var c2 = centerFilter.Update(c);
            var spd = (c2 - c) / dt;




            for (int i = 0; i < v.Length; ++i) {
                //   prevValue[i] = filters[i].Update(v[i]-c2)+c2;
                prevValue[i] = filters[i].Update(v[i]);
            }
            return (Vector3[])prevValue.Clone();

        }
    }
}