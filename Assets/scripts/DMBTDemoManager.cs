using UnityEngine;
using Joint = Assets.Joint;
using Assets.scripts.Avatar;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
//using Newtonsoft.Json.Linq;
using System.Linq;
using Assets.Demo.Scripts;
using Assets.PoseEstimator;
using System.ComponentModel;
using UnityWeld.Binding;
using Assets;
using Mediapipe;
using System.Text.RegularExpressions;
using Lukso;
using Skeleton = Lukso.Skeleton;


struct Landmarks
{
  public NormalizedLandmarkList lastLandmarks { get; private set; }
  public long lastValidTime { get; private set; }

  public void Set(NormalizedLandmarkList landmarks) {
    if (landmarks != null) {
      lastLandmarks = landmarks;
      lastValidTime = time();
    }
  }

  public NormalizedLandmarkList GetActualIfValid(long durationMS) {
    return IsValid(durationMS) ? lastLandmarks : null;
  }

  public bool IsValid(long durationMs) {
    return time() - lastValidTime < durationMs;
  }

  private long time() {
    return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
  }
}

public class FPSCounter
{
  const float fpsMeasurePeriod = 0.5f;
  private int counter = 0;
  private float lastTime = 0;
  private float fps;

  public float UpdateFps() {
    counter++;
    float t = Time.realtimeSinceStartup;
    if (t > lastTime + fpsMeasurePeriod) {
      fps = counter / (t - lastTime);
      counter = 0;
      lastTime = t;
    }
    return fps;
  }
}

namespace DeepMotion.DMBTDemo
{


  [Binding]
  public class DMBTDemoManager : MonoBehaviour
  {


    [Serializable]
    public class AvatarDescription
    {
      public string id;
      public GameObject prefab;
    }

    public StatisticDisplay display;
    public FilterSettings scaleFilter;
    public SkeletonManager skeletonManager;
    public GameObject facePrefab;
    public GameObject hat;

    [SerializeField] private Camera screenCamera;

    private GameObject face;
    private Mesh faceMesh;
    public Mesh FaceMesh => faceMesh;
    private Quaternion faceDirection = Quaternion.identity;
    public Quaternion FaceDirection => faceDirection;

    [Range(0, 2)]
    public float scaleDepth = 0.5f;

    public delegate void OnNewPoseHandler(bool skeletonExist);

    public delegate void OnNewFaceHandler(NormalizedLandmarkList faceLandmarks, Texture2D texture, bool flipped);

    public event OnNewPoseHandler newPoseEvent;
    public event OnNewFaceHandler newFaceEvent;

    private Texture2D lastFrame;
    private bool paused = false;
    private Vector3[] skeletonPoints;

    private Landmarks skeletonLandmarks;
    private Landmarks faceLandmarks;

    public Texture2D GetLastFrame() {
      return lastFrame;
    }

    private readonly int[] FLIP_POINTS = new int[] { 0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 17, 17, 20, 19, 22, 21, 24, 23, 26, 25, 28, 27, 30, 29, 32, 31 };

    private FPSCounter counter = new FPSCounter();
    public bool ShowTransparentFace {
      get => face.GetComponent<TransparentMaterialRenderer>().enabled;
      set => face.GetComponent<TransparentMaterialRenderer>().enabled = value;
    }

    void OnValidate() {
      scaleFilter.SetModified();
    }

    void Start() {
      InitFace();
      Init();
    }

    private void Init() {

      try {

        /*     var foundedAvatar = Array.Find(avatars, x => x.id == avatarType);
             if (foundedAvatar == null) {
                 Debug.LogError("Could not found avatar by id");
             }

             var obj = Instantiate(foundedAvatar.prefab, transform);
             obj.SetActive(false);
             Utils.PreparePivots(obj);
             /*Skeleton = CreateSkeleton(obj);
             controller = new Assets.Avatar(obj, Skeleton);
             controller.settings = ikSettings;
             controller.SetIkSource();*/


        // obj.SetActive(false);

        /*poseScaler = GetComponent<PoseScaler>();

        poseScaler.Init();

        obj = Instantiate(foundedAvatar.prefab, transform);
        obj.name = "Initial debug copy";
        obj.SetActive(false);
        Utils.PreparePivots(obj);
        initialAvatar = new Assets.Avatar(obj, CreateSkeleton(obj));
        */
      } catch (Exception ex) {
        Debug.LogError("DMBTManage failed");
        Debug.LogException(ex);
      }

    }

    protected Vector3 ScaleVector(Transform transform) {
      return new Vector3(1 * transform.localScale.x, 1 * transform.localScale.y, transform.localScale.z);
    }

