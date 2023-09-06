using System.Collections;
using UnityEngine;

namespace Lukso {
    [RequireComponent(typeof(HelperDrawer))]
    public class GradientDrawer : MonoBehaviour {

        [Range(0, 200)]
        public int stepToDisplay = 0;
        private HelperDrawer helper;
        // Use this for initialization
        void Start() {
            helper = GetComponent<HelperDrawer>();
        }

        // Update is called once per frame
        void Update() {

        }

        public void DisplayChanges(int step) {
            if (step == stepToDisplay) {
                helper.UpdateHelpers(true);
            }
        }

    }
}