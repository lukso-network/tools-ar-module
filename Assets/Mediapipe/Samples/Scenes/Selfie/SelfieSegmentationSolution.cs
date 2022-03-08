// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;

namespace Mediapipe.Unity.SelfieSegmentation
{
  public class SelfieSegmentationSolution : ImageSourceSolution<SelfieSegmentationGraph>
  {
    [SerializeField] private SelfieMaskAnnotationController _SelfieMaskAnnotationController;

    protected override void OnStartRun() {
      graphRunner.OnSelfieMaskOutput.AddListener(_SelfieMaskAnnotationController.DrawLater);
      SetupAnnotationController(_SelfieMaskAnnotationController, ImageSourceProvider.ImageSource);
      _SelfieMaskAnnotationController.InitScreen();
    }

    protected override void AddTextureFrameToInputStream(TextureFrame textureFrame) {
      graphRunner.AddTextureFrameToInputStream(textureFrame);
    }

    protected override IEnumerator WaitForNextValue() {
      if (runningMode == RunningMode.Sync) {
        var _ = graphRunner.TryGetNext(out var _, true);
      } else if (runningMode == RunningMode.NonBlockingSync) {
        yield return new WaitUntil(() => graphRunner.TryGetNext(out var _, false));
      }
    }
  }
}
