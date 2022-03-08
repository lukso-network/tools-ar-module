// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Linq;
using UnityEngine;

namespace Mediapipe.Unity
{
  public class SelfieMaskAnnotationController : AnnotationController<SelfieMaskAnnotation>
  {
    [SerializeField] private int _maskWidth = 512;
    [SerializeField] private int _maskHeight = 512;
    [SerializeField, Range(0, 1)] private float _minAlpha = 0.9f;
    [SerializeField, Range(0, 1)] private float _maxAlpha = 1.0f;

    private ImageFrame _currentTarget;
    private byte[] _maskArray;

    public void InitScreen()
    {
      _maskArray = new byte[_maskWidth * _maskHeight*4];
      annotation.InitScreen();
    }

    public void DrawNow(ImageFrame target)
    {
      _currentTarget = target;
      UpdateMaskArray(_currentTarget);
      SyncNow();
    }

    public void DrawLater(ImageFrame target)
    {
      UpdateCurrentTarget(target, ref _currentTarget);
      UpdateMaskArray(_currentTarget);
    }

    private void UpdateMaskArray(ImageFrame imageFrame)
    {
      if (imageFrame != null)
      {
        int sz = imageFrame.ChannelSize();
        int w = imageFrame.Width();
        int h = imageFrame.Height();

        var f = new float[w * h];
        var f2 = imageFrame.CopyToFloatBuffer(w * h);

        float s = f2.Sum();

        float mx = f2.Max();
        float my = f2.Max();

        int l = w * h;
        for (int i = 0; i < l; ++i) {
          _maskArray[i] = (byte)(f2[i] * 255);
        }

      // var _ = imageFrame.GetChannel(0, isMirrored, _maskArray);
      }
    }

    protected override void SyncNow()
    {
      isStale = false;
      annotation.Draw(_maskArray, _maskWidth, _maskHeight, _minAlpha, _maxAlpha);
    }
  }
}
