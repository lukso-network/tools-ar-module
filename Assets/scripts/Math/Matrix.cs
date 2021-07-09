using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using LinearAlgebra.MatrixAlgebra;
namespace LinearAlgebra
{
    public sealed class Matrix : IEnumerable<double>, IEquatable<Matrix>, IFormattable
    {
        double[,] elements;
        const double eps = 1e-9;
        const int DOUBLE_SIZE = sizeof(double);
        private MatrixSubset SubMat;
        /// <summary>
        /// 利用二维double数组初始化矩阵
        /// </summary>
        /// <param name="Elements"></param>
        public Matrix(double[,] Elements)
        {
            Initialize(Elements);
        }

        public static Matrix Identity(int size) {
            var m = new Matrix(new double[size, size]);
            for (int i = 0; i < size; ++i) {
                m[i, i] = 1;
            }
            return m;
        }

        public static Matrix CreateVector(params double[] values) {
            var matrix = new double[values.Length, 1];
            for (int i = 0; i < values.Length; i++) matrix[i, 0] = values[i];

            return new Matrix(matrix);
        }
        public Matrix(Matrix M)
        {
            Initialize(Utility.CopyMatrix(M.elements));
        }
        private void Initialize(double[,] Elements)
        {
            elements = Elements;
            SubMat = new MatrixSubset(elements);
        }
        /// <summary>
        /// 获取矩阵的行数
        /// </summary>
        public int RowCount
        {
            get { return elements.GetLength(0); }
        }
        /// <summary>
        /// 获取矩阵的列数
        /// </summary>
        public int ColumnCount
        {
            get { return elements.GetLength(1); }
        }
        /// <summary>
        /// 获取矩阵的元素数量
        /// </summary>
        public int Count
        {
            get { return elements.Length; }
        }
        public double this[int row, int col]
        {
            get { return elements[row, col]; }
            set { elements[row, col] = value; }
        }
        /// <summary>
        /// 返回矩阵元素
        /// </summary>
        /// <returns></returns>
        public double[,] Elements
        {
            get { return elements; }
            set { elements = value; }
        }
        /// <summary>
        /// 初始化矩阵
        /// </summary>
        /// <param name="nRows">行数</param>
        /// <param name="nCols">列数</param>
        /// <param name="Elements">矩阵元素</param>
        /// <returns></returns>
        public static Matrix Create(int nRows, int nCols, double[] Elements)
        {
            return Utility.ArrayToMatrix(nRows, nCols, Elements);
        }
        /// <summary>
        /// 初始化矩阵
        /// </summary>
        /// <param name="nRows">行数</param>
        /// <param name="nCols">列数</param>
        /// <param name="Elements">矩阵元素</param>
        /// <returns></returns>
        public static Matrix Create(int nRows, int nCols, IEnumerable<double> Elements)
        {
            return Utility.IEnumerableToMatrix(nRows, nCols, Elements);
        }
        public static implicit operator Matrix(double[,] Elements)
        {
            return new Matrix(Elements);
        }
        #region Overload operators 
        public static Matrix operator +(Matrix M)
        {
            return new Matrix(M);
        }
        public static Matrix operator -(Matrix M)
        {
            return MatrixComputation.UnaryMinus(M.elements);
        }
        public static Matrix operator +(Matrix A, Matrix B)
        {
            return MatrixComputation.Add(A.elements, B.elements);
        }
        public static Matrix operator -(Matrix A, Matrix B)
        {
            return MatrixComputation.Subtract(A.elements, B.elements);
        }
        public static Matrix operator *(Matrix A, Matrix B)
        {
            return MatrixComputation.Multiply(A.elements, B.elements);
        }
        public static Matrix operator /(Matrix A, Matrix B)
        {
            return MatrixComputation.Divide(A.elements, B.elements);
        }
     
