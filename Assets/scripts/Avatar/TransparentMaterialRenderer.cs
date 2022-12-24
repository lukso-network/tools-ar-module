using Mediapipe.Unity;
using Mediapipe.Unity.SkeletonTracking;
using UnityEngine;

namespace Lukso {
    public class TransparentMaterialRenderer : MonoBehaviour {

        private Camera3DController cam3d;
        private SkeletonTrackingGraph skelGraph;
        private Renderer renderer;
        private AvatarManager avatarManager;
        private Material oldMaterial;
        private Material newMaterial;

        public void Init() {
            skelGraph = FindObjectOfType<SkeletonTrackingGraph>();
            skelGraph.newFrameRendered += OnNewFrameRendered;


            avatarManager = FindObjectOfType<AvatarManager>();
            cam3d = FindObjectOfType<Camera3DController>();
               

            //sometime Unity does not find renderer in child just after attach
            renderer = transform.parent.GetComponentInChildren<Renderer>() ?? transform.GetComponent<Renderer>();
            oldMaterial = renderer.material;
            newMaterial = FindObjectOfType<AvatarManager>().transparentMaterial;
            renderer.material = newMaterial;
        }

        // Use this for initialization
        void Start() {
            if (skelGraph == null) {
                Init();
            }

        }

        private void OnDestroy() {
            if (skelGraph != null) {
                skelGraph.newFrameRendered -= OnNewFrameRendered;
            }
        }

        private void OnNewFrameRendered(Texture2D texture) {
            if (!gameObject.activeSelf || !enabled) {
                renderer.material = oldMaterial;
                return;
            }


            if (texture == null) {
                gameObject.SetActive(false);
                return;
            }
            renderer.material = newMaterial;

        }
    }


}
