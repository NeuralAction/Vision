using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Vision.Cv
{
    public class RotationMatrixTransform
    {
        public InterpolationFlags Inter { get; set; } = InterpolationFlags.Cubic;

        public RotationMatrixTransform()
        {

        }

        public void Transform(Mat input, Mat output, double[] rvec, Size outputSize = null)
        {
            double[,] rodrigues;
            Core.Cv.Rodrigues(rvec, out rodrigues);
            using (Mat rod = MatTool.New(new Size(3, 3), MatType.CV_64FC1, rodrigues))
            {
                Transform(input, output, rod, outputSize);
            }
        }

        public void Transform(Mat input, Mat output, Mat rod, Size outputSize = null)
        {
            if(outputSize == null)
            {
                outputSize = input.SizeProperty.ToSize();
            }

            double w = input.Cols;
            double h = input.Rows;

            // Projection 2D -> 3D matrix
            Mat A1 = MatTool.New(new Size(4, 3), MatType.CV_64FC1, new double[4, 3]
            {
                { 1, 0, -w / 2 },
                { 0, 1, -h / 2 },
                { 0, 0, 0 },
                { 0, 0, 1 }
            });

            Mat R = MatTool.New(new Size(4, 4), MatType.CV_64FC1, new double[4, 4]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });
            rod.CopyTo(MatTool.New(R, new Rect(0, 0, 3, 3)));

            // Translation matrix
            Mat T = MatTool.New(new Size(4, 4), MatType.CV_64FC1, new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, input.Cols },
                { 0, 0, 0, 1 }
            });

            // 3D -> 2D matrix
            Mat A2 = MatTool.New(new Size(3, 4), MatType.CV_64FC1, new double[,]
            {
                { input.Cols, 0, w / 2, 0 },
                { 0, input.Cols, h / 2, 0 },
                { 0, 0, 1, 0 }
            });

            // Final transformation matrix
            Mat trans = A2 * (T * (R * A1));

            // Apply matrix transformation
            Core.Cv.WarpPerspective(input, output, trans, outputSize, Inter);
        }
    }
}