    protected Vector3 GetFacePointForRotation(Vector3 scaleVector, NormalizedLandmark landmark, bool isFlipped, float scale) {
      var v = ToVector3(landmark);
      var relX = (isFlipped ? -1 : 1) * (v.x - 0.5f);
      var relY = 0.5f - v.y;
      return Vector3.Scale(new Vector3(relX, relY, v.z), scaleVector);
    }

    protected Vector3 GetPositionFromNormalizedPoint(Vector3 scaleVector, Vector3 v, bool isFlipped, float zShift, float perspectiveScale, bool inZDirection = false) {
      var relX = (isFlipped ? -1 : 1) * (v.x - 0.5f);
      var relY = 0.5f - v.y;

      var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), scaleVector);// + screenTransform.position;
      var dir = (screenCamera.transform.position - pos3d).normalized;
      // dir /= Math.Abs(dir.z);
      pos3d += dir * (-(v.z + zShift)) * scaleVector.z * perspectiveScale;
      return pos3d;
    }

    private float CalculateZShift(Transform screenTransform, Vector3[] skeletonPoints, NormalizedLandmarkList faceLandmarks, bool isFlipped, float perspectiveScale) {

      if (faceLandmarks == null || faceLandmarks.Landmark.Count == 0) {
        return 0;
      }
      var scaleVector = ScaleVector(screenTransform);
      var from = LandmarkToVector(faceLandmarks.Landmark[4]); //nose

      var l = skeletonPoints[(int)Skeleton.Point.LEFT_SHOULDER];
      var r = skeletonPoints[(int)Skeleton.Point.RIGHT_SHOULDER];
      var lh = skeletonPoints[(int)Skeleton.Point.LEFT_HIP];
      var rh = skeletonPoints[(int)Skeleton.Point.RIGHT_HIP];

      var rlDir = (l - r).normalized;
      var upDir = ((l + r) / 2 - (lh + rh) / 2).normalized;
      var len = (l - r).magnitude;

      var nosePose = (l + r) / 2 + len * upDir * 0.5f + Vector3.Cross(upDir, rlDir) * len * 0.2f;

      var to = nosePose;
      //var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), ScaleVector(screenTransform)) + screenTransform.position;
      //pos3d += (screenCamera.transform.position - pos3d).normalized * (-z) * screenTransform.localScale.y * perspectiveScale;

      var relX = (isFlipped ? -1 : 1) * (from.x - 0.5f);
      var relY = 0.5f - from.y;
      var pos3d = Vector3.Scale(new Vector3(relX, relY, 0), scaleVector);
      float delta = -(to.z - pos3d.z) / (screenCamera.transform.position - pos3d).normalized.z / (scaleVector.z * perspectiveScale) - from.z;


      //var testP = GetPositionFromNormalizedPoint(scaleVector, from, false, delta, perspectiveScale, false);

      //Debug.Log((testP - to).z);
      return delta;
    }

    private Vector3 ToVector3(NormalizedLandmark landmark) {
      return new Vector3(landmark.X, landmark.Y, landmark.Z);
    }

    private float GetSpineSize(NormalizedLandmarkList landmarklist) {

      var left = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.LEFT_HIP]);
      var right = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.RIGHT_HIP]);

      var leftArm = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.LEFT_SHOULDER]);
      var rightArm = ToVector3(landmarklist.Landmark[(int)Skeleton.Point.RIGHT_SHOULDER]);

      //probably Vector2 needed
      var l = ((left + right) / 2 - (leftArm + rightArm) / 2).magnitude;

      return l;
    }

    private Vector3 LandmarkToVector(NormalizedLandmark lnd) {
      return new Vector3(lnd.X, lnd.Y, lnd.Z);
    }

    public void PauseProcessing(bool pause) {
      this.paused = pause;
    }

    private Vector3[] TransformPoints(Transform transform, NormalizedLandmarkList landmarkList, bool flipped, float zShift = 0, float spineSize = -1) {
      if (spineSize < 0) {
        spineSize = 1;
      }
      int count = landmarkList.Landmark.Count;
      var scaleVector = ScaleVector(transform);

      //TODOLK - memory 
      var points = new Vector3[count];
      for (int i = 0; i < count; ++i) {
        var landmark = landmarkList.Landmark[i];
        var p = GetPositionFromNormalizedPoint(scaleVector, LandmarkToVector(landmark), flipped, zShift, spineSize);
        points[i] = p;
      }

      return points;
    }


    private float defaultFaceSize;
    private float faceGeomCoef;
    private void InitFace() {
      face = Instantiate(facePrefab, transform);
      face.SetActive(false);
      face.AddComponent<TransparentMaterialRenderer>();
      faceMesh = face.GetComponent<MeshFilter>().mesh;
      faceMesh.RecalculateNormals();

      var points = faceMesh.vertices;

      var t = points[10];
      var b = points[152];
      var r = points[33];
      var l = points[263];
      defaultFaceSize = ((t - b).magnitude * (l - r).magnitude);

      var nose = points[4];
      var c0 = (r + l) / 2;
      var d1 = nose - c0;
      var d2 = (l - r);

      faceGeomCoef = Vector3.Dot(d1, d1) / Vector3.Dot(d2, d2);
    }



    private float[] times = new float[] { 0, 0, 0, 0, 0 };

    private Vector3[] UpdateSkeleton(Transform screenTransform, NormalizedLandmarkList landmarkList, bool flipped) {

      if (landmarkList == null) {

        return null;
      }

      var t0 = Time.realtimeSinceStartup;
      var spineSize = GetSpineSize(landmarkList);
      float scale = screenCamera.aspect > 1 ? screenCamera.aspect * screenCamera.aspect : 1;
      scale /= 2.8f;
      var points = TransformPoints(screenTransform, landmarkList, flipped, 0, scale);
      /*
      var min = new Vector3(1000, 1000, 100);
      var max = new Vector3(-1000, -1000, -100);
      foreach(var p in points) {
        min = Vector3.Min(min, p);
        max = Vector3.Max(max, p);
      }

      Debug.Log("" + min + " " + max);
      */

      //TODO make it faster
      if (flipped) {
        //TODOLK
        var fPoints = new Vector3[points.Length];
        int maxSize = Math.Min(points.Length, FLIP_POINTS.Length);
        for (int i = 0; i < maxSize; ++i) {
          fPoints[i] = points[FLIP_POINTS[i]];
        }
        points = fPoints;
      }

      var ps = points.Select(x => new Vector3?(x)).ToArray();

      var t = Time.realtimeSinceStartup;
      skeletonManager.UpdatePose(ps);
      var dt = Time.realtimeSinceStartup - t;



      times[0] = dt;
      times[1] = t - t0;

      return points;
    }

    private void UpdateFace(Transform screenTransform, NormalizedLandmarkList faceLandmarks, bool flipped, Vector3[] skelPoints) {
      if (faceLandmarks == null) {
        return;
      }
      float faceScale = screenCamera.aspect > 1 ? screenCamera.aspect * screenCamera.aspect : 1;
      var faceNoseShift = CalculateZShift(screenTransform, skelPoints, faceLandmarks, flipped, faceScale);

      //faceMesh.vertices = points;
      //TOFO


      var points = TransformPoints(screenTransform, faceLandmarks, flipped, faceNoseShift, faceScale);
      if (points.Length == 0) {
        return;
      }

      faceMesh.vertices = points;

      var nose = points[4];
      var t = points[10];
      var b = points[152];
      var r = points[33];
      var l = points[263];

      //Debug.Log("face:" + (l - r) * 100 + " " + (nose - (l + r) / 2)*100);

      var center = (t + b + r + l) / 4;
      // Debug.Log("Magn:" + (t - b).magnitude + " " + (l - r).magnitude);
      var scale = Mathf.Sqrt(((t - b).magnitude * (l - r).magnitude) / defaultFaceSize);

      var up = (t - b).normalized;
      var left = (l - r).normalized;
      var front = Vector3.Cross(left, up);
      if (flipped) {
        front = -front;
      }

      faceDirection = Quaternion.LookRotation(front, up);
      hat.transform.localScale = new Vector3(scale, scale, scale);
      hat.transform.rotation = faceDirection;
      hat.transform.localPosition = nose;
    }


    internal void OnNewPose(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped, Texture2D texture) {
      if (paused) {
        return;
      }

      if (!enabled) {
        return;
      }

      if (faceLandmarks == null) {
        faceLandmarks = new NormalizedLandmarkList();
      }

      lastFrame = texture;
      face.SetActive(faceLandmarks != null && faceLandmarks.Landmark.Count > 0);
      var t = Time.realtimeSinceStartup;
      float t2 = 0;
      float t3 = 0;
      float t4 = 0;

      if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
        newPoseEvent(false);

        var fps0 = counter.UpdateFps();
        display.LogValue($"FPS:{fps0:0.0}", times[0], times[1], 0, 0, 0);
        return;
      }

      var t1 = Time.realtimeSinceStartup;

      try {
        var scale = screenTransform.localScale;
        scale.z = scaleDepth;
        screenTransform.localScale = scale;

        var skelPoints = UpdateSkeleton(screenTransform, landmarkList, flipped);
        t2 = Time.realtimeSinceStartup;

        UpdateFace(screenTransform, faceLandmarks, flipped, skelPoints);
        t3 = Time.realtimeSinceStartup;

        newFaceEvent(faceLandmarks, texture, flipped);

        t4 = Time.realtimeSinceStartup;


        //  Debug.Log("light:" + (t4 - t3));

      } catch (Exception ex) {
        Debug.LogError("DMBTManage new pose failed");
        Debug.LogException(ex);
      }

      newPoseEvent(true);
      var fps = counter.UpdateFps();
      display.LogValue($"FPS:{fps:0.0}", times[0], times[1], t1 - t, t2 - t1, t3 - t2, t4 - t3);

    }

    internal void OnNewPose2(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped, Texture2D texture) {
      if (paused) {
        return;
      }

      if (!enabled) {
        return;
      }

      lastFrame = texture;

      //TODO
      if (landmarkList == null && faceLandmarks == null) {
        face.SetActive(false);
        //face.SetActive(faceLandmarks != null && faceLandmarks.Landmark.Count > 0);
      } else if (faceLandmarks != null) {
        face.SetActive(true);
      }


      var t = Time.realtimeSinceStartup;
      float t2 = 0;
      float t3 = 0;
      float t4 = 0;

      /*
            if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
                newPoseEvent(false);

                var fps0 = counter.UpdateFps();
                display.LogValue($"FPS:{fps0:0.0}", times[0], times[1], 0,0,0);
                return;
            }
      */

      var t1 = Time.realtimeSinceStartup;

      try {
        var scale = screenTransform.localScale;
        scale.z = scaleDepth;
        screenTransform.localScale = scale;

        var skelPoints = UpdateSkeleton(screenTransform, landmarkList, flipped);
        if (skelPoints == null) {
          skelPoints = this.skeletonPoints;
        } else {
          this.skeletonPoints = skelPoints;
        }
        if (skelPoints == null) {
          return;
        }


        t2 = Time.realtimeSinceStartup;

        if (faceLandmarks != null) {
          UpdateFace(screenTransform, faceLandmarks, flipped, skelPoints);
          t3 = Time.realtimeSinceStartup;

          newFaceEvent(faceLandmarks, texture, flipped);
        }



        t4 = Time.realtimeSinceStartup;


        //  Debug.Log("light:" + (t4 - t3));

      } catch (Exception ex) {
        Debug.LogError("DMBTManage new pose failed");
        Debug.LogException(ex);
      }

      if (landmarkList != null) {
        newPoseEvent(true);
      } else {

      }
      var fps = counter.UpdateFps();
      display.LogValue($"FPS:{fps:0.0}", times[0], times[1], t1 - t, t2 - t1, t3 - t2, t4 - t3);

    }




    internal void OnNewPose3(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped, Texture2D texture) {
      lastFrame = texture;
      if (paused) {
        return;
      }

      if (!enabled) {
        return;
      }

      this.skeletonLandmarks.Set(landmarkList);
      this.faceLandmarks.Set(faceLandmarks);

      const long VALID_DURATION = 2000; //2 sec
      landmarkList = this.skeletonLandmarks.GetActualIfValid(VALID_DURATION);
      faceLandmarks = this.faceLandmarks.GetActualIfValid(VALID_DURATION);

      OnNewPose3(screenTransform, landmarkList, faceLandmarks, flipped);
    }

    private void OnNewPose3(Transform screenTransform, NormalizedLandmarkList landmarkList, NormalizedLandmarkList faceLandmarks, bool flipped) {
        
      
      face.SetActive(faceLandmarks != null);
      var t = Time.realtimeSinceStartup;
      float t2 = 0;
      float t3 = 0;
      float t4 = 0;

      if (!enabled || landmarkList == null || landmarkList.Landmark.Count == 0) {
        newPoseEvent(false);

        var fps0 = counter.UpdateFps();
        display.LogValue($"FPS:{fps0:0.0}", times[0], times[1], 0, 0, 0);
        return;
      }

      var t1 = Time.realtimeSinceStartup;

      try {
        var scale = screenTransform.localScale;
        scale.z = scaleDepth;
        screenTransform.localScale = scale;

        var skelPoints = UpdateSkeleton(screenTransform, landmarkList, flipped);
        t2 = Time.realtimeSinceStartup;

        UpdateFace(screenTransform, faceLandmarks, flipped, skelPoints);
        t3 = Time.realtimeSinceStartup;

        newFaceEvent(faceLandmarks, lastFrame, flipped);

        t4 = Time.realtimeSinceStartup;


        //  Debug.Log("light:" + (t4 - t3));

      } catch (Exception ex) {
        Debug.LogError("DMBTManage new pose failed");
        Debug.LogException(ex);
      }

      newPoseEvent(true);
      var fps = counter.UpdateFps();
      display.LogValue($"FPS:{fps:0.0}", times[0], times[1], t1 - t, t2 - t1, t3 - t2, t4 - t3);

    }

    internal void ResetAvatar() {
      //           controller.CopyRotationAndPositionFromAvatar(initialAvatar);
    }


  }
}
