// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.scripts.UI
{
  public class ScaleTexture : MonoBehaviour
  {
    public enum Mode
    {
      NONE,
      IMAGE_ONLY,
      SCALE_SKELETON,
      SCALE_IMAGE

    }

    public RawImage imageScreen;
    public Vector2 scale = new Vector2(1, 1);
    public Vector2 offset;
    public Mode mode = Mode.NONE;

    private Vector3 mouseXY = Vector3.zero;
    private bool pressed = false;
    private bool isFace = false;

    //TODO DEBUGGING ONLY
    public static ScaleTexture instance;


    // Use this for initialization
    void Start() {
      //TESTING ONLY
      instance = this;
    }

    private void OnValidate() {
      if (mode == Mode.NONE) {
        imageScreen.uvRect = new Rect(0, 0, 1, 1);
        scale = Vector2.one;
        offset = Vector2.zero;
      }
    }

    // Update is called once per frame
    void Update() {
      if (imageScreen.uvRect.width < 0) {
        isFace = true;
        return;
      }
      

      if (mode == Mode.NONE) {
        imageScreen.uvRect = new Rect(0, 0, 1, 1);
        return;
      }

      if (Input.GetMouseButtonDown(0)) {
        pressed = true;
        mouseXY = Input.mousePosition;
      }

      if (Input.GetMouseButtonUp(0)) {
        pressed = false;
      }

      float s = scale[0];
      var ds = -Input.GetAxis("Mouse ScrollWheel");
      s += ds;
      offset.x = -ds * 0.5f + offset.x;
      offset.y = -ds * 0.5f + offset.y;
      s = Math.Max(s, 0.1f);

      if (pressed) { 
     
        var delta = Input.mousePosition - mouseXY;
        mouseXY = Input.mousePosition;

        offset -= new Vector2(delta.x, delta.y) * 0.001f*s;
      }

      scale = new Vector2(s, s);
      if (mode == Mode.SCALE_SKELETON) {
        imageScreen.uvRect = new Rect(0, 0, 1, 1);
      } else {
        imageScreen.uvRect = new Rect(offset.x, offset.y, s, s);
      }
    }
  }
}
