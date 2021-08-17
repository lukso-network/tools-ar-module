using Mediapipe;
using System;
using UnityEngine;

public class FullPoseTrackingAnnotationController : AnnotationController {
  [SerializeField] GameObject poseLandmarkListPrefab = null;
  [SerializeField] GameObject poseDetectionPrefab = null;

  private GameObject poseLandmarkListAnnotation;
  private GameObject poseDetectionAnnotation;

  void Awake() {
    poseLandmarkListAnnotation = Instantiate(poseLandmarkListPrefab);
    poseDetectionAnnotation = Instantiate(poseDetectionPrefab);

        poseLandmarkListAnnotation.SetActive(false);
        poseDetectionAnnotation.SetActive(false);
  }

  void OnDestroy() {
    Destroy(poseLandmarkListAnnotation);
    Destroy(poseDetectionAnnotation);
  }

  public override void Clear() {
    poseLandmarkListAnnotation.GetComponent<FullBodyPoseLandmarkListAnnotationController>().Clear();
    poseDetectionAnnotation.GetComponent<DetectionAnnotationController>().Clear();
  }

  public void Draw(Transform screenTransform, NormalizedLandmarkList poseLandmarkList, Detection poseDetection, bool isFlipped = false)
  {
        try {
            poseLandmarkListAnnotation.GetComponent<FullBodyPoseLandmarkListAnnotationController>().Draw(screenTransform, poseLandmarkList, isFlipped);
            poseDetectionAnnotation.GetComponent<DetectionAnnotationController>().Draw(screenTransform, poseDetection, isFlipped);
        } catch (Exception e) {

        }
  }
}
