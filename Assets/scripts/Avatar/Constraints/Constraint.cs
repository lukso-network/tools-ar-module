using System;
using Joint = Assets.Joint;

[Serializable]
public class Constraint
{
    internal virtual void Fix(Joint joint) {
    }
    internal virtual void KeepPrevState(Joint joint) {
    }
}
