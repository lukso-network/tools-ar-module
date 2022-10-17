namespace Assets.PoseEstimator {

    public interface IFilter {
        public object Filter(object t);
    }

    public abstract class Filter<T> : IFilter {
        protected T prevValue;
        protected T prevInputValue;
        private bool isFirst = true;
        public T filter(T v) {
            if (isFirst) {
                isFirst = false;
                prevValue = v;
                prevInputValue = v;
                return prevValue;
            }

            return filterInternal(v);
        }


        protected abstract T filterInternal(T v);

        object IFilter.Filter(object t) {
            return filter((T)t);
        }
    }
}