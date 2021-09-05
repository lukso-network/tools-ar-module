namespace DeepMotion.DMBTDemo
{
    using UnityEngine;

    //using Newtonsoft.Json.Linq;
    [System.Serializable]
    public class SkeletonSet
    {
        public Skeleton[] skeletons;



        [System.Serializable]
        public class Skeleton
        {
            [System.Serializable]
            public class SkeletonJointDescriptor
            {
                public string type;
                public int id;
                public string node;

            }

            public string name;
            public SkeletonJointDescriptor[] description;

        }

        public static SkeletonSet CreateFromJSON(string jsonString) {
            return JsonUtility.FromJson<SkeletonSet>(jsonString);
        }

    }
}