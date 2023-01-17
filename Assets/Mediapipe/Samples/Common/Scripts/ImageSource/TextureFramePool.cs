// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mediapipe.Unity
{
  public class TextureFramePool : MonoBehaviour
  {
    private const string _TAG = nameof(TextureFramePool);

    [SerializeField] private int _poolSize = 10;

    private readonly object _formatLock = new object();
    private int _textureWidth = 0;
    private int _textureHeight = 0;
    private TextureFormat _format = TextureFormat.RGBA32;

    private Queue<TextureFrame> _availableTextureFrames;
    private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
    /// <remarks>
    ///   key: TextureFrame's instance ID
    /// </remarks>
    private Dictionary<Guid, TextureFrame> _textureFramesInUse;
    private const long lifeTime = 100000;

    /// <returns>
    ///   The total number of texture frames in the pool.
    /// </returns>
    public int frameCount
    {
      get
      {
        var availableTextureFramesCount = _availableTextureFrames == null ? 0 : _availableTextureFrames.Count;
        var textureFramesInUseCount = _textureFramesInUse == null ? 0 : _textureFramesInUse.Count;

        return availableTextureFramesCount + textureFramesInUseCount;
      }
    }

    private void Start()
    {
      _availableTextureFrames = new Queue<TextureFrame>(_poolSize);
      _textureFramesInUse = new Dictionary<Guid, TextureFrame>();
      timer.Start();
    }

       protected long GetCurrentTimestampMicrosec() {
            return timer.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);
        }

    private void OnDestroy()
    {
      lock (((ICollection)_availableTextureFrames).SyncRoot)
      {
        _availableTextureFrames.Clear();
        _availableTextureFrames = null;
      }

      lock (((ICollection)_textureFramesInUse).SyncRoot)
      {
        foreach (var textureFrame in _textureFramesInUse.Values)
        {
          textureFrame.OnRelease.RemoveListener(OnTextureFrameRelease);
        }
        _textureFramesInUse.Clear();
        _textureFramesInUse = null;
      }
    }

    public void Reset() {
      lock (((ICollection)_availableTextureFrames).SyncRoot) {
        _availableTextureFrames.Clear();
        _availableTextureFrames = null;
      }

      lock (((ICollection)_textureFramesInUse).SyncRoot) {
        foreach (var textureFrame in _textureFramesInUse.Values) {
          textureFrame.OnRelease.RemoveListener(OnTextureFrameRelease);
        }
        _textureFramesInUse.Clear();
        _textureFramesInUse = null;
      }

      _availableTextureFrames = new Queue<TextureFrame>(_poolSize);
      _textureFramesInUse = new Dictionary<Guid, TextureFrame>();
    }

    public void ResizeTexture(int textureWidth, int textureHeight, TextureFormat format)
    {
      lock (_formatLock)
      {
        _textureWidth = textureWidth;
        _textureHeight = textureHeight;
        _format = format;
      }
    }

    public void ResizeTexture(int textureWidth, int textureHeight)
    {
      ResizeTexture(textureWidth, textureHeight, _format);
    }

    public bool TryGetTextureFrame(out TextureFrame outFrame)
    {
      TextureFrame nextFrame = null;
      lock (((ICollection)_availableTextureFrames).SyncRoot)
      {
        Debug.Log($"Available: {_availableTextureFrames.Count}, InUse: {_textureFramesInUse.Count} ");
        if (_poolSize <= frameCount)
        {
          while (_availableTextureFrames.Count > 0)
          {
            var textureFrame = _availableTextureFrames.Dequeue();

            if (!IsStale(textureFrame))
            {
              nextFrame = textureFrame;
              break;
            }
          }
        }
        else
        {
          nextFrame = CreateNewTextureFrame();
        }
      }

     var time = GetCurrentTimestampMicrosec();
     if (nextFrame == null)
      {
        outFrame = null;
        RemoveOutdated(time, lifeTime);
        return false;
      }

     
      nextFrame.activationTime = time;
      lock (((ICollection)_textureFramesInUse).SyncRoot)
      {
        _textureFramesInUse.Add(nextFrame.GetInstanceID(), nextFrame);
        RemoveOutdated(time, lifeTime);
      }

      nextFrame.WaitUntilReleased();
      outFrame = nextFrame;
      return true;
    }

    private void OnTextureFrameRelease(TextureFrame textureFrame)
    {
      lock (((ICollection)_textureFramesInUse).SyncRoot)
      {
        textureFrame.activationTime = 0;
        if (!_textureFramesInUse.Remove(textureFrame.GetInstanceID()))
        {
          // won't be run
          Logger.LogWarning(_TAG, "The released texture does not belong to the pool");
          return;
        }

        if (frameCount > _poolSize || IsStale(textureFrame))
        {
          return;
        }
        _availableTextureFrames.Enqueue(textureFrame);
      }
    }

    public void RemoveOutdated(long currentTime, long lifetime) {
            lock (((ICollection)_textureFramesInUse).SyncRoot) {
                var toDelete = _textureFramesInUse.Where(kv => kv.Value.activationTime != 0 && currentTime - kv.Value.activationTime > lifetime).ToList();
                foreach (var t in toDelete) {
                    t.Value.Release();
                }
            }
        }

    private bool IsStale(TextureFrame textureFrame)
    {
      lock (_formatLock)
      {
        return textureFrame.width != _textureWidth || textureFrame.height != _textureHeight;
      }
    }

    private TextureFrame CreateNewTextureFrame()
    {
      var textureFrame = new TextureFrame(_textureWidth, _textureHeight, _format);
      textureFrame.OnRelease.AddListener(OnTextureFrameRelease);

      return textureFrame;
    }
  }
}
