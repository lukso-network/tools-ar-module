using Kalman;
using LinearAlgebra;
using System;

namespace Assets.PoseEstimator
{

    [Serializable]
    public class FilterSettings
    {
        public double sigma;
        public double dt;
        public FilterType filterType = FilterType.XV;

        public FilterSettings() {
            ResetModified();
        }

        public FilterSettings(double sigma, double dt, FilterType filterType) {
            this.sigma = sigma;
            this.dt = dt;
            this.filterType = filterType;
            ResetModified();
        }

        public bool IsModified { get; private set; }

        public void SetModified() {
            IsModified = true;
        }

        public void ResetModified() {
            IsModified = false;
        }
        
    }

    class KalmanPointFilter : Filter<float> {

        private KalmanFilter kalman;
        private FilterType type;
        private FilterSettings settings;

        public KalmanPointFilter(FilterSettings settings) {
            this.settings = settings;
            CreateFilter();
        }

        private void CreateFilter() {
            var type = settings.filterType;
            var dt = settings.dt;
            var sigma = settings.sigma;
            settings.ResetModified();

            if (type == FilterType.XVA) {
                var f = new Matrix(new[,] { { 1, dt, dt * dt / 2 }, { 0, 1, dt }, { 0, 0, 1 } });
                var b = new Matrix(new[,] { { 0.0 }, { 0 }, { 0 } });
                var u = new Matrix(new[,] { { 0.0 }, { 0 }, { 0 } });
                var r = Matrix.CreateVector(sigma * sigma);
                var q = new Matrix(new[,] {{.25 * dt*dt*dt*dt, .5 * dt * dt*dt, .5 * dt * dt },
                                        { .5 * dt * dt*dt,    dt * dt,       dt },
                                        { .5 * dt * dt,       dt,        1 } });

                var h = new Matrix(new[,] { { 1.0, 0, 0 } });

                kalman = new KalmanFilter(f, b, u, q, h, r); // задаем F, H, Q и R
                kalman.SetState(Matrix.CreateVector(0, 0, 0), new Matrix(new[,] { { 10.0, 0, 0 }, { 0, 10.0, 0 }, { 0, 0, 10 } })); // задаем начальные State и Covariance
            } else {
                var f = new Matrix(new[,] { { 1, dt }, { 0, 1 } });
                var b = new Matrix(new[,] { { 0.0 }, { 0 } });
                var u = new Matrix(new[,] { { 0.0 }, { 0 } });
                var r = Matrix.CreateVector(sigma * sigma);
                var q = new Matrix(new[,] {{.25 * dt*dt*dt*dt, .5 * dt * dt*dt },
                                     { .5 * dt * dt*dt,    dt * dt }});

                var h = new Matrix(new[,] { { 1.0, 0 } });

                kalman = new KalmanFilter(f, b, u, q, h, r); // задаем F, H, Q и R
                kalman.SetState(Matrix.CreateVector(0, 0), new Matrix(new[,] { { 10.0, 0 }, { 0, 10.0 } })); // задаем начальные State и Covariance
            }

        }

        public KalmanPointFilter(double sigma, double dt, FilterType filterType) :
            this(new FilterSettings(sigma, dt, filterType)) { 

        }

        public float Update(float v) {
            if (settings.IsModified) {
                CreateFilter();
            }

            if (type == FilterType.XVA) {
                kalman.Correct(new Matrix(new[,] { { (double)v } }));
            } else {
                // optimized version
                kalman.Correct(v);
            }
            return (float)(kalman.State[0,0]);
        }

        protected override float filterInternal(float v) {
            return Update(v);
        }

        public void SetFilterModified() {
            CreateFilter();
        }
    }
}
