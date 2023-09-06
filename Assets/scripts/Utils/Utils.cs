using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class MatrixExtensions {
    public static Quaternion ExtractRotation(this Matrix4x4 matrix) {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractPosition(this Matrix4x4 matrix) {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public static Vector3 ExtractScale(this Matrix4x4 matrix) {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}





namespace Assets.Demo.Scripts {
    public class Utils {

        public class FPSCounter {
            const float fpsMeasurePeriod = 0.5f;
            private int counter = 0;
            private float lastTime = 0;
            private float fps;

            private float[] times;
            private int idx = 0;

            public FPSCounter(int periods = 30) {
                times = new float[periods];
            }

            public float GetFps() {
                return fps;
            }

            public float UpdateFps2() {
                counter++;
                float t = Time.realtimeSinceStartup;
                if (t > lastTime + fpsMeasurePeriod) {
                    fps = counter / (t - lastTime);
                    counter = 0;
                    lastTime = t;
                }
                return fps;
            }

            public float UpdateFps() {
                var t = Time.realtimeSinceStartup;
                var tprev = times[idx];
                times[idx] = t;

                idx = (idx + 1) % times.Length;

                fps = times.Length / (t - tprev);
                return fps;
            }
        }

        public static void GetAllChildrenDSF(Transform root, List<Transform> list) {
            foreach (Transform t in root) {
                list.Add(t);
                GetAllChildrenDSF(t, list);
            }
        }

        /*
        private static void ScaleAnisotropic(Transform root, Vector3 scale, List<Vector3> positions, List<Vector3> scales, int idx) {

            var invScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            var ms = Matrix4x4.Scale(scale);
            var msInv = ms.inverse;// Matrix4x4.Scale(invScale);


            //root.localScale = Vector3.Scale(root.localScale, curScale);
            //curScale.x /= scale.x;
            //curScale.y /= scale.y;
            //curScale.z /= scale.z;
            foreach (Transform t in root) {
                //ScaleAnisotropic(t, scale, curScale, positions, scales, idx + 1);
                var m = t.localToWorldMatrix;
             //   m = msInv * m * ms;
                m = ms * m * msInv;
                var p = m.ExtractPosition();
                var s = m.ExtractScale();
                var r = m.ExtractRotation();
                t.localPosition = p;
                t.localRotation = r;
                t.localScale = s;

                ScaleAnisotropic(t, scale, positions, scales, idx + 1);
            }
        }

        public static void ScaleAnisotropic(Transform root, Vector3 scale) {
            var allChildren = new List<Transform>();

            allChildren.Add(root);
            GetAllChildrenDSF(root, allChildren);

            var positions = allChildren.Select(t => t.position).ToList();
            var scales = allChildren.Select(t => t.lossyScale).ToList();
            var locScales = allChildren.Select(t => t.localScale).ToList();
            root.localScale = Vector3.Scale(root.localScale, scale);
            ScaleAnisotropic(root, scale, positions, scales, 0);
            return;

            var ds = Vector3.one;
            int i = 0;

            root.localScale = Vector3.Scale(root.localScale, scale);
            foreach(var tr in allChildren) {
                tr.position = positions[i];
                ++i;
            }
            
        }
        */

        public static void PreparePivots(GameObject avatar) {
            var children = avatar.GetComponentsInChildren<Transform>();

            foreach (var c in children) {
                if (c.parent != null && c.parent.name.ToLower() == "pivot") {
                    c.parent.name = c.name;
                    c.name += "_old";

                    Debug.Log($"Replaced pivot name of {c.parent.name}");
                }
            }

            HashSet<string> names = new HashSet<string>();
            foreach (var c in avatar.GetComponentsInChildren<Transform>()) {
                if (names.Contains(c.name)) {
                    c.name = c.name + "_" + names.Count;
                }

                names.Add(c.name);

            }
        }

        public static void AddMissedJoints(GameObject origin, GameObject to) {
            var children = origin.GetComponentsInChildren<Transform>();
            var childrenTo = to.GetComponentsInChildren<Transform>();

            //TODO
            if (children.Length == childrenTo.Length) {
                return;
            }

            var processed = new List<Transform>();

            foreach (var c in children) {
                if (c.parent != null && c.name.ToLower().EndsWith("_old")) {
                    var obj1 = c.parent.parent;
                    var obj2 = c;

                    var name = c.name.Substring(0, c.name.Length - 4);

                    var target = childrenTo.Where(x => x.name == name).FirstOrDefault();
                    if (target == null) {
                        Debug.LogError($"Incorrect hierarchy: name '{obj2.name}' not found");
                        continue;
                    }

                    var targetGrand = new GameObject($"{obj2.name}_grandpa");
                    var targetPivot = new GameObject("pivot");
                    targetPivot.transform.parent = targetGrand.transform;
                    targetGrand.transform.parent = target.parent;
                    target.parent = targetPivot.transform;
                    //target.name = target.name + "_old";

                    CopyLocalTransform(obj1, targetGrand.transform);
                    CopyLocalTransform(c.parent.transform, targetPivot.transform);
                    CopyLocalTransform(obj2.transform, target.transform);

                    processed.Add(targetGrand.transform);
                    processed.Add(targetPivot.transform);
                    processed.Add(target.transform);

                }
            }

            childrenTo = to.GetComponentsInChildren<Transform>();
            foreach (var c in children) {
                var target = childrenTo.Where(x => x.name == c.name).FirstOrDefault();
                if (processed.Find(x => x == target) != null) {
                    CopyLocalTransform(c, target);
                }
            }

        }

        private static void CopyLocalTransform(Transform from, Transform to) {
            to.localScale = from.localScale;
            to.localPosition = from.localPosition;
            to.localRotation = from.localRotation;
        }

        public static string ReplaceSpace(string name) {
            return name.Replace(" ", "_");
        }

        public static bool CompareNodeByName(string name1, string name2) {
            return ReplaceSpace(name1) == ReplaceSpace(name2);
        }
        public static void LeastSquares(float[] x, float[] y, out float k, out float m) {
            float xAver = x.Average();
            float yAver = y.Average();
            float xSqAver = x.Select(v => v * v).Average();
            float xyAver = x.Zip(y, (a, b) => a * b).Average();

            k = (xyAver - xAver * yAver) / (xSqAver - xAver * xAver);
            m = (yAver * xSqAver - xyAver * xAver) / (xSqAver - xAver * xAver);
        }


        public static void Main(string[] args) {
            {
                float[] x = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                var y = new float[] { 101, 201, 301, 401, 501, 601, 701, 801, 901, 1001 };

                float k, m;
                LeastSquares(x, y, out k, out m);

                System.Console.WriteLine($"{k}, {m}");
            }


            {
                float[] x = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                float[] y = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                float[] a = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                float[] b = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                float k0x = 2;
                float k0y = 4;

                float m0x = -3;
                float m0y = 4;



                for (int i = 0; i < x.Length; ++i) {
                    a[i] = x[i] * k0x + m0x;
                    b[i] = y[i] * k0y + m0y;
                }

                float[] w = new float[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

                float k, mx, my;

                LeastSquaresUniform(x, y, a, b, w, out k, out mx, out my);
                System.Console.WriteLine($"{k}, {mx} {my}");

            }


        }


        public static void LeastSquaresUniform(float[] x, float[] y, float[] a, float[] b, float[] w, out float k, out float mx, out float my) {
            float xAver = x.Zip(w, (v1, v2) => v1 * v2).Average();
            float yAver = y.Zip(w, (v1, v2) => v1 * v2).Average();
            float xSqAver = x.Select(v => v * v).Average();

            float aAver = a.Zip(w, (v1, v2) => v1 * v2).Average();
            float bAver = b.Zip(w, (v1, v2) => v1 * v2).Average();
            float wAver = w.Average();

            float s1 = 0;
            float s2 = 0;


            for (int i = 0; i < w.Length; ++i) {
                s1 += (x[i] * x[i] + y[i] * y[i]) * w[i];
                s2 += x[i] * a[i] * w[i] + y[i] * b[i] * w[i];
            }
            float xyAver = s1 / w.Length;
            float xav = s2 / w.Length;

            float c00 = xAver, c01 = wAver, r0 = aAver;
            float c10 = yAver, c12 = c01, r1 = bAver;
            float c20 = xyAver, c21 = c00, c22 = c10, r2 = xav;

            k = (c00 * r0 - c01 * r2 + c10 * r1) / (c00 * c00 - c01 * c20 + c10 * c10);
            mx = (-c00 * c01 * r2 + c00 * c10 * r1 + c01 * c20 * r0 + c10 * c10 * (-r0)) / (c01 * (-c00 * c00 + c01 * c20 - c10 * c10));
            my = (c00 * c00 * (-r1) + c00 * c10 * r0 - c01 * c10 * r2 + c01 * c20 * r1) / (c01 * (-c00 * c00 + c01 * c20 - c10 * c10));


        }
    }
}
