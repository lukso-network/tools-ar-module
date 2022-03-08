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

    private const string _InputStreamName = "input_video";

    private const string _SelfieMaskStreamName = "selfie_mask";
    private OutputStream<ImageFramePacket, ImageFrame> _selfieMaskStream;
    protected long prevSelfieMaskMicrosec = 0;

    public override Status StartRun(ImageSource imageSource)
    {
      InitializeOutputStreams();
      _selfieMaskStream.StartPolling(true).AssertOk();
      return calculatorGraph.StartRun(BuildSidePacket(imageSource));
    }

    public Status StartRunAsync(ImageSource imageSource)
    {
      InitializeOutputStreams();
      _selfieMaskStream.AddListener(SelfieMaskCallback, true).AssertOk();
      return calculatorGraph.StartRun(BuildSidePacket(imageSource));
    }

    public override void Stop()
    {
      base.Stop();
      OnSelfieMaskOutput.RemoveAllListeners();
    }

    public Status AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
      return AddTextureFrameToInputStream(_InputStreamName, textureFrame);
    }

    public ImageFrame FetchNextValue()
    {
      var _ = _selfieMaskStream.TryGetNext(out var selfieMask);
      OnSelfieMaskOutput.Invoke(selfieMask);
      return selfieMask;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr SelfieMaskCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<SelfieSegmentationGraph>(graphPtr, packetPtr, (selfieSegmentationGraph, ptr) =>
      {
        using (var packet = new ImageFramePacket(ptr, false))
        {
          if (selfieSegmentationGraph.TryGetPacketValue(packet, ref selfieSegmentationGraph.prevSelfieMaskMicrosec, out var value))
          {
            selfieSegmentationGraph.OnSelfieMaskOutput.Invoke(value);
          }
        }
      }).mpPtr;
    }


#if UNITY_IOS
    protected override ConfigType DetectConfigType() {
      return ConfigType.CPU;
    }
#endif

    protected override IList<WaitForResult> RequestDependentAssets()
    {
      return new List<WaitForResult> {
        WaitForAsset("selfie_segmentation.bytes"),
        //WaitForAsset("selfie_segmentation.bytes"),

      };
    }

    protected void InitializeOutputStreams()
    {
      _selfieMaskStream = new OutputStream<ImageFramePacket, ImageFrame>(calculatorGraph, _SelfieMaskStreamName);
    }

    private SidePacket BuildSidePacket(ImageSource imageSource)
    {
      var sidePacket = new SidePacket();

      SetImageTransformationOptions(sidePacket, imageSource);
      var outputRotation = imageSource.isHorizontallyFlipped ? imageSource.rotation.Reverse() : imageSource.rotation;
      sidePacket.Emplace("output_rotation", new IntPacket((int)outputRotation));
      //sidePacket.Emplace("model_selection", new IntPacket((int)0));

      return sidePacket;
    }
  }
}
