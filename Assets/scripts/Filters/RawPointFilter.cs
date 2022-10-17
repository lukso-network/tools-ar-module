using System.Linq;
using UnityEngine;

namespace Lukso {
    public class RawPointFilter : Filter<Vector3[]> {
        private readonly float discardValue;
        private readonly float step;

        public RawPointFilter(float discardValue, float step) {
            this.discardValue = discardValue;
            this.step = step;

        }

        protected override Vector3[] filterInternal(Vector3[] v) {

            var d = v.Zip(prevValue, (x, y) => (x - y).magnitude).ToArray();
            var av = d.Average();
            var sqAv = d.Select(x => x * x).Average();
            var disp = Mathf.Sqrt(sqAv - av * av);
            //Array.Sort(d);


            // var str = String.Join(",", d);
            //  Debug.Log($"av:{av}, disp:{disp}, max:{d.Max()}, {str}");

            float threshold = av + disp * discardValue + 0.0001f;
            /* {
                 for (int i = 15; i > 0; --i) {
                     if (d[i] > threshold) {
                         Debug.Log("===========Threshold: " + i + " " + d[i]);
                     }
                 }
             }*/

            av += 0.001f;

            for (int i = 0; i < v.Length; ++i) {
                /*  var atten = (d[i] - av) / av * step;
                  if (d[i] < threshold || atten < 1) {
                      prevValue[i] = v[i];
                  } else { 
                      prevValue[i] += (v[i] - prevValue[i]) / atten;
                  }*/


                var atten = d[i] / av * step / 10;
                if (atten < 1) {
                    prevValue[i] = v[i];
                } else {
                    prevValue[i] += (v[i] - prevValue[i]) / atten;
                }

            }

            // var res = new float[prev];
            //Array.Copy(v, prevInputValue, v.Length);
            return (Vector3[])prevValue.Clone();
        }
    }
}