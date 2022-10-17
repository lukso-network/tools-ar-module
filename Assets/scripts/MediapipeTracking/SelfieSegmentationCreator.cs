// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity.SelfieSegmentation {
    public class SelfieSegmentationCreator : ImageSourceSolution<SelfieSegmentationGraph> {
        [SerializeField] private SelfieMaskAnnotationController _selfieMaskAnnotationController;
        [SerializeField] private int maskWidth = 256;
        [SerializeField] private int maskHeight = 256;

        private Texture2D maskTexture;
        private byte[] maskRGBA;
        private bool initied;

        protected override void OnStartRun() {

            maskTexture = new Texture2D(maskWidth, maskHeight, TextureFormat.RGBA32, false);
            maskRGBA = new byte[maskHeight * maskWidth * 4];

            graphRunner.OnSelfieMaskOutput.AddListener(_selfieMaskAnnotationController.DrawLater);
            SetupAnnotationController(_selfieMaskAnnotationController, ImageSourceProvider.ImageSource);
            _selfieMaskAnnotationController.InitScreen();
        }

        protected override void AddTextureFrameToInputStream(TextureFrame textureFrame) {
            graphRunner.AddTextureFrameToInputStream(textureFrame);

        }

        protected override IEnumerator WaitForNextValue() {
            if (runningMode == RunningMode.Sync) {
                var _ = graphRunner.TryGetNext(out var _, true);
            } else if (runningMode == RunningMode.NonBlockingSync) {
                yield return new WaitUntil(() => graphRunner.TryGetNext(out var _, false) != null);
            }
        }

        protected override void WaitForNextValueSync() {
            if (runningMode == RunningMode.Sync) {
                var _ = graphRunner.TryGetNext(out var _, true);
            }
        }

        public override void Play() {

            var startNormally = false;
            if (startNormally) {
                base.Play();
            } else {
                //   if (_coroutine != null) {
                Stop();
                //  }
                PlayPredecessor();
                StartCoroutine(PrepareCustomRun());
            }
        }



        protected IEnumerator PrepareCustomRun() {
            var graphInitRequest = graphRunner.WaitForInit(runningMode);
            var imageSource = ImageSourceProvider.ImageSource;

            if (isVideoPlayerController) {
                yield return imageSource.Play();
            }



            yield return new WaitUntil(() => imageSource.isPrepared);
            if (!imageSource.isPrepared) {
                Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
                yield break;
            }

            // Use RGBA32 as the input format.
            // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
            textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
            //SetupScreen(imageSource);

            yield return graphInitRequest;
            if (graphInitRequest.isError) {
                Logger.LogError(TAG, graphInitRequest.error);
                yield break;
            }


            graphRunner.StartRun(imageSource);
            OnStartRun();


            OnPrepared();
        }

        protected override void OnPrepared() {
            _coroutine = StartCoroutine(Test());
            initied = true;
        }

        private IEnumerator Test() {
            yield break;
            while (true) {
                CaptureSelfieToTexture();
                yield return new WaitForEndOfFrame();
                //yield return ProcessImage(true);
            }
        }

        public void CaptureSegmentation() {
            //ProcessImageSync();

            CaptureSelfieToTexture();
            //StartCoroutine(CaptureSegmentationCoroutine());
        }

        private IEnumerator CaptureSegmentationCoroutine() {
            yield return ProcessImage(true);

            //while (true) {
            //yield return ProcesImage(true);
            //}
        }

        public Texture2D CaptureSelfieToTexture() {
            if (!initied) {
                return null;
            }
            var imageSource = ImageSourceProvider.ImageSource;

            if (!textureFramePool.TryGetTextureFrame(out var textureFrame)) {
                return null;
            }

            ReadFromImageSource(imageSource, textureFrame);
            AddTextureFrameToInputStream(textureFrame);

            var mask = graphRunner.TryGetNext(out var _, true, false);
            if (mask != null) {
                //maskTexture.LoadRawTextureData(mask.GetPixels32());
                //maskTexture.SetPixels32(mask.GetPixels32());

                ConvertMask(mask);
                return maskTexture;
            }

            return null;

        }

        public Texture2D GetLastMask() {
            return maskTexture;
        }

        private void ConvertMask(ImageFrame mask) {

            int sz = mask.ChannelSize();
            int w = mask.Width();
            int h = mask.Height();
            int count = w * h;

            var floatArray = mask.CopyToFloatBuffer(w * h);

            unsafe {
                fixed (float* ptr = floatArray) {
                    fixed (byte* outPtr = maskRGBA) {
                        var p1 = outPtr;
                        var p2 = ptr;
                        for (var i = 0; i < count; ++i) {
                            byte v = (byte)(*p2 * 255);
                            *p1++ = v;
                            *p1++ = v;
                            *p1++ = v;
                            *p1++ = 255;

                            ++p2;
                        }
                    }

                }
            }
            maskTexture.LoadRawTextureData(maskRGBA);
            maskTexture.Apply();
        }
    }
}