        #endregion
        /// <summary>
        /// 全选主元高斯-约当法求逆矩阵
        /// </summary>
        /// <returns>逆矩阵</returns>
        public Matrix Inverse()
        {
            return MatrixComputation.Inverse(elements);
        }
        /// <summary>
        /// 矩阵的转置
        /// </summary>
        /// <returns>返回转置后的矩阵</returns>
        public Matrix Transpose()
        {
            return MatrixComputation.Transpose(elements);
        }
        /// <summary>
        /// 矩阵的幂
   
        public static Matrix rBind(params Matrix[] Matrices)
        {
            return Utility.rBind(Matrices);
        }
        /// <summary>
        /// 横向拼接相同行数的矩阵
        /// </summary>
        /// <param name="Matrices"></param>
        /// <returns></returns>
        public static Matrix cBind(params Matrix[] Matrices)
        {
            return Utility.cBind(Matrices);
        }
        /// <summary>
        /// 根据函数，将原矩阵的值映射到新矩阵上
        /// </summary>
        /// <param name="f">函数</param>
        /// <returns></returns>
        public Matrix Map(Func<double, double> f)
        {
            int m = RowCount;
            int n = ColumnCount;
            int count = Count;
            var res = new double[m, n];
            unsafe
            {
                fixed (double* mat = elements)
                fixed (double* result = res)
                    for (int i = 0; i < count; i++)
                    result[i] = f(mat[i]);
            }
            return res;
        }
        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            foreach (var item in elements)
                yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return this.AsEnumerable();
        }
        public IEnumerable<double> GetRow(int index)
        {
            return SubMat.GetRow(index);
        }
        public IEnumerable<double> GetColumn(int index)
        {
            return SubMat.GetColumn(index);
        }
        public IEnumerable<double> GetDiagonal(bool mainDiagonal = true)
        {
            return SubMat.GetDiagonal(mainDiagonal);
        }
        public void SetRow(int index, IEnumerable<double> data)
        {
            SubMat.SetRow(index, data);
        }
        public void SetColumn(int index, IEnumerable<double> data)
        {
            SubMat.SetColumn(index, data);
        }
        public void SetDiagonal(IEnumerable<double> data, bool mainDiagonal = true)
        {
            SubMat.SetDiagonal(data, mainDiagonal);
        }
        /// <summary>
        /// 矩阵判等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Matrix other)
        {
            int mA = RowCount, nA = ColumnCount;
            int mB = other.RowCount, nB = other.ColumnCount;
            if (mA == mB && nA == nB)
            {
                unsafe
                {
                    fixed (double* a = elements)
                    fixed (double* b = other.elements)
                        for (int i = Count - 1; i >= 0; i--)
                        if (Math.Abs(a[i] - b[i]) > eps)//考虑浮点数的误差
                            return false;
                }
                return true;
            }
            else return false;
        }
        /// <summary>
        /// 将矩阵以字符串的形式输出
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Utility.MatrixToString(elements);
        }
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return Utility.MatrixToString(elements, format, formatProvider);
        }
        public string ToString(string format)
        {
            return Utility.MatrixToString(elements, format, CultureInfo.CurrentCulture);
        }
        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((Matrix)obj);
        }
        // override object.GetHashCode
        public override int GetHashCode()
        {
            double sum = 0.0;
            foreach (var item in elements)
                sum += Math.Abs(item);
            return (int)Math.Sqrt(sum);
        }
        /// <summary>
        /// 从文本文件中加载矩阵
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>

        /// 从文本文件中加载矩阵
        /// </summary>
        /// <pa

        public double[][] ToJaggedArray()
        {
            int nRows = RowCount;
            int nCols = ColumnCount;
            double[][] arr = new double[nRows][];
            for (int i = 0; i < nRows; i++)
            {
                arr[i] = new double[nCols];
                Buffer.BlockCopy(elements, i * nCols * DOUBLE_SIZE, arr[i], 0, nCols * DOUBLE_SIZE);
            }
            return arr;
        }
    }
}
