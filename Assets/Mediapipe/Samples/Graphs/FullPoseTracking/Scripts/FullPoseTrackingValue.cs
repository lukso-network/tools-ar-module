using Mediapipe;

class FullPoseTrackingValue {
  public readonly NormalizedLandmarkList PoseLandmarkList;
  public readonly Detection PoseDetection;

  public FullPoseTrackingValue(NormalizedLandmarkList landmarkList, Detection detection) {
    PoseLandmarkList = landmarkList;
    PoseDetection = detection;
  }

  public FullPoseTrackingValue(NormalizedLandmarkList landmarkList) : this(landmarkList, new Detection()) {}

  public FullPoseTrackingValue() : this(new NormalizedLandmarkList()) {}
}
