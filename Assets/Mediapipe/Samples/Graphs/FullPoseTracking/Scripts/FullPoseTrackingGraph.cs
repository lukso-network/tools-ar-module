using DeepMotion.DMBTDemo;
using Mediapipe;
using UnityEngine;

public class FullPoseTrackingGraph : DemoGraph {
  private const string poseLandmarksStream = "pose_landmarks_smoothed";
  private OutputStreamPoller<NormalizedLandmarkList> poseLandmarksStreamPoller;
  private NormalizedLandmarkListPacket poseLandmarksPacket;

  private const string poseDetectionStream = "pose_detection";
  private OutputStreamPoller<Detection> poseDetectionStreamPoller;
  private DetectionPacket poseDetectionPacket;

  private const string poseLandmarksPresenceStream = "pose_landmarks_smoothed_presence";
  private OutputStreamPoller<bool> poseLandmarksPresenceStreamPoller;
  private BoolPacket poseLandmarksPresencePacket;

  private const string poseDetectionPresenceStream = "pose_detection_presence";
  private OutputStreamPoller<bool> poseDetectionPresenceStreamPoller;
  private BoolPacket poseDetectionPresencePacket;

  private DMBTDemoManager skeletonManager;

  public override Status StartRun() {
    skeletonManager = FindObjectOfType<DMBTDemoManager>();

    poseLandmarksStreamPoller = graph.AddOutputStreamPoller<NormalizedLandmarkList>(poseLandmarksStream).Value();
    poseLandmarksPacket = new NormalizedLandmarkListPacket();

    poseDetectionStreamPoller = graph.AddOutputStreamPoller<Detection>(poseDetectionStream).Value();
    poseDetectionPacket = new DetectionPacket();

    poseLandmarksPresenceStreamPoller = graph.AddOutputStreamPoller<bool>(poseLandmarksPresenceStream).Value();
    poseLandmarksPresencePacket = new BoolPacket();

    poseDetectionPresenceStreamPoller = graph.AddOutputStreamPoller<bool>(poseDetectionPresenceStream).Value();
    poseDetectionPresencePacket = new BoolPacket();

    return graph.StartRun();
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var poseTrackingValue = FetchNextPoseTrackingValue();
    RenderAnnotation(screenController, poseTrackingValue);

    screenController.DrawScreen(textureFrame);
  }

  private FullPoseTrackingValue FetchNextPoseTrackingValue() {
    if (!FetchNextPoseLandmarksPresence()) {
      return new FullPoseTrackingValue();
    }

    var poseLandmarks = FetchNextPoseLandmarks();

    if (!FetchNextPoseDetectionPresence()) {
      return new FullPoseTrackingValue(poseLandmarks);
    }

    var poseDetection = FetchNextPoseDetection();

    return new FullPoseTrackingValue(poseLandmarks, poseDetection);
  }

  private NormalizedLandmarkList FetchNextPoseLandmarks() {
    return FetchNext(poseLandmarksStreamPoller, poseLandmarksPacket, poseLandmarksStream);
  }

  private Detection FetchNextPoseDetection() {
    return FetchNext(poseDetectionStreamPoller, poseDetectionPacket, poseDetectionStream);
  }

  private bool FetchNextPoseLandmarksPresence() {
    return FetchNext(poseLandmarksPresenceStreamPoller, poseLandmarksPresencePacket, poseLandmarksPresenceStream);
  }

  private bool FetchNextPoseDetectionPresence() {
    return FetchNext(poseDetectionPresenceStreamPoller, poseDetectionPresencePacket, poseDetectionPresenceStream);
  }

  private void RenderAnnotation(WebCamScreenController screenController, FullPoseTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<FullPoseTrackingAnnotationController>().Draw(screenController.transform, value.PoseLandmarkList, value.PoseDetection, false);

    skeletonManager.OnNewPose(screenController.transform, value.PoseLandmarkList);
  }

  protected override void PrepareDependentAssets() {
    PrepareDependentAsset("pose_detection.bytes");
    PrepareDependentAsset("pose_landmark_upper_body.bytes");
  }
}
