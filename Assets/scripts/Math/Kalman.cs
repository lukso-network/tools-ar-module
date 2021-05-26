using LinearAlgebra;

namespace Kalman
{
    public sealed class KalmanFilter
    {
        //System matrices
        public Matrix X0 { get; private set; } 
        public Matrix P0 { get; private set; }

        public Matrix F { get; private set; }
        public Matrix B { get; private set; }
        public Matrix U { get; private set; }
        public Matrix Q { get; private set; }
        public Matrix H { get; private set; }
        public Matrix R { get; private set; }

        public Matrix F_tr { get; private set; }
        public Matrix H_tr { get; private set; }
        public Matrix Ident { get; private set; }
        private Matrix k;
        private Matrix ikh;

        public Matrix State { get; private set; } 
        public Matrix Covariance { get; private set; }

        public KalmanFilter(Matrix f, Matrix b, Matrix u, Matrix q, Matrix h, Matrix r)
        {
            F = f;
            B = b;
            U = u;
            Q = q;
            H = h;
            R = r;

            F_tr = F.Transpose();
            H_tr = H.Transpose();
            Ident = Matrix.Identity(F.RowCount);

            k = new Matrix(new double[F.RowCount, 1]);
            ikh = new Matrix(new double[F.RowCount, F.RowCount]);
            X0 = new Matrix(new double[F.RowCount, 1]);
        }
       
        public void SetState(Matrix state, Matrix covariance)
        {
            // Set initial state
            State = state;
            Covariance = covariance;
        }

        public void Correct(Matrix z)
        {
            // Predict
            X0 = F * State;// +(B * U);
            P0 = F * Covariance * F_tr + Q;

            // Correct
            var k = P0 * H_tr * (H * P0 * H_tr + R).Inverse(); // kalman gain

            State = X0 + (k * (z - (H * X0)));
            Covariance = (Ident - k * H) * P0;
        }


        public void Correct2(double z) {
            // Predict
            X0 = F * State;// +(B * U);
            P0 = F * Covariance * F_tr + Q;

            // Correct
            //var k = P0 * H_tr * (H * P0 * H_tr + R).Inverse(); // kalman gain



            var t = 1 / (P0[0, 0] + R[0, 0]);
            k[0, 0] = P0[0, 0] * t;
            k[1, 0] = P0[1, 0] * t;
            k[2, 0] = P0[2, 0] * t;

            // t = (z - X0[0, 0]) / (P0[0, 0] + R[0, 0]);
            t = (z - X0[0, 0]);
            State[0, 0] = X0[0, 0] + k[0, 0] * t;
            State[1, 0] = X0[1, 0] + k[1, 0] * t;
            State[2, 0] = X0[2, 0] + k[2, 0] * t;

            // var k = P0 * H_tr * (H * P0 * H_tr + R).Inverse();
            //var s2 = X0 + (k * (new Matrix(new double[,] { { z } }) - (H * X0)));

            //  State = X0 + (k * (z - (H * X0)));

            Covariance = (Ident - k * H) * P0;
        }

        public void Correct(double z) {
            // Predict
            X0[0, 0] = State[0,0] + F[0, 1] * State[1, 0];// = F * State;// +(B * U);
            X0[1, 0] = State[1,0];// = F * State;// +(B * U);
            P0 = F * Covariance * F_tr + Q;

            // Correct
            //var k = P0 * H_tr * (H * P0 * H_tr + R).Inverse(); // kalman gain



            var t = 1 / (P0[0, 0] + R[0, 0]);
            k[0, 0] = P0[0, 0] * t;
            k[1, 0] = P0[1, 0] * t;


            // t = (z - X0[0, 0]) / (P0[0, 0] + R[0, 0]);
            t = (z - X0[0, 0]);
            State[0, 0] = X0[0, 0] + k[0, 0] * t;
            State[1, 0] = X0[1, 0] + k[1, 0] * t;


            // var k = P0 * H_tr * (H * P0 * H_tr + R).Inverse();
            //var s2 = X0 + (k * (new Matrix(new double[,] { { z } }) - (H * X0)));

            //  State = X0 + (k * (z - (H * X0)));
           // Covariance = (Ident - k * H) * P0;


            ikh[0, 0] = 1 - k[0, 0];
            ikh[1, 0] = -k[1, 0];
            ikh[1, 1] = 1;

            Covariance = ikh * P0;
        }
    }
}