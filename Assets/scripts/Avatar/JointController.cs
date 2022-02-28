using System.Collections;
using UnityEngine;

namespace Assets.scripts.Avatar
{
    public class JointController : MonoBehaviour
    {
        public Joint joint;

        public bool gradientEnabled;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        void OnValidate() {
            if (joint != null) {
                joint.gradEnabled = gradientEnabled;
            }
        }
    }
}