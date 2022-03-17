// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Mediapipe.Unity.SelfieSegmentation
{
  public class SelfieSegmentationGraph : GraphRunner
  {
#pragma warning disable IDE1006
    public UnityEvent<ImageFrame> OnSelfieMaskOutput = new UnityEvent<ImageFrame>();
#pragma warning restore IDE1006

#if UNITY_IOS
    public override ConfigType configType => ConfigType.CPU;
#endif

    private const string _InputStreamName = "input_video";
    private const string _selfieMaskStreamName = "selfie_mask";
    private OutputStream<ImageFramePacket, ImageFrame> _selfieMaskStream;

    public override void StartRun(ImageSource imageSource) {
      if (runningMode.IsSynchronous()) {
        _selfieMaskStream.StartPolling().AssertOk();
      } else {
        _selfieMaskStream.AddListener(SelfieMaskCallback).AssertOk();
      }
      StartRun(BuildSidePacket(imageSource));
    }


    public override void Stop() {
      base.Stop();
      OnSelfieMaskOutput.RemoveAllListeners();
      _selfieMaskStream = null;
    }

    public void AddTextureFrameToInputStream(TextureFrame textureFrame) {
      AddTextureFrameToInputStream(_InputStreamName, textureFrame);
    }

    public ImageFrame TryGetNext(out ImageFrame selfieMask, bool allowBlock = true, bool notifyListener = true) {
      if (TryGetNext(_selfieMaskStream, out selfieMask, allowBlock, GetCurrentTimestampMicrosec())) {
        if (notifyListener) {
          OnSelfieMaskOutput.Invoke(selfieMask);
        }
        return selfieMask;
      }
      return null;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr SelfieMaskCallback(IntPtr graphPtr, IntPtr packetPtr) {
      return InvokeIfGraphRunnerFound<SelfieSegmentationGraph>(graphPtr, packetPtr, (SelfieSegmentationGraph, ptr) => {
        using (var packet = new ImageFramePacket(ptr, false)) {
          if (SelfieSegmentationGraph._selfieMaskStream.TryGetPacketValue(packet, out var value, SelfieSegmentationGraph.timeoutMicrosec)) {
            SelfieSegmentationGraph.OnSelfieMaskOutput.Invoke(value);
          }
        }
      }).mpPtr;
    }

    protected override IList<WaitForResult> RequestDependentAssets() {
      return new List<WaitForResult> {
        WaitForAsset("selfie_segmentation.bytes"),
      };
    }

    protected override Status ConfigureCalculatorGraph(CalculatorGraphConfig config) {
      if (runningMode == RunningMode.NonBlockingSync) {
        _selfieMaskStream = new OutputStream<ImageFramePacket, ImageFrame>(calculatorGraph, _selfieMaskStreamName, config.AddPacketPresenceCalculator(_selfieMaskStreamName));
      } else {
        _selfieMaskStream = new OutputStream<ImageFramePacket, ImageFrame>(calculatorGraph, _selfieMaskStreamName);
      }
      return calculatorGraph.Initialize(config);
    }

    private SidePacket BuildSidePacket(ImageSource imageSource) {
      var sidePacket = new SidePacket();

      SetImageTransformationOptions(sidePacket, imageSource);
      var outputRotation = imageSource.isHorizontallyFlipped ? imageSource.rotation.Reverse() : imageSource.rotation;
      sidePacket.Emplace("output_rotation", new IntPacket(180));
   //   sidePacket.Emplace("input_rotation", new IntPacket((int)outputRotation));
     // sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(false));
      //sidePacket.Emplace("input_vertically_flipped", new BoolPacket(false));
      return sidePacket;
    }
  }
}
