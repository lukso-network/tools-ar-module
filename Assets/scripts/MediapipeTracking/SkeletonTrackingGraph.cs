// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using DeepMotion.DMBTDemo;
using Lukso;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mediapipe.Unity.SkeletonTracking
{


  public class SkeletonTrackingGraph : GraphRunner
  {
    public enum ModelComplexity
    {
      Lite = 0,
      Full = 1,
      Heavy = 2,
    }

    private NormalizedLandmarkList lastSkeletonLandmarkds = null;
    private NormalizedLandmarkList lastFaceLandmarkds = null;


    public ModelComplexity modelComplexity = ModelComplexity.Full;
    public bool smoothLandmarks = true;

#pragma warning disable IDE1006  // UnityEvent is PascalCase
    public UnityEvent<Detection> OnSkeletonDetectionOutput = new UnityEvent<Detection>();
    public UnityEvent<NormalizedLandmarkList> OnSkeletonLandmarksOutput = new UnityEvent<NormalizedLandmarkList>();
    public UnityEvent<LandmarkList> OnSkeletonWorldLandmarksOutput = new UnityEvent<LandmarkList>();
    public UnityEvent<NormalizedRect> OnRoiFromLandmarksOutput = new UnityEvent<NormalizedRect>();
#pragma warning restore IDE1006

    [SerializeField] private DMBTDemoManager skeletonManager;
    [SerializeField] private Transform screenPlane;
    [SerializeField] private Camera3DController cameraController;

    public delegate void OnNewFrameRendered(Texture2D texture);
    public event OnNewFrameRendered newFrameRendered;

    //private const string poseLandmarksStream = "pose_landmarks";
    // private const string poseDetectionStream = "pose_detection";
    ///  private const string faceLandmarksStream = "face_landmarks";
    // private const string poseLandmarksPresenceStream = "pose_landmarks_presence";
    //  private const string faceLandmarksPresenceStream = "face_landmarks_presence";
    // private const string poseDetectionPresenceStream = "pose_detection_presence";


    private Texture2D lastTexture;
    private const string _InputStreamName = "input_video";
    private const string poseDetectionStream = "pose_detection";
    private const string poseLandmarksStream = "pose_landmarks";
    private const string faceLandmarksStream = "face_landmarks";
    //  private const string _SkeletonWorldLandmarksStreamName1 = "skeleton_world_landmarks";
    // private const string _RoiFromLandmarksStreamName = "roi_from_landmarks";

    private ImageSource imageSource;
    

    private OutputStream<DetectionPacket, Detection> _skeletonDetectionStream;
    private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _skeletonLandmarksStream;
    private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _faceLandmarksStream;
    private OutputStream<LandmarkListPacket, LandmarkList> _skeletonWorldLandmarksStream;
   // private OutputStream<NormalizedRectPacket, NormalizedRect> _roiFromLandmarksStream;

    public override void StartRun(ImageSource imageSource)
    {
      this.imageSource = imageSource;
      if (runningMode.IsSynchronous())
      {
        _skeletonDetectionStream.StartPolling().AssertOk();
        _skeletonLandmarksStream.StartPolling().AssertOk();
        _faceLandmarksStream.StartPolling().AssertOk();
       // _skeletonWorldLandmarksStream.StartPolling().AssertOk();
      //  _roiFromLandmarksStream.StartPolling().AssertOk();
      }
      else
      {
        _skeletonDetectionStream.AddListener(SkeletonDetectionCallback).AssertOk();
        _skeletonLandmarksStream.AddListener(SkeletonLandmarksCallback).AssertOk();
        _faceLandmarksStream.AddListener(FaceLandmarksCallback).AssertOk();

     //   _skeletonWorldLandmarksStream.AddListener(SkeletonWorldLandmarksCallback).AssertOk();
       // _roiFromLandmarksStream.AddListener(RoiFromLandmarksCallback).AssertOk();
      }
      StartRun(BuildSidePacket(imageSource));
    }

    public override void Stop()
    {
      base.Stop();
      OnSkeletonDetectionOutput.RemoveAllListeners();
      OnSkeletonLandmarksOutput.RemoveAllListeners();
      OnSkeletonWorldLandmarksOutput.RemoveAllListeners();
      OnRoiFromLandmarksOutput.RemoveAllListeners();
      _skeletonDetectionStream = null;
      _skeletonLandmarksStream = null;
      _faceLandmarksStream = null;
      _skeletonWorldLandmarksStream = null;
     // _roiFromLandmarksStream = null;
    }

    public void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
      AddTextureFrameToInputStream(_InputStreamName, textureFrame);

      Update3DCamera(textureFrame);
      lastTexture = textureFrame.Texture;

      if (newFrameRendered != null) {
        newFrameRendered(lastTexture);
      }
    }

    private void Update3DCamera(TextureFrame textureFrame) {
      
      cameraController.UpdateCamera(textureFrame, rotation, imageSource);
    }

    public void LateUpdate() {
      var imageSource = ImageSourceProvider.ImageSource;
      var mirrored = imageSource.isHorizontallyFlipped ^ imageSource.isFrontFacing;

      skeletonManager.OnNewPose3(screenPlane, lastSkeletonLandmarkds, lastFaceLandmarkds, mirrored, lastTexture);
    }

    public bool TryGetNext(out Detection skeletonDetection, out NormalizedLandmarkList skeletonLandmarks, out LandmarkList skeletonWorldLandmarks, out NormalizedRect roiFromLandmarks,
      out NormalizedLandmarkList faceLandmarks,
      bool allowBlock = true)
    {

      var currentTimestampMicrosec = GetCurrentTimestampMicrosec();
    //  var r1 = TryGetNext(_skeletonDetectionStream, out skeletonDetection, allowBlock, currentTimestampMicrosec);
      var r3 = TryGetNext(_faceLandmarksStream, out faceLandmarks, allowBlock, currentTimestampMicrosec);
      var r2 = TryGetNext(_skeletonLandmarksStream, out skeletonLandmarks, allowBlock, currentTimestampMicrosec);

      // var r3 = TryGetNext(_skeletonWorldLandmarksStream, out skeletonWorldLandmarks, allowBlock, currentTimestampMicrosec);
      // var r3 = TryGetNext(_skeletonWorldLandmarksStream, out skeletonWorldLandmarks, allowBlock, currentTimestampMicrosec);
      // var r4 = TryGetNext(_roiFromLandmarksStream, out roiFromLandmarks, allowBlock, currentTimestampMicrosec);
      Debug.Log((faceLandmarks == null ? "NULL FACE" : "Has face") + " " + ((skeletonLandmarks == null ? "NULL skeleton" : "Has skeleton")));

      skeletonWorldLandmarks = null;
      skeletonDetection = null;
      roiFromLandmarks = null;

      lastFaceLandmarkds = faceLandmarks;
      lastSkeletonLandmarkds = skeletonLandmarks;
      if (skeletonLandmarks == null) {
        return false;
      }

    //  if (r1 && skeletonDetection != null) { OnSkeletonDetectionOutput.Invoke(skeletonDetection); }
      if (r2 && skeletonLandmarks != null) { OnSkeletonLandmarksOutput.Invoke(skeletonLandmarks); }
    //  if (r3) { OnSkeletonWorldLandmarksOutput.Invoke(skeletonWorldLandmarks); }
    //  if (r4) { OnRoiFromLandmarksOutput.Invoke(roiFromLandmarks); }
      /*
      var tempList = new NormalizedLandmarkList();
      for(int i = 0; i < skeletonWorldLandmarks.Landmark.Count; ++i) {
        var n = new NormalizedLandmark();
        n.X = skeletonWorldLandmarks.Landmark[i].X;
        n.Y = skeletonWorldLandmarks.Landmark[i].Y;
        n.Z = skeletonWorldLandmarks.Landmark[i].Z;
        tempList.Landmark.Add(n);
      }*/



      //TODO check on different phones
    //  var imageSource = ImageSourceProvider.ImageSource;
//      var mirrored = imageSource.isHorizontallyFlipped ^ imageSource.isFrontFacing;

  //    skeletonManager.OnNewPose(screenPlane, skeletonLandmarks, faceLandmarks, mirrored, lastTexture);
      return  r2;// || r3;//|| r4;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr SkeletonDetectionCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) =>
      {
        using (var packet = new DetectionPacket(ptr, false))
        {
          if (skeletonTrackingGraph._skeletonDetectionStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec))
          {
            skeletonTrackingGraph.OnSkeletonDetectionOutput.Invoke(value);
          }
        }
      }).mpPtr;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr SkeletonLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) =>
      {
        using (var packet = new NormalizedLandmarkListPacket(ptr, false))
        {
          if (skeletonTrackingGraph._skeletonLandmarksStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec))
          {
            skeletonTrackingGraph.OnSkeletonLandmarksOutput.Invoke(value);
            skeletonTrackingGraph.lastSkeletonLandmarkds = value;
          }
        }
      }).mpPtr;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr FaceLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr) {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) => {
        using (var packet = new NormalizedLandmarkListPacket(ptr, false)) {
          if (skeletonTrackingGraph._skeletonLandmarksStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec)) {
            //skeletonTrackingGraph.OnSkeletonLandmarksOutput.Invoke(value);
            skeletonTrackingGraph.lastFaceLandmarkds = value;
          }
        }
      }).mpPtr;
    }


    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr SkeletonWorldLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) =>
      {
        using (var packet = new LandmarkListPacket(ptr, false))
        {
          if (skeletonTrackingGraph._skeletonWorldLandmarksStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec))
          {
            skeletonTrackingGraph.OnSkeletonWorldLandmarksOutput.Invoke(value);
          }
        }
      }).mpPtr;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr RoiFromLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) =>
      {
        using (var packet = new NormalizedRectPacket(ptr, false))
        {
       //   if (skeletonTrackingGraph._roiFromLandmarksStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec))
        //  {
       //     skeletonTrackingGraph.OnRoiFromLandmarksOutput.Invoke(value);
       //   }
        }
      }).mpPtr;
    }

    protected override IList<WaitForResult> RequestDependentAssets()
    {
      return new List<WaitForResult> {
        WaitForAsset("pose_detection.bytes"),
        WaitForAsset("face_landmark.bytes"),
        WaitForAsset("face_detection_short_range.bytes"),
        WaitForSkeletonLandmarkModel(),
      };
    }

    protected override Status ConfigureCalculatorGraph(CalculatorGraphConfig config)
    {
      if (runningMode == RunningMode.NonBlockingSync)
      {
        _skeletonDetectionStream = new OutputStream<DetectionPacket, Detection>(calculatorGraph, poseDetectionStream, config.AddPacketPresenceCalculator(poseDetectionStream));
        _skeletonLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, poseLandmarksStream, config.AddPacketPresenceCalculator(poseLandmarksStream));
        _faceLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, faceLandmarksStream, config.AddPacketPresenceCalculator(faceLandmarksStream));
        //_skeletonWorldLandmarksStream = new OutputStream<LandmarkListPacket, LandmarkList>(calculatorGraph, _SkeletonWorldLandmarksStreamName, config.AddPacketPresenceCalculator(_SkeletonWorldLandmarksStreamName));
        //  _roiFromLandmarksStream = new OutputStream<NormalizedRectPacket, NormalizedRect>(calculatorGraph, _RoiFromLandmarksStreamName, config.AddPacketPresenceCalculator//(_RoiFromLandmarksStreamName));
      }
      else
      {
        _skeletonDetectionStream = new OutputStream<DetectionPacket, Detection>(calculatorGraph, poseDetectionStream, true);
        _skeletonLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, poseLandmarksStream, true);
        _faceLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, faceLandmarksStream, true);
      //  _skeletonWorldLandmarksStream = new OutputStream<LandmarkListPacket, LandmarkList>(calculatorGraph, _SkeletonWorldLandmarksStreamName, true);
      //  _roiFromLandmarksStream = new OutputStream<NormalizedRectPacket, NormalizedRect>(calculatorGraph, _RoiFromLandmarksStreamName, true);
      }
      return calculatorGraph.Initialize(config);
    }

    private WaitForResult WaitForSkeletonLandmarkModel()
    {
      switch (modelComplexity)
      {
        case ModelComplexity.Lite: return WaitForAsset("pose_landmark_lite.bytes");
        case ModelComplexity.Full: return WaitForAsset("pose_landmark_full.bytes");
        case ModelComplexity.Heavy: return WaitForAsset("pose_landmark_heavy.bytes");
        default: throw new InternalException($"Invalid model complexity: {modelComplexity}");
      }
    }

    private SidePacket BuildSidePacket(ImageSource imageSource)
    {
      var sidePacket = new SidePacket();

      SetImageTransformationOptions(sidePacket, imageSource);
      sidePacket.Emplace("model_complexity", new IntPacket((int)modelComplexity));
      sidePacket.Emplace("smooth_landmarks", new BoolPacket(smoothLandmarks));

      Logger.LogInfo(TAG, $"Model Complexity = {modelComplexity}");
      Logger.LogInfo(TAG, $"Smooth Landmarks = {smoothLandmarks}");

      return sidePacket;
    }
  }
}
