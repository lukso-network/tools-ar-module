using Lukso;
using Mediapipe;
using Mediapipe.Unity;
using System.Linq;
using UnityEngine;

namespace Assets.scripts {
    public class LightManager : MonoBehaviour {

        public Light lightSource;
        [Range(0, 1)]
        public float calculationInterval = 0.5f;

        public Vector3 faceNormalScaler = new Vector3(1, 0.3f, 2f);
        public Vector3 lightDirectionOverrider;

        private Vector3[] faceNormals;
        private Vector3[] faceVertices;
        private float[,][] faceNormalsProduct;

        private DMBTDemoManager dmtManager;

        private Vector3 prevDir = Vector3.up;

        [Range(0, 1)]
        public float filterScale = 0.1f;

        private const int FACE_POINT_COUNT = 468;
        float[] intencities = new float[FACE_POINT_COUNT];
        private float lastCaclulationTime = 0;
        private Vector3 lightDir;


        private readonly float[][,] rotTransform = new float[][,] {
            new float[,] { { 1, 0, 0 }, { 0, -1, 1 }},
            new float[,] { { -1, 0, 1 }, { 0, -1, 1 } },
            new float[,] { { 0, -1, 1 }, { -1, 0, 1 } },
            new float[,] { { 0, 1, 0 }, { 1, 0, 0 } }
        };

        public void OnValidate() {
            InitFace();
        }

        // Use this for initialization
        void Start() {


            InitFace();

            lightDir = lightSource.transform.rotation * Vector3.forward;

        }

        private void InitFace(){
            if (dmtManager == null) {
                dmtManager = FindObjectOfType<DMBTDemoManager>();
                dmtManager.newFaceEvent += CalculateLight;
            }
            if (dmtManager.FaceMesh == null) {
                return;
            }
            faceNormals = (Vector3[])dmtManager.FaceMesh.normals.Clone();
            for (int i = 0; i < faceNormals.Length; ++i) {
                var n = faceNormals[i];
                n.x *= faceNormalScaler.x;
                n.y *= faceNormalScaler.y;
                n.z *= faceNormalScaler.z;
                faceNormals[i] = n.normalized;
            }
            faceVertices = dmtManager.FaceMesh.vertices;
            PrepareFaceNormals(faceNormals);
        }


        // Update is called once per frame
        void Update() {
            lightSource.transform.rotation = Quaternion.Lerp(lightSource.transform.rotation, Quaternion.FromToRotation(-Vector3.forward, lightDir), 0.1f);
        }

        private Matrix4x4 InitLeastSqrMatrix(int[] indices) {
            var mt = Matrix4x4.zero;
            for (int i = 0; i < 4; ++i) {
                for (int j = i; j < 4; ++j) {
                    var vals = faceNormalsProduct[i, j];
                    float s = 0;
                    foreach (int k in indices) {
                        s += vals[k];
                    }

                    mt[i, j] = mt[j, i] = s;
                }
            }

            mt = mt.inverse;
            return mt;
        }

        private void PrepareFaceNormals(Vector3[] faceNormals) {
            float[,][] mat = new float[4, 4][];
            for (int i = 0; i < 4; ++i) {
                for (int j = i; j < 4; ++j) {
                    mat[i, j] = new float[faceNormals.Length];
                    for (int k = 0; k < faceNormals.Length; ++k) {
                        var p = faceNormals[k];
                        var n1 = i < 3 ? p[i] : 1;
                        var n2 = j < 3 ? p[j] : 1;

                        mat[i, j][k] = n1 * n2;
                    }
                }
            }

            faceNormalsProduct = mat;

        }

