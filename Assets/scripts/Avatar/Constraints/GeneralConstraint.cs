using Joint = Lukso.Joint;

namespace Lukso {
    public class GeneralConstraint {
        public GeneralConstraint(params Constraint[] constraints) {

        }
        internal virtual void Fix(Joint joint) {
        }
        internal virtual void KeepPrevState(Joint joint) {
        }
    }
}