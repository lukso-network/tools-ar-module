namespace Lukso
{
  public partial class Skeleton
  {

    public const int POINT_COUNT = 33;
    public enum Point : int
    {
      DUMMY = -4,
      CHEST = -3,
      SPINE = -2,
      HIPS = -1,

      NOSE = 0,
      LEFT_EYE_INNER = 1,
      LEFT_EYE = 2,
      LEFT_EYE_OUTER = 3,
      RIGHT_EYE_INNER = 4,
      RIGHT_EYE = 5,
      RIGHT_EYE_OUTER = 6,
      LEFT_EAR = 7,
      RIGHT_EAR = 8,
      MOUTH_LEFT = 9,
      MOUTH_RIGHT = 10,
      LEFT_SHOULDER = 11,
      RIGHT_SHOULDER = 12,
      LEFT_ELBOW = 13,
      RIGHT_ELBOW = 14,
      LEFT_WRIST = 15,
      RIGHT_WRIST = 16,
      LEFT_PINKY = 17,
      RIGHT_PINKY = 18,
      LEFT_INDEX = 19,
      RIGHT_INDEX = 20,
      LEFT_THUMB = 21,
      RIGHT_THUMB = 22,
      LEFT_HIP = 23,
      RIGHT_HIP = 24,
      LEFT_KNEE = 25,
      RIGHT_KNEE = 26,
      LEFT_ANKLE = 27,
      RIGHT_ANKLE = 28,
      LEFT_HEEL = 29,
      RIGHT_HEEL = 30,
      LEFT_FOOT_INDEX = 31,
      RIGHT_FOOT_INDEX = 32,
    }

    private readonly Point[] MIRROR_POINTS = {
          Point.DUMMY,
          Point.RIGHT_EYE_INNER,
          Point.RIGHT_EYE,
          Point.RIGHT_EYE_OUTER,
          Point.LEFT_EYE_INNER,
          Point.LEFT_EYE,
          Point.LEFT_EYE_OUTER,
          Point.RIGHT_EAR,
          Point.LEFT_EAR,
          Point.MOUTH_RIGHT,
          Point.MOUTH_LEFT,
          Point.RIGHT_SHOULDER,
          Point.LEFT_SHOULDER,
          Point.RIGHT_ELBOW,
          Point.LEFT_ELBOW,
          Point.RIGHT_WRIST,
          Point.LEFT_WRIST,
          Point.RIGHT_PINKY,
          Point.LEFT_PINKY,
          Point.RIGHT_INDEX,
          Point.LEFT_INDEX,
          Point.RIGHT_THUMB,
          Point.LEFT_THUMB,
          Point.RIGHT_HIP,
          Point.LEFT_HIP,
          Point.RIGHT_KNEE,
          Point.LEFT_KNEE,
          Point.RIGHT_ANKLE,
          Point.LEFT_ANKLE,
          Point.RIGHT_HEEL,
          Point.LEFT_HEEL,
          Point.RIGHT_FOOT_INDEX,
          Point.LEFT_FOOT_INDEX,
      };

    public Skeleton.Point GetMirrored(Skeleton.Point point) {
      return (int)point >= 0 ? MIRROR_POINTS[(int)point] : Point.DUMMY;
    }


  }
}
