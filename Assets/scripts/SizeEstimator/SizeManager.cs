using Assets;
using DeepMotion.DMBTDemo;
using Mediapipe;
using System;
using System.Collections;
using UnityEngine;

namespace Lukso
{

    class ManualSizing
    {
        private GameObject selected;
        private GameObject duplicate;

        [SerializeField] private Camera screenCamera; 


        public void ProcessUI() {
            if (Input.GetMouseButtonDown(0)) {

                if (selected == null) {

                    RaycastHit hit;
                    Ray ray = screenCamera.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit, LayerMask.NameToLayer("joint"))) {
                        Transform objectHit = hit.transform;

                        selected = objectHit.gameObject;
                        duplicate = GameObject.Instantiate(selected);
                        duplicate.SetActive(false);


                        selected.transform.localScale *= 3;
                        selected.GetComponent<Renderer>().material.color = new Color(1, 0.5f, 0);
                        // Do something with the object that was hit by the raycast.
                    }
                } else {

                }


            }

            if (Input.GetMouseButtonUp(0) && selected != null) {

                selected.transform.localScale = duplicate.transform.localScale;
                selected.GetComponent<Renderer>().material.color = duplicate.GetComponent<Renderer>().material.color;
                GameObject.Destroy(duplicate);

                selected = duplicate = null;
            }
        }
    }

    public class SizeManager : MonoBehaviour  {

        [SerializeField] Camera clothCamera;
        [SerializeField] Shader selfieClothShader;
        [SerializeField] DMBTDemoManager poseManager;
   //     [SerializeField] SelfieSegmentation selfieSegmentation;
        [SerializeField] AvatarManager avatarManager;
        [SerializeField] ComputeShader iouShader;
        [SerializeField] SkeletonManager skeletonManager;

    }
}
