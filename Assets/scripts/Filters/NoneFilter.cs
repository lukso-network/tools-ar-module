using UnityEngine;

namespace Lukso {
    public class NoneFilter : Filter<Vector3[]> {
        protected override Vector3[] filterInternal(Vector3[] v) {
            return v;
        }
    }
}