using Mediapipe;

class FullPoseTrackingValue {
  public readonly NormalizedLandmarkList PoseLandmarkList;
  public readonly NormalizedLandmarkList FaceLandmark;
  public readonly Detection PoseDetection;

  public FullPoseTrackingValue(NormalizedLandmarkList landmarkList, Detection detection, NormalizedLandmarkList faceLandmark) {
    PoseLandmarkList = landmarkList;
    PoseDetection = detection;
    FaceLandmark = faceLandmark;
  }

  public FullPoseTrackingValue(NormalizedLandmarkList landmarkList) : this(landmarkList, new Detection(), null) {}

  public FullPoseTrackingValue() : this(new NormalizedLandmarkList()) {}
}
