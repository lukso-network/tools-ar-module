// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe.Unity.SkeletonTracking;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using static Mediapipe.Unity.GraphRunner;
using Debug = UnityEngine.Debug;

class Timer
{
  public int count;
  private Stopwatch stopwatch = new Stopwatch();
  public void Start() {
    stopwatch.Start();
    count += 1;
  }

  public void Stop() {
    stopwatch.Stop();
  }

  public float ms => 1000 * (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
  public float msAver => ms / (count == 0 ? 1 : count);
}

class Stat
{
  public Timer t1 = new Timer();
  public Timer t2 = new Timer();
  public Timer t3 = new Timer();
  public Timer t4 = new Timer();
  public Timer t5 = new Timer();
  public Timer t6 = new Timer();

  public void Log(string label="") {
    string s = $"\n{label}\nt1={t1.msAver} {t1.count}\nt2={t2.msAver} {t2.count}\nt3={t3.msAver}  {t3.count}\nt4={t4.msAver}  {t4.count}\nt5={t5.msAver}  {t5.count}\nt6={t6.msAver}  {t6.count}\n";
    UnityEngine.Debug.Log(s);
  }

}

namespace Mediapipe.Unity
{
  public abstract class ImageSourceSolution<T> : Solution where T : GraphRunner
  {
    [SerializeField] protected Screen screen;
    [SerializeField] protected T graphRunner;
    [SerializeField] protected TextureFramePool textureFramePool;
    [SerializeField] protected bool isVideoPlayerController = true;

    protected Coroutine _coroutine;

    public RunningMode runningMode;

    public long timeoutMillisec {
      get => graphRunner.timeoutMillisec;
      set => graphRunner.timeoutMillisec = value;
    }

    public override void Play() {
      if (_coroutine != null) {
        Stop();
      }
      base.Play();
      _coroutine = StartCoroutine(Run());
    }

    protected void PlayPredecessor() {
      base.Play();
    }

    public override void Pause() {
      base.Pause();

      if (isVideoPlayerController) {
        ImageSourceProvider.ImageSource.Pause();
      }
    }

    public override void Resume() {
      base.Resume();
      if (isVideoPlayerController) {
        StartCoroutine(ImageSourceProvider.ImageSource.Resume());
      }
    }
    int frameIdx = 0;

    public override void Stop() {
      base.Stop();
      //TODO
      //textureFramePool.Reset();

      graphRunner.onDataProcessed -= RenderCurrentFrame;
      if (_coroutine != null) {
        StopCoroutine(_coroutine);
      }
      if (isVideoPlayerController) {
        ImageSourceProvider.ImageSource.Stop();
      }
      graphRunner.Stop();
    }

    protected IEnumerator Run() {
      var graphInitRequest = graphRunner.WaitForInit(runningMode);
      var imageSource = ImageSourceProvider.ImageSource;

      if (isVideoPlayerController) {
        yield return imageSource.Play();
      }

      if (!imageSource.isPrepared) {
        Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
      textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
      if (isVideoPlayerController) {
        SetupScreen(imageSource);
      }

      yield return graphInitRequest;
      if (graphInitRequest.isError) {
        Logger.LogError(TAG, graphInitRequest.error);
        yield break;
      }

      graphRunner.onDataProcessed += RenderCurrentFrame;
      graphRunner.StartRun(imageSource);
      OnStartRun();
    //  Debug.unityLogger.logEnabled = false;

      var waitWhilePausing = new WaitWhile(() => isPaused);
      while (true) {
        if (isPaused) {
          yield return waitWhilePausing;
        }


        // Debug.Log("Save image:" + frameIdx);
        // ScreenCapture.CaptureScreenshot($"out/screenshot_{frameIdx:00000}_{Time.frameCount}.png");
        // frameIdx += 1;
        // yield return new WaitForEndOfFrame();
        //continue;

        //Thread.Sleep(100);
        //continue;

        if (!textureFramePool.TryGetTextureFrame(out var textureFrame)) {
          yield return new WaitForEndOfFrame();
          continue;
        }

        while (!imageSource.didUpdateSinceLastAsk) {
          // Debug.Log("Wait:" + Time.frameCount);
          yield return new WaitForSecondsRealtime(0.001f);
          //yield return new WaitForEndOfFrame();
          //break;
        }
        


        // Copy current image to TextureFrame
        ReadFromImageSource(imageSource, textureFrame);
        AddTextureFrameToInputStream(textureFrame);

        if (runningMode.IsSynchronous()) {
          RenderCurrentFrame(textureFrame);
          var frame = Time.frameCount;
          yield return WaitForNextValue();
          if (Time.frameCount == frame) {
            yield return new WaitForEndOfFrame();
          }
        } else {
          yield return new WaitForEndOfFrame();
        }


        }
      }

    protected virtual void SetupScreen(ImageSource imageSource) {
      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);
    }


    private int prevRenderFrame = 0;
    protected virtual void RenderCurrentFrame(TextureFrame textureFrame) {
      if (Time.frameCount == prevRenderFrame) {
        return;
      }
      screen.ReadSync(textureFrame);
      prevRenderFrame = Time.frameCount;
    }

    protected abstract void OnStartRun();

    protected abstract void AddTextureFrameToInputStream(TextureFrame textureFrame);

    protected abstract IEnumerator WaitForNextValue();
    protected virtual void WaitForNextValueSync() { }
    protected virtual void OnPrepared() { }



    protected IEnumerator PrepareCustomRun() {
      var graphInitRequest = graphRunner.WaitForInit(runningMode);
      var imageSource = ImageSourceProvider.ImageSource;

      if (isVideoPlayerController) {
        yield return imageSource.Play();
      }

      yield break;
      if (!imageSource.isPrepared) {
        Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
      textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
      SetupScreen(imageSource);

      yield return graphInitRequest;
      if (graphInitRequest.isError) {
        Logger.LogError(TAG, graphInitRequest.error);
        yield break;
      }

      graphRunner.StartRun(imageSource);
      OnStartRun();


      OnPrepared();
  }

    public IEnumerator ProcessImage(bool waitEndOfFrame) {
      var imageSource = ImageSourceProvider.ImageSource;
      if (isPaused) {
        yield break;
      }

      if (!textureFramePool.TryGetTextureFrame(out var textureFrame)) {
        yield return new WaitForEndOfFrame();
        yield break;
      }

      // Copy current image to TextureFrame
      ReadFromImageSource(imageSource, textureFrame);
      AddTextureFrameToInputStream(textureFrame);
      if (waitEndOfFrame) {
        yield return new WaitForEndOfFrame();
      }

      if (runningMode.IsSynchronous()) {
        RenderCurrentFrame(textureFrame);
        yield return WaitForNextValue();
      }

    }

    public void ProcessImageSync() {
      var imageSource = ImageSourceProvider.ImageSource;

      if (!textureFramePool.TryGetTextureFrame(out var textureFrame)) {
        return;
      }

      // Copy current image to TextureFrame
      ReadFromImageSource(imageSource, textureFrame);
      AddTextureFrameToInputStream(textureFrame);
      RenderCurrentFrame(textureFrame);
      WaitForNextValueSync();
    }
  }
}
