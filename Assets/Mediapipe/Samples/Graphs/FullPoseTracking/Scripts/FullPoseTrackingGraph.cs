using DeepMotion.DMBTDemo;
using Mediapipe;
using UnityEngine;

public class FullPoseTrackingGraph : DemoGraph {
    enum ModelComplexity {
    Lite = 0,
    Full = 1,
    Heavy = 2,
  }

  [SerializeField] ModelComplexity modelComplexity = ModelComplexity.Full;
  [SerializeField] bool smoothLandmarks = true;

  private const string poseLandmarksStream = "pose_landmarks";
  private OutputStreamPoller<NormalizedLandmarkList> poseLandmarksStreamPoller;
  private NormalizedLandmarkListPacket poseLandmarksPacket;

  private const string poseDetectionStream = "pose_detection";
  private OutputStreamPoller<Detection> poseDetectionStreamPoller;
  private DetectionPacket poseDetectionPacket;

  private const string faceLandmarksStream = "face_landmarks";
  private OutputStreamPoller<NormalizedLandmarkList> faceLandmarksStreamPoller;
  private NormalizedLandmarkListPacket faceLandmarksPacket;

  private const string poseLandmarksPresenceStream = "pose_landmarks_presence";
  private OutputStreamPoller<bool> poseLandmarksPresenceStreamPoller;
  private BoolPacket poseLandmarksPresencePacket;

  private const string faceLandmarksPresenceStream = "face_landmarks_presence";
  private OutputStreamPoller<bool> faceLandmarksPresenceStreamPoller;
  private BoolPacket faceLandmarksPresencePacket;

  private const string poseDetectionPresenceStream = "pose_detection_presence";
  private OutputStreamPoller<bool> poseDetectionPresenceStreamPoller;
  private BoolPacket poseDetectionPresencePacket;

  private DMBTDemoManager skeletonManager;
  private SidePacket sidePacket;

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

    faceLandmarksStreamPoller = graph.AddOutputStreamPoller<NormalizedLandmarkList>(faceLandmarksStream).Value();
    faceLandmarksPacket = new NormalizedLandmarkListPacket();
    
    faceLandmarksPresenceStreamPoller = graph.AddOutputStreamPoller<bool>(faceLandmarksPresenceStream).Value();
    faceLandmarksPresencePacket = new BoolPacket();

    sidePacket = new SidePacket();
    //sidePacket.Emplace("enable_iris_detection", new BoolPacket(false));
    sidePacket.Emplace("model_complexity", new IntPacket((int)modelComplexity));
    sidePacket.Emplace("smooth_landmarks", new BoolPacket(smoothLandmarks));

    return graph.StartRun(sidePacket);
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
        var poseTrackingValue = FetchNextPoseTrackingValue();
        RenderAnnotation(screenController, poseTrackingValue);

        screenController.DrawScreen(textureFrame);
  }

  private FullPoseTrackingValue FetchNextPoseTrackingValue() {
    var isFaceLandmarksPresent = FetchNextFaceLandmarksPresence();
    var faceLandmarks = isFaceLandmarksPresent ? FetchNextFaceLandmarks() : new NormalizedLandmarkList();
    if (!FetchNextPoseLandmarksPresence()) {
      return new FullPoseTrackingValue(null, null, faceLandmarks);
    }

    var poseLandmarks = FetchNextPoseLandmarks();

    if (!FetchNextPoseDetectionPresence()) {
      return new FullPoseTrackingValue(poseLandmarks, null, faceLandmarks);
    }

    var poseDetection = FetchNextPoseDetection();

    return new FullPoseTrackingValue(poseLandmarks, poseDetection, faceLandmarks);
  }

    private NormalizedLandmarkList FetchNextFaceLandmarks() {
        return FetchNext(faceLandmarksStreamPoller, faceLandmarksPacket, faceLandmarksStream);
    }

    private bool FetchNextFaceLandmarksPresence() {
    return FetchNext(faceLandmarksPresenceStreamPoller, faceLandmarksPresencePacket, faceLandmarksPresenceStream);
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
        skeletonManager.OnNewPose(screenController.transform, value.PoseLandmarkList, value.FaceLandmark, !IsFlipped());
        GetComponent<FullPoseTrackingAnnotationController>().Draw(screenController.transform, value.PoseLandmarkList, value.PoseDetection, value.FaceLandmark, !IsFlipped());

  }

  protected override void PrepareDependentAssets() {
    PrepareDependentAsset("pose_detection.bytes");
    PrepareDependentAsset("face_detection_front.bytes");
    PrepareDependentAsset("face_landmark.bytes");

      /*  PrepareDependentAsset("face_detection_front.bytes");
        PrepareDependentAsset("face_landmark.bytes");
        PrepareDependentAsset("iris_landmark.bytes");
        PrepareDependentAsset("hand_landmark.bytes");
        PrepareDependentAsset("hand_recrop.bytes");
        PrepareDependentAsset("handedness.txt");
        PrepareDependentAsset("palm_detection.bytes");
        PrepareDependentAsset("pose_detection.bytes");
      */

        if (modelComplexity == ModelComplexity.Lite) {
      PrepareDependentAsset("pose_landmark_lite.bytes");
    } else if (modelComplexity == ModelComplexity.Full) {
      PrepareDependentAsset("pose_landmark_full.bytes");
    } else {
      PrepareDependentAsset("pose_landmark_heavy.bytes");
    }
  }
}
