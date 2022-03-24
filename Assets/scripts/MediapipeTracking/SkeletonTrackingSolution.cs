// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe.Unity.SelfieSegmentation;
using System.Collections;
using UnityEngine;

namespace Mediapipe.Unity.SkeletonTracking
{
  public class SkeletonTrackingSolution : ImageSourceSolution<SkeletonTrackingGraph>
  {
    [SerializeField] private RectTransform _worldAnnotationArea;
    [SerializeField] private DetectionAnnotationController _skeletonDetectionAnnotationController;
    [SerializeField] private PoseLandmarkListAnnotationController _skeletonLandmarksAnnotationController;
    [SerializeField] private PoseWorldLandmarkListAnnotationController _skeletonWorldLandmarksAnnotationController;
    [SerializeField] private NormalizedRectAnnotationController _roiFromLandmarksAnnotationController;
    [SerializeField] private SelfieSegmentationCreator selfieCreator;


    public SkeletonTrackingGraph.ModelComplexity modelComplexity
    {
      get => graphRunner.modelComplexity;
      set => graphRunner.modelComplexity = value;
    }

    public bool smoothLandmarks
    {
      get => graphRunner.smoothLandmarks;
      set => graphRunner.smoothLandmarks = value;
    }

    protected override void SetupScreen(ImageSource imageSource)
    {
      base.SetupScreen(imageSource);
      _worldAnnotationArea.localEulerAngles = imageSource.rotation.Reverse().GetEulerAngles();
    }

    protected override void OnStartRun()
    {
      graphRunner.OnSkeletonDetectionOutput.AddListener(_skeletonDetectionAnnotationController.DrawLater);
      graphRunner.OnSkeletonLandmarksOutput.AddListener(_skeletonLandmarksAnnotationController.DrawLater);
      graphRunner.OnSkeletonWorldLandmarksOutput.AddListener(_skeletonWorldLandmarksAnnotationController.DrawLater);
      graphRunner.OnRoiFromLandmarksOutput.AddListener(_roiFromLandmarksAnnotationController.DrawLater);

      var imageSource = ImageSourceProvider.ImageSource;
      SetupAnnotationController(_skeletonDetectionAnnotationController, imageSource);
      SetupAnnotationController(_skeletonLandmarksAnnotationController, imageSource);
      SetupAnnotationController(_skeletonWorldLandmarksAnnotationController, imageSource);
      SetupAnnotationController(_roiFromLandmarksAnnotationController, imageSource);
    }

    internal void StartTracking() {
      base.Play();
      selfieCreator.Play();
    }

    protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
      graphRunner.AddTextureFrameToInputStream(textureFrame);
    }

    protected override IEnumerator WaitForNextValue()
    {
      if (runningMode == RunningMode.Sync)
      {
        var _ = graphRunner.TryGetNext(out var _, out var _, out var _, out var _, out var _, true);
      }
      else if (runningMode == RunningMode.NonBlockingSync)
      {
        yield return new WaitUntil(() => graphRunner.TryGetNext(out var _, out var _, out var _, out var _, out var _, false));
      }
    }
  }
}
