using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lukso{
    public partial class Avatar {
        /*  private class ClothParameter
          {
              private ClothPoint point;
              private int idx;
              private float regulararization = 1;
              public ClothParameter(ClothPoint point, int idx) {
                  this.point = point;
                  this.idx = idx;
              }

              public float Get() {
                  return point.position[idx];
              }

              public void Set(float v, bool clamp = false) {
                  var max = point.definition.maxShift[idx];
                  v = Mathf.Clamp(v, -max, max);
                  point.position[idx] = v;
              }

              public void Add(float v) {
                  point.position[idx] += v;
              }

          }*/


        private List<ClothPoint> clothPoints = new List<ClothPoint>();
        private List<PointParameter> clothPointParameters = new List<PointParameter>();
        private List<Joint> clothJoints = new List<Joint>();
        private void InitCloth() {
            var definition = skeleton.clothPoints;

            foreach (var d in definition) {

                var joint = GetJointByPoint(d.point);
                clothPoints.Add(new ClothPoint(d));
                clothJoints.Add(joint);

                var mirrored = skeleton.GetMirrored(d.point);

                if ((int)mirrored >= 0) {
                    clothJoints.Add(GetJointByPoint(mirrored));
                }
            }

            foreach (var cp in clothPoints) {
                clothPointParameters.AddRange(cp.definition.GetParameters());
            }
        }


        public void ApplyClothShift(bool keepPrevious) {
            clothSkeletonTransform.CopyTo(clothJoints);

            var hips = GetHips();
            //float globalScale = 1;// hips.transform.localScale.x/5;
            float globalScale = hips.transform.localScale.x / 1.6f;

            foreach (var cp in clothPoints) {
                var point = cp.definition.point;
                var mirrored = skeleton.GetMirrored(point);

                Joint j;
                if ((int)point < 0) {
                    j = GetJointByPoint(point); // slower
                } else {
                    j = jointByPointId[(int)point];
                }

                cp.definition.Apply(j, globalScale);

                if ((int)mirrored >= 0) {
                    j = jointByPointId[(int)mirrored];
                    cp.definition.Apply(j, globalScale);
                }
            }

        }

        public void ResetClothSize() {
            foreach (var cp in clothPoints) {
                foreach (var p in cp.definition.GetParameters()) {
                    p.Reset();
                }
            }
        }

        public void DebugChange() {
            var parameters = new List<PointParameter>();
            foreach (var cp in clothPoints) {
                parameters.AddRange(cp.definition.GetParameters());
            }

            foreach (var p in parameters) {
                p.Set(Mathf.Sin(Time.realtimeSinceStartup) * 0.5f);
            }
        }

        public void CopyToClothParameters(float[] parameters) {
            var l = Math.Min(parameters.Length, clothPointParameters.Count);
            for (int i = 0; i < l; ++i) {
                clothPointParameters[i].Set(parameters[i]);
            }
        }
        public void CopyFromClothParameters(float[] parameters) {
            var l = Math.Min(parameters.Length, clothPointParameters.Count);
            for (int i = 0; i < l; ++i) {
                parameters[i] = clothPointParameters[i].Get();
            }
        }


        public IEnumerator FindBestCloth(Func<float> target) {
            ResetClothSize();
            /*  foreach (var cp in clothPoints) {

                  var x = 0;// 0.4f * Mathf.Cos(Time.time * 0.5f);
                  var y = 0.8f;

                  cp.position = new Vector3(x, y, 0);
              }*/



            var parameters = clothPointParameters;
            //foreach(var cp in clothPoints) {
            //  parameters.AddRange(cp.definition.GetParameters());
            //}

            /*
            foreach (var p in parameters) {
                p.Set(p.Get()+0.1f);
            }

            yield break;
            */


            var rnd = new System.Random();

            float dx = settings.clothStepGradient;
            float regularization = settings.clothRegularization;
            float lambda = settings.clothLambdaGradient;

            var value = target();
            var startIor = value;

            const float EARLY_STOP_THRESHOLD = 1 + 0.001f;
            int EARLY_STOP_COUNT = parameters.Count * 2;
            int unchangeCount = 0;

            for (int step = 0; step < 600; ++step) {
                int idx = rnd.Next(0, parameters.Count);
                idx = step % parameters.Count;
                var par = parameters[idx];

                float prevVal = value;
                var oldX = par.Get();
                par.Set(oldX + dx);
                var newValue = target();
                if (settings.clothDemoMode) {
                    yield return new WaitForEndOfFrame();

                }

                var grad = (newValue - value) / dx;

                var temp = lambda * par.gradScale;
                var tryCount = 3;
                var found = false;
                for (int k = 0; k < tryCount; ++k) {

                    par.Set(oldX * (1 - temp * regularization) - temp * grad);
                    var value2 = target();
                    if (value2 <= value) {
                        value = value2;
                        found = true;
                        break;
                    }

                    temp /= 4;
                }

                if (!found) {
                    par.Set(oldX);
                }


                // Debug.Log("Scale:" + value / prevVal);
                if (value / prevVal < EARLY_STOP_THRESHOLD) {
                    unchangeCount += 1;
                    if (unchangeCount > EARLY_STOP_COUNT) {
                        //    Debug.Log($"EARLY_STOP: iter={step}");
                        break;
                    }

                } else {
                    unchangeCount = 0;
                }

                /*
                 * 
                 * par.Set(oldX*(1-lambda*regularization) - lambda * grad, true);
                var value2 = target();
                 * if (value2 > value * 0.95f) {//value < 0




                     par.Set(oldX, false);
                     //dx /= 2;

                     Debug.Log("lambda:" + lambda + " " + dx);
                 } else {
                     value = value2;
                 }*/
            }

            string message = "";
            foreach (var cp in clothPoints) {
                message += $"Point:{cp.definition.point}\n";
                foreach (var p in cp.definition.GetParameters()) {
                    message += $"   {p.Get()}\n";
                }
            }

            Debug.Log("Params:\n" + message);
            Debug.Log("Ior:" + startIor + " now:" + value);
            yield return new WaitForEndOfFrame();
        }
    }
}
