using Assets;
using Assets.Demo.Scripts;
using DeepMotion.DMBTDemo;
using Lukso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public partial class Skeleton
{
    public const int JOINT_COUNT = 33;
    public class Bone
    {
        public readonly int fromIdx;
        public readonly int toIdx;

        public Bone(int fromIdx, int toIdx) {
            this.fromIdx = fromIdx;
            this.toIdx = toIdx;
        }
    }


    public string Name { get; private set; }
    public List<JointDefinition> joints = new List<JointDefinition>();
    private int[] keyPointsIds;

    public List<ClothAttachementDefinition> clothPoints = new List<ClothAttachementDefinition>();

    public List<Bone> ScaleBones;
    private Dictionary<Point, string> boneNameByPoint = new Dictionary<Point, string>();


    public Skeleton(string name) {
        Name = name;
    }

    public JointDefinition GetByName(string name) {
        // used in initialization. Performance is not the matter
        return joints.Where(x => Utils.CompareNodeByName(name, x.name)).FirstOrDefault();
    }

    public JointDefinition GetByPoint(string name) {
        // used in initialization. Performance is not the matter
        return joints.Where(x => x.point.ToString().Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    }


    internal static bool CompareNodeByNames(string objName, string rexExp) {
        //objName = Utils.ReplaceSpace(objName.ToLower());
        objName = objName.ToLower();
        return Regex.Match(objName, rexExp).Success;
    }

    internal bool Init(GameObject obj, int[,] scaleBones, SkeletonSet.Skeleton skeletonDescrs) {
        var children = obj.GetComponentsInChildren<Transform>();

        List<int> ids = new List<int>();
        
        
        foreach(var skelPoint in skeletonDescrs.description.Where(x => x.node.Length > 0)) {
            var type = (Point)Enum.Parse(typeof(Point), skelPoint.type, true);
            if (type == Point.DUMMY) {
                continue;
            }


            var j = GetByPoint(skelPoint.type);
            if (j == null) {
                Debug.LogError("Cant find joint by type specified in skeleton descriptor:" + skelPoint.type);
                return false;
            }

            var boneObject = Array.Find(children, c => CompareNodeByNames(c.gameObject.name, skelPoint.node))?.gameObject;
            if (boneObject != null) {
                boneNameByPoint[type] = boneObject.name;
                j.name = boneObject.name;
            }

            if (j.pointId >= 0) {
                if (boneObject == null) {
                    Debug.LogError("Cant find node:" + skelPoint.node);
                    return false;
                }
              
              //  this.jointBones[j.pointId] = Array.Find(children, c => Utils.CompareNodeByName(c.gameObject.name, j.name))?.gameObject;
                ids.Add(j.pointId);
            }
        }
 
        ids.Sort();
        this.keyPointsIds = ids.ToArray();

        ScaleBones = new List<Bone>();
        for (var i = 0; i < scaleBones.GetLength(0); ++i) {
            int idx1 = scaleBones[i, 0];
            int idx2 = scaleBones[i, 1];
            ScaleBones.Add(new Bone(idx1, idx2));
        }

        return true;
    }

    public string GetBoneName(Point point) {
        try {
            return boneNameByPoint[point];
        } catch (Exception e) {
            return boneNameByPoint[point];
        }
    }

    // returns only points which corresponds to joint bone
    internal Vector3?[] FilterKeyPoints(Vector3?[] target) {
        return keyPointsIds.Where(id => id < target.Length).Select(id => target[id]).ToArray();
    }
    
    internal int[] GetkeyPointIds() {
        return keyPointsIds;
    }

}
