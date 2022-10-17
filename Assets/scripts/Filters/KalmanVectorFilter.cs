using UnityEngine;

namespace Lukso {
    class KalmanVectorFilter {
        private KalmanPointFilter[] filters = new KalmanPointFilter[3];
        private FilterSettings settings;
        public KalmanVectorFilter(FilterSettings settings) {
            this.settings = settings;
            for (int i = 0; i < filters.Length; ++i) {
                filters[i] = new KalmanPointFilter(settings);
            }
        }

        public KalmanVectorFilter(float sigma, float dt, FilterType filterType) : this(new FilterSettings(sigma, dt, filterType)) {
        }

        public Vector3 Update(Vector3 p) {
            if (settings.IsModified) {
                foreach (var f in filters) {
                    f.SetFilterModified();
                }
            }

            float x1 = filters[0].Update(p.x);
            float y1 = filters[1].Update(p.y);
            float z1 = filters[2].Update(p.z);

            return new Vector3(x1, y1, z1);
        }
    }
}