        public void CalculateLight(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped) {
            if (faceLandmarks == null || faceLandmarks.Landmark.Count == 0) {
                return;
            }


            if (Time.realtimeSinceStartup - lastCaclulationTime < calculationInterval) {
                return;
            }
            lastCaclulationTime = Time.realtimeSinceStartup;

            Vector4 res = SolveLightEquation(faceLandmarks, texture, flipped);
            
            RenderSettings.ambientLight = Vector4.one * Mathf.Clamp(res.w, 0, 0.3f);

            var dir = new Vector3(res.x, res.y, res.z);
            dir += lightDirectionOverrider;
            
            dir = dir.normalized;

            //dir = new Vector3(0, 0, -1);

            dir = dmtManager.FaceDirection * dir;
            //dir = FilterDir(dir);
            var imageSource = ImageSourceProvider.ImageSource;
            if (imageSource.isHorizontallyFlipped ^ imageSource.isFrontFacing) {
                dir.x = -dir.x;
            }
            lightDir = dir;

        }


        private Vector3 FilterDir(Vector3 dir) {
            dir = (dir - prevDir) * filterScale + prevDir;
            dir = dir.normalized;
            prevDir = dir;

            return dir;

        }

        private Vector4 CreateBVector(int[] indices, float[] intencities, bool checkDirection = false) {
            Vector4 b = Vector4.zero;
            foreach (var k in indices) {
                var intencity = intencities[k];

                var n = faceNormals[k];
                b.x += n.x * intencity;
                b.y += n.y * intencity;
                b.z += n.z * intencity;
                b.w += intencity;
            }
            return b;
        }


        private Vector4 SolveLightEquation(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped) {
            float[] intencities = GetIntencities(faceLandmarks, texture, flipped);

            const float MIN_INTENCITY = 0.1f;
            var indices = intencities
                        .Where(x => x > MIN_INTENCITY)
                        .Select((x, i) => i)
                        .ToArray();

            var mt = InitLeastSqrMatrix(indices);
            var bvector = CreateBVector(indices, intencities);
            return mt * bvector;

        }

        private float[] GetIntencities(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped) {

            var im = ImageSourceProvider.ImageSource;
            var angle = (int)im.rotation;

            int w = texture.width;
            int h = texture.height;

            var localForwardDir = Quaternion.Inverse(dmtManager.FaceDirection) * (-Vector3.forward);

            var fakeLightDir = Quaternion.Inverse(dmtManager.FaceDirection) * new Vector3(0, 1.0f, -1.0f).normalized;
            const float MIN_COS = 0.3f;
            //TODO


            float[,] tr = null;
            if (angle == 0) {
                if (flipped) {
                    tr = rotTransform[0];
                } else {
                    tr = rotTransform[1];
                }
            } else if (angle == 270) {
                tr = rotTransform[2];
            } else if (angle == 90) {
                tr = rotTransform[3];

            }

            for (int i = 0; i < faceVertices.Length; ++i) {
                float intencity = 0;

                if (Vector3.Dot(faceNormals[i], localForwardDir) > MIN_COS) {
                    var p = faceLandmarks.Landmark[i];
                    var x = (int)(w * (tr[0, 0] * p.X + tr[0, 1] * p.Y + tr[0, 2]));
                    var y = (int)(h * (tr[1, 0] * p.X + tr[1, 1] * p.Y + tr[1, 2]));
                    //    x = (int)Mathf.Clamp((flipped ? p.X : (1 - p.X)) * w, 2, w - 2f);
                    //  y = (int)Mathf.Clamp((1 - p.Y) * h, 1, h - 2f);
                    x = (int)Mathf.Clamp(x, 2, w - 2f);
                    y = (int)Mathf.Clamp(y, 2, h - 2f);

                    //var c = texture.GetPixel(x, y) + texture.GetPixel(x - 2, y) + texture.GetPixel(x + 2, y) + texture.GetPixel(x, y - 2) + texture.GetPixel(x, y + 2);
                    var c = texture.GetPixel(x, y);

                    intencity = (c[0] + c[1] + c[2]) / 3;// /5;
                    intencity += Mathf.Max(Vector3.Dot(faceNormals[i], fakeLightDir)) * 0.07f;

                    /*
                              for (int r = 0; r < 4; ++r) {
                                for (int r2 = 0; r2 < 4; ++r2) {
                                  texture.SetPixel(x+r, y+r2, new Color(1, 1, 1, 1));
                                }
                              }*/
                }


                intencities[i] = intencity;
            }
            //texture.Apply();

            return intencities;
        }
    }
}
