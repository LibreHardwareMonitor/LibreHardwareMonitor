// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Conrec.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Creates contours from a triangular mesh.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides functionality to create contours from a triangular mesh.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ported from C / Fortran code by Paul Bourke.
    /// See <a href="http://paulbourke.net/papers/conrec/">Conrec</a> for
    /// full description of code and the original source.
    ///  </para>
    /// <para>
    /// Contouring aids in visualizing three dimensional surfaces on a two dimensional
    /// medium (on paper or in this case a computer graphics screen). Two most common
    /// applications are displaying topological features of an area on a map or the air
    /// pressure on a weather map. In all cases some parameter is plotted as a function
    /// of two variables, the longitude and latitude or x and y axis. One problem with
    /// computer contouring is the process is usually CPU intensive and the algorithms
    /// often use advanced mathematical techniques making them susceptible to error.
    ///  </para>
    /// </remarks>
    public static class Conrec
    {
        /// <summary>
        /// Renderer delegate
        /// </summary>
        /// <param name="x1">
        /// Start point x-coordinate
        /// </param>
        /// <param name="y1">
        /// Start point y-coordinate
        /// </param>
        /// <param name="x2">
        /// End point x-coordinate
        /// </param>
        /// <param name="y2">
        /// End point y-coordinate
        /// </param>
        /// <param name="z">
        /// Contour level
        /// </param>
        public delegate void RendererDelegate(double x1, double y1, double x2, double y2, double z);

        /// <summary>
        /// Contour is a contouring subroutine for rectangularily spaced data
        /// It emits calls to a line drawing subroutine supplied by the user
        /// which draws a contour map corresponding to data on a randomly
        /// spaced rectangular grid. The coordinates emitted are in the same
        /// units given in the x() and y() arrays.
        /// Any number of contour levels may be specified but they must be
        /// in order of increasing value.
        /// </summary>
        /// <param name="d">
        /// Matrix of data to contour.
        /// </param>
        /// <param name="x">
        /// Data matrix column coordinates.
        /// </param>
        /// <param name="y">
        /// Data matrix row coordinates.
        /// </param>
        /// <param name="z">
        /// Contour levels in increasing order.
        /// </param>
        /// <param name="renderer">
        /// The renderer.
        /// </param>
        public static void Contour(double[,] d, double[] x, double[] y, double[] z, RendererDelegate renderer)
        {
            double x1 = 0.0;
            double x2 = 0.0;
            double y1 = 0.0;
            double y2 = 0.0;

            var h = new double[5];
            var sh = new int[5];
            var xh = new double[5];
            var yh = new double[5];

            int ilb = d.GetLowerBound(0);
            int iub = d.GetUpperBound(0);
            int jlb = d.GetLowerBound(1);
            int jub = d.GetUpperBound(1);
            int nc = z.Length;

            // The indexing of im and jm should be noted as it has to start from zero
            // unlike the fortran counter part
            int[] im = { 0, 1, 1, 0 };
            int[] jm = { 0, 0, 1, 1 };

            // Note that castab is arranged differently from the FORTRAN code because
            // Fortran and C/C++ arrays are transposed of each other, in this case
            // it is more tricky as castab is in 3 dimension
            int[,,] castab =
                            {
                                 { { 0, 0, 8 }, { 0, 2, 5 }, { 7, 6, 9 } }, { { 0, 3, 4 }, { 1, 3, 1 }, { 4, 3, 0 } },
                                 { { 9, 6, 7 }, { 5, 2, 0 }, { 8, 0, 0 } }
                            };

            Func<int, int, double> xsect = (p1, p2) => ((h[p2] * xh[p1]) - (h[p1] * xh[p2])) / (h[p2] - h[p1]);
            Func<int, int, double> ysect = (p1, p2) => ((h[p2] * yh[p1]) - (h[p1] * yh[p2])) / (h[p2] - h[p1]);

            for (int j = jub - 1; j >= jlb; j--)
            {
                int i;
                for (i = ilb; i <= iub - 1; i++)
                {
                    double temp1 = Math.Min(d[i, j], d[i, j + 1]);
                    double temp2 = Math.Min(d[i + 1, j], d[i + 1, j + 1]);
                    double dmin = Math.Min(temp1, temp2);
                    temp1 = Math.Max(d[i, j], d[i, j + 1]);
                    temp2 = Math.Max(d[i + 1, j], d[i + 1, j + 1]);
                    double dmax = Math.Max(temp1, temp2);

                    if (dmax >= z[0] && dmin <= z[nc - 1])
                    {
                        int k;
                        for (k = 0; k < nc; k++)
                        {
                            if (z[k] >= dmin && z[k] <= dmax)
                            {
                                int m;
                                for (m = 4; m >= 0; m--)
                                {
                                    if (m > 0)
                                    {
                                        // The indexing of im and jm should be noted as it has to
                                        // start from zero
                                        h[m] = d[i + im[m - 1], j + jm[m - 1]] - z[k];
                                        xh[m] = x[i + im[m - 1]];
                                        yh[m] = y[j + jm[m - 1]];
                                    }
                                    else
                                    {
                                        h[0] = 0.25 * (h[1] + h[2] + h[3] + h[4]);
                                        xh[0] = 0.5 * (x[i] + x[i + 1]);
                                        yh[0] = 0.5 * (y[j] + y[j + 1]);
                                    }

                                    if (h[m] > 0.0)
                                    {
                                        sh[m] = 1;
                                    }
                                    else if (h[m] < 0.0)
                                    {
                                        sh[m] = -1;
                                    }
                                    else
                                    {
                                        sh[m] = 0;
                                    }
                                }

                                //// Note: at this stage the relative heights of the corners and the
                                //// centre are in the h array, and the corresponding coordinates are
                                //// in the xh and yh arrays. The centre of the box is indexed by 0
                                //// and the 4 corners by 1 to 4 as shown below.
                                //// Each triangle is then indexed by the parameter m, and the 3
                                //// vertices of each triangle are indexed by parameters m1,m2,and
                                //// m3.
                                //// It is assumed that the centre of the box is always vertex 2
                                //// though this isimportant only when all 3 vertices lie exactly on
                                //// the same contour level, in which case only the side of the box
                                //// is drawn.
                                //// vertex 4 +-------------------+ vertex 3
                                //// | \               / |
                                //// |   \    m-3    /   |
                                //// |     \       /     |
                                //// |       \   /       |
                                //// |  m=2    X   m=2   |       the centre is vertex 0
                                //// |       /   \       |
                                //// |     /       \     |
                                //// |   /    m=1    \   |
                                //// | /               \ |
                                //// vertex 1 +-------------------+ vertex 2

                                // Scan each triangle in the box
                                for (m = 1; m <= 4; m++)
                                {
                                    int m1 = m;
                                    int m2 = 0;
                                    int m3;
                                    if (m != 4)
                                    {
                                        m3 = m + 1;
                                    }
                                    else
                                    {
                                        m3 = 1;
                                    }

                                    int caseValue = castab[sh[m1] + 1, sh[m2] + 1, sh[m3] + 1];
                                    if (caseValue != 0)
                                    {
                                        switch (caseValue)
                                        {
                                            case 1: // Line between vertices 1 and 2
                                                x1 = xh[m1];
                                                y1 = yh[m1];
                                                x2 = xh[m2];
                                                y2 = yh[m2];
                                                break;
                                            case 2: // Line between vertices 2 and 3
                                                x1 = xh[m2];
                                                y1 = yh[m2];
                                                x2 = xh[m3];
                                                y2 = yh[m3];
                                                break;
                                            case 3: // Line between vertices 3 and 1
                                                x1 = xh[m3];
                                                y1 = yh[m3];
                                                x2 = xh[m1];
                                                y2 = yh[m1];
                                                break;
                                            case 4: // Line between vertex 1 and side 2-3
                                                x1 = xh[m1];
                                                y1 = yh[m1];
                                                x2 = xsect(m2, m3);
                                                y2 = ysect(m2, m3);
                                                break;
                                            case 5: // Line between vertex 2 and side 3-1
                                                x1 = xh[m2];
                                                y1 = yh[m2];
                                                x2 = xsect(m3, m1);
                                                y2 = ysect(m3, m1);
                                                break;
                                            case 6: // Line between vertex 3 and side 1-2
                                                x1 = xh[m3];
                                                y1 = yh[m3];
                                                x2 = xsect(m1, m2);
                                                y2 = ysect(m1, m2);
                                                break;
                                            case 7: // Line between sides 1-2 and 2-3
                                                x1 = xsect(m1, m2);
                                                y1 = ysect(m1, m2);
                                                x2 = xsect(m2, m3);
                                                y2 = ysect(m2, m3);
                                                break;
                                            case 8: // Line between sides 2-3 and 3-1
                                                x1 = xsect(m2, m3);
                                                y1 = ysect(m2, m3);
                                                x2 = xsect(m3, m1);
                                                y2 = ysect(m3, m1);
                                                break;
                                            case 9: // Line between sides 3-1 and 1-2
                                                x1 = xsect(m3, m1);
                                                y1 = ysect(m3, m1);
                                                x2 = xsect(m1, m2);
                                                y2 = ysect(m1, m2);
                                                break;
                                        }

                                        renderer(x1, y1, x2, y2, z[k]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}