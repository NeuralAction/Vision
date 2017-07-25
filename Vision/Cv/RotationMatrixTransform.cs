using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public class RotationMatrixTransform
    {
        public Interpolation Inter { get; set; } = Interpolation.Cubic;

        public RotationMatrixTransform()
        {

        }

        public void Transform(VMat input, VMat output, double[] rvec, Size outputSize = null)
        {
            double[,] rodrigues;
            Core.Cv.Rodrigues(rvec, out rodrigues);
            using (VMat rod = VMat.New(new Size(3, 3), MatType.CV_64FC1, rodrigues))
            {
                Transform(input, output, rod, outputSize);
            }
        }

        public void Transform(VMat input, VMat output, VMat rod, Size outputSize = null)
        {
            if(outputSize == null)
            {
                outputSize = input.Size;
            }

            double w = input.Cols;
            double h = input.Rows;

            // Projection 2D -> 3D matrix
            VMat A1 = VMat.New(new Size(4, 3), MatType.CV_64FC1, new double[4, 3]
            {
                { 1, 0, -w / 2 },
                { 0, 1, -h / 2 },
                { 0, 0, 0 },
                { 0, 0, 1 }
            });

            VMat R = VMat.New(new Size(4, 4), MatType.CV_64FC1, new double[4, 4]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });
            rod.CopyTo(VMat.New(R, new Rect(0, 0, 3, 3)));

            // Translation matrix
            VMat T = VMat.New(new Size(4, 4), MatType.CV_64FC1, new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, input.Cols },
                { 0, 0, 0, 1 }
            });

            // 3D -> 2D matrix
            VMat A2 = VMat.New(new Size(3, 4), MatType.CV_64FC1, new double[,]
            {
                { input.Cols, 0, w / 2, 0 },
                { 0, input.Cols, h / 2, 0 },
                { 0, 0, 1, 0 }
            });

            // Final transformation matrix
            VMat trans = A2 * (T * (R * A1));

            // Apply matrix transformation
            Core.Cv.WarpPerspective(input, output, trans, outputSize, Inter);
        }
    }
}
