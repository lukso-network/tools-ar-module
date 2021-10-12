using Mediapipe;
using System;
using UnityEngine;

public class FullPoseTrackingAnnotationController : AnnotationController {
  [SerializeField] GameObject poseLandmarkListPrefab = null;
  [SerializeField] GameObject poseDetectionPrefab = null;
  [SerializeField] GameObject faceLandmarksPrefab = null;

    private GameObject poseLandmarkListAnnotation;
    private GameObject faceLandmarksAnnotation;
    private GameObject poseDetectionAnnotation;

  void Awake() {
    poseLandmarkListAnnotation = Instantiate(poseLandmarkListPrefab);
    poseDetectionAnnotation = Instantiate(poseDetectionPrefab);
        faceLandmarksAnnotation = Instantiate(faceLandmarksPrefab);

        poseLandmarkListAnnotation.SetActive(false);
        poseDetectionAnnotation.SetActive(false);
        faceLandmarksAnnotation.SetActive(false);
  }

  void OnDestroy() {
    Destroy(poseLandmarkListAnnotation);
    Destroy(poseDetectionAnnotation);
        Destroy(faceLandmarksAnnotation);
    }

  public override void Clear() {
    poseLandmarkListAnnotation.GetComponent<FullBodyPoseLandmarkListAnnotationController>().Clear();
    poseDetectionAnnotation.GetComponent<DetectionAnnotationController>().Clear();
  }

  public void Draw(Transform screenTransform, NormalizedLandmarkList poseLandmarkList, Detection poseDetection, NormalizedLandmarkList faceLandmarks, bool isFlipped = false)
  {
        try {
            if (poseLandmarkList != null) {
                poseLandmarkListAnnotation.GetComponent<FullBodyPoseLandmarkListAnnotationController>().Draw(screenTransform, poseLandmarkList, isFlipped);
            }

            if (poseDetection != null) {
                poseDetectionAnnotation.GetComponent<DetectionAnnotationController>().Draw(screenTransform, poseDetection, isFlipped);
            }

            if (faceLandmarks != null) {
                faceLandmarksAnnotation.GetComponent<FaceLandmarkListAnnotationController>().Draw(screenTransform, faceLandmarks, isFlipped);
            }
        } catch (Exception e) {

        }
  }
}
