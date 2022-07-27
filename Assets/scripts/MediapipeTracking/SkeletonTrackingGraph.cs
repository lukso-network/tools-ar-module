// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Assets.scripts.UI;
using DeepMotion.DMBTDemo;
using Lukso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Mediapipe.Unity.SkeletonTracking
{
   public class LandmarkData
  {
    public NormalizedLandmarkList landmarks;
    public long timestamp;

    public LandmarkData(NormalizedLandmarkList list, long timestamp) {
      this.landmarks = list;
      this.timestamp = timestamp;
    }
  }

  public class SkeletonTrackingGraph : GraphRunner
  {
    public enum ModelComplexity
    {
      Lite = 0,
      Full = 1,
      Heavy = 2,
    }

    private LandmarkData lastSkeletonLandmarkds = null;
    private LandmarkData lastFaceLandmarkds = null;
    private TextureFrame lastTextureTemp;

    public int textureLifetime = 200000;

    public override event OnDataProcessed onDataProcessed;


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

    private object syncObject = new object();
    //TODO volotile
    private long lastRenderedTimestamp;

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
    public Dictionary<long, TextureFrame> sentTextures = new Dictionary<long, TextureFrame>();

    // MEdiapipe can return value near to long.MaxValue
    private static long MAX_TIMESTAMP = long.MaxValue / 2; 
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
      // _skeletonDetectionStream.AddListener(SkeletonDetectionCallback).AssertOk();
        _skeletonLandmarksStream.AddListener(SkeletonLandmarksCallback).AssertOk();
        _faceLandmarksStream.AddListener(FaceLandmarksCallback).AssertOk();

     //   _skeletonWorldLandmarksStream.AddListener(SkeletonWorldLandmarksCallback).AssertOk();
       // _roiFromLandmarksStream.AddListener(RoiFromLandmarksCallback).AssertOk();
      }
      StartRun(BuildSidePacket(imageSource));

      Thread thread = new Thread(TexturePoolCleaner);
      thread.Start();
    }

    private void TexturePoolCleaner() {

      while (true) {
        Thread.Sleep(10);

        var ts = GetCurrentTimestampMicrosec();

        lock (syncObject) {
          try {
            // delete outdated textures
            var toDelete = sentTextures.Keys.Where(k => k < Math.Max(lastRenderedTimestamp, ts - textureLifetime)).ToList();

            //Debug.Log("************ToDelete:" + toDelete.Count + " of " + sentTextures.Count);
            foreach (var key in toDelete) {

              var text = sentTextures[key];
              sentTextures.Remove(key);
              text.Release();
            }
          } catch (Exception ex) {

          }
        }
      }
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

      lock (syncObject) {
        var t = textureFrame.packetTime;
        //TODO android gives erros
        while (sentTextures.ContainsKey(t)) {
          t += 2;
        }

        sentTextures.Add(t, textureFrame);

      }
      Update3DCamera(textureFrame);
      lastTexture = textureFrame.Texture;
    }

    private void Update3DCamera(TextureFrame textureFrame) {
      
      cameraController.UpdateCamera(textureFrame, rotation, imageSource);
    }


    public void Update() {


      var imageSource = ImageSourceProvider.ImageSource;
      if (imageSource == null) {
        return;
      }



      var mirrored = imageSource.isHorizontallyFlipped ^ imageSource.isFrontFacing;

      
     


      var ts = lastSkeletonLandmarkds?.timestamp ?? 0;
      if (ts > 0) {
        lastRenderedTimestamp = ts;
      }
      
      if (lastSkeletonLandmarkds?.landmarks == null) {
        skeletonManager.OnNewPose3(screenPlane, null, null, mirrored, null);
        return;
      }

      // get texture. Note - packet can return t-1 timestamp. So check this too
      TextureFrame texture;
      if (ts != 0) {
        lock (syncObject) {
          if (!sentTextures.TryGetValue(ts, out texture)) {
            if (!sentTextures.TryGetValue(ts + 1, out texture))
              return;
            //TODO REMOVE
              texture = lastTextureTemp;

            }
          }
        }
      else {
        texture = lastTextureTemp;
      }

      if (texture != null) {

        skeletonManager.OnNewPose3(screenPlane, lastSkeletonLandmarkds?.landmarks, lastFaceLandmarkds?.landmarks, mirrored, texture.Texture);
        if (newFrameRendered != null) {
          newFrameRendered(texture.Texture);
        }
      }

      lastSkeletonLandmarkds = null;
      lastFaceLandmarkds = null;
      if (texture != null && onDataProcessed != null) {
        onDataProcessed(texture);
      }
      lastTextureTemp = texture;
    }

    public bool TryGetNext(out Detection skeletonDetection, out NormalizedLandmarkList skeletonLandmarks, out LandmarkList skeletonWorldLandmarks, out NormalizedRect roiFromLandmarks,
      out NormalizedLandmarkList faceLandmarks,
      bool allowBlock = true)
    {

      var currentTimestampMicrosec = GetCurrentTimestampMicrosec();

    //  var r1 = TryGetNext(_skeletonDetectionStream, out skeletonDetection, allowBlock, currentTimestampMicrosec);
      var r3 = TryGetNext(_faceLandmarksStream, out faceLandmarks, allowBlock, currentTimestampMicrosec);
      var faceTimestamp = _faceLandmarksStream.GetLastpacketTimestamp();

      var r2 = TryGetNext(_skeletonLandmarksStream, out skeletonLandmarks, allowBlock, currentTimestampMicrosec);
      var skelTimestamp = _faceLandmarksStream.GetLastpacketTimestamp();
      // var r3 = TryGetNext(_skeletonWorldLandmarksStream, out skeletonWorldLandmarks, allowBlock, currentTimestampMicrosec);
      // var r3 = TryGetNext(_skeletonWorldLandmarksStream, out skeletonWorldLandmarks, allowBlock, currentTimestampMicrosec);
      // var r4 = TryGetNext(_roiFromLandmarksStream, out roiFromLandmarks, allowBlock, currentTimestampMicrosec);
      // Debug.Log((faceLandmarks == null ? "NULL FACE" : "Has face") + " " + ((skeletonLandmarks == null ? "NULL skeleton" : "Has skeleton")));
      

      skeletonWorldLandmarks = null;
      skeletonDetection = null;
      roiFromLandmarks = null;

      lastFaceLandmarkds = new LandmarkData(faceLandmarks, faceTimestamp);
      lastSkeletonLandmarkds = new LandmarkData(skeletonLandmarks, skelTimestamp);
      if (skeletonLandmarks == null) {
     //  return false;
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
      return  r2||r3;// || r3;//|| r4;
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
            if (ScaleTexture.instance.mode == ScaleTexture.Mode.SCALE_SKELETON && value != null) {
              (Vector2 scale, Vector2 offset) tr = (ScaleTexture.instance.scale, ScaleTexture.instance.offset);
              foreach (var v in value.Landmark) {
                v.X = (v.X-0.5f) / tr.scale[0] +0.5f- tr.offset[0];
                v.Y = (v.Y - 0.5f)/ tr.scale[0]+0.5f + tr.offset[1];
                v.Z = v.Z / tr.scale[0];
              }
            }
            skeletonTrackingGraph.OnSkeletonLandmarksOutput.Invoke(value);
            using (var timestamp = packet.Timestamp()) {

             AddSkeletonResult(skeletonTrackingGraph, value, timestamp.Microseconds());
              //skeletonTrackingGraph.lastSkeletonLandmarkds = new LandmarkData(value, timestamp.Microseconds());
            }
          }
        }
      }).mpPtr;
    }

    private static void AddSkeletonResult(SkeletonTrackingGraph skeletonTrackingGraph, NormalizedLandmarkList value, long timestamp) {
    
      if (timestamp < MAX_TIMESTAMP && (skeletonTrackingGraph.lastSkeletonLandmarkds == null ||  skeletonTrackingGraph.lastSkeletonLandmarkds.timestamp < timestamp)) {
        skeletonTrackingGraph.lastSkeletonLandmarkds = new LandmarkData(value, timestamp);
      } else {
        Debug.Log("Add sckeleton results error!:" + timestamp + " " + (value!=null));
      }
    }

    private static void AddFaceResult(SkeletonTrackingGraph skeletonTrackingGraph, NormalizedLandmarkList value, long timestamp) {
      if (timestamp < MAX_TIMESTAMP && (skeletonTrackingGraph.lastFaceLandmarkds == null || skeletonTrackingGraph.lastFaceLandmarkds.timestamp < timestamp)) {
        skeletonTrackingGraph.lastFaceLandmarkds = new LandmarkData(value, timestamp);
      }
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr FaceLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr) {
      return InvokeIfGraphRunnerFound<SkeletonTrackingGraph>(graphPtr, packetPtr, (skeletonTrackingGraph, ptr) => {
        using (var packet = new NormalizedLandmarkListPacket(ptr, false)) {
          if (skeletonTrackingGraph._skeletonLandmarksStream.TryGetPacketValue(packet, out var value, skeletonTrackingGraph.timeoutMicrosec)) {
            //skeletonTrackingGraph.OnSkeletonLandmarksOutput.Invoke(value);
            using (var timestamp = packet.Timestamp()) {
              AddFaceResult(skeletonTrackingGraph, value, timestamp.Microseconds());
             // skeletonTrackingGraph.lastFaceLandmarkds = new LandmarkData(value, timestamp.Microseconds());
            }
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
