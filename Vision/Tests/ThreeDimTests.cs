using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Tests
{
    public class ThreeDimTests
    {
        VMat rod = VMat.New();
        List<Point3D> objpoint = new List<Point3D>()
        {
            new Point3D(0,0,0),
            new Point3D(100,0,0),
            new Point3D(0,0,0),
            new Point3D(0,100,0),
            new Point3D(0,0,0),
            new Point3D(0,0,100),
            new Point3D(0,20,0),
            new Point3D(20,20,0),
            new Point3D(20,0,0),
            new Point3D(20,20,0),
            new Point3D(0,20,0),
            new Point3D(0,20,20),
            new Point3D(0,0,20),
            new Point3D(0,20,20),
            new Point3D(0,0,20),
            new Point3D(20,0,20),
            new Point3D(20,0,0),
            new Point3D(20,0,20),
            new Point3D(20,20,0),
            new Point3D(20,20,20),
            new Point3D(0,20,20),
            new Point3D(20,20,20),
            new Point3D(20,0,20),
            new Point3D(20,20,20),
        };
        List<Scalar> objcolor = new List<Scalar>()
        {
            Scalar.BgrRed,
            Scalar.BgrGreen,
            Scalar.BgrBlue,
        };
        List<double> rvec = new List<double>() { -3.1, 0.1, -0.1 };
        List<double> tvec = new List<double>() { -9, 3, 266 };
        double[,] rodriues = null;
        double[] dist_coeffs = new double[4];
        double rvec_step = 0.02;
        double tvec_step = 1;
        bool updatetheta = true;
        bool invertROD = false;

        RotationMatrixTransform transform;
        VMat input;
        VMat buffer;
        Point center;
        double theta;
        double[] vec;
        double[,] camera_matrix_buffer;
        float focal_length;

        public ThreeDimTests()
        {
            buffer = VMat.New(new Size(500, 500), MatType.CV_8UC3);

            center = new Point(buffer.Cols / 2, buffer.Rows / 2);
            focal_length = buffer.Cols;
            camera_matrix_buffer = new double[,]
            {
                { focal_length, 0, center.X },
                { 0, focal_length, center.Y },
                { 0, 0, 1 }
            };

            input = VMat.New(new Size(500, 500), MatType.CV_8UC3);
            for(int i=0; i<10; i++)
            {
                double margin = i * (input.Width / 20);
                input.DrawRectangle(new Rect(margin, margin, input.Width-2*margin, input.Height-2*margin), Scalar.Random(), -1);
            }

            transform = new RotationMatrixTransform();
        }

        public void Run()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            long lastMs = 0;
            bool on = true;
            while (on)
            {
                Profiler.Count("FPS");

                RenderProc();

                int sleep = (int)Math.Max(1, (1000.0/ 30.0) - sw.ElapsedMilliseconds + lastMs);
                lastMs = sw.ElapsedMilliseconds;

                char c = Core.Cv.WaitKey(sleep);

                updatetheta = false;
                switch (c)
                {
                    case 'z':
                        on = false;
                        break;
                    case 'i':
                        rvec[0] += rvec_step;
                        break;
                    case 'k':
                        rvec[0] -= rvec_step;
                        break;
                    case 'j':
                        rvec[1] -= rvec_step;
                        break;
                    case 'l':
                        rvec[1] += rvec_step;
                        break;
                    case 'u':
                        rvec[2] -= rvec_step;
                        break;
                    case 'o':
                        rvec[2] += rvec_step;
                        break;
                    case 's':
                        tvec[1] += tvec_step;
                        break;
                    case 'w':
                        tvec[1] -= tvec_step;
                        break;
                    case 'a':
                        tvec[0] -= tvec_step;
                        break;
                    case 'd':
                        tvec[0] += tvec_step;
                        break;
                    case 'q':
                        tvec[2] -= tvec_step;
                        break;
                    case 'e':
                        tvec[2] += tvec_step;
                        break;
                    case '1':
                        Logger.Log($"th:{theta}  vec:{Logger.Print(vec)}");
                        Core.Cv.Rodrigues(rvec.ToArray(), out rodriues);
                        Logger.Log(Logger.Print(rodriues, 3, 3));
                        break;
                    case '2':
                        Logger.Log($"th:{theta}  vec:{Logger.Print(vec)}");
                        tvec[0] = -tvec[0];
                        tvec[1] = -tvec[1];
                        tvec[2] = -tvec[2];
                        updatetheta = true;
                        break;
                    case '3':
                        theta -= 0.1;
                        updatetheta = true;
                        break;
                    case '4':
                        theta += 0.1;
                        updatetheta = true;
                        break;
                    case '5':
                        theta -= Math.PI / 4;
                        updatetheta = true;
                        break;
                    case '6':
                        theta += Math.PI / 4;
                        updatetheta = true;
                        break;
                    case '7':
                        vec[0] = -vec[0];
                        vec[1] = -vec[1];
                        vec[2] = -vec[2];
                        updatetheta = true;
                        break;
                    case '8':
                        // TODO: FileDialog
                        if (rodriues != null)
                            Logger.Log(Logger.Print(rodriues, 3, 3));
                        if (!rod.IsEmpty)
                            Logger.Log(rod.Print());
                        break;
                    case '9':
                        invertROD = !invertROD;
                        break;
                    case '+':
                        buffer.Resize(new Size(buffer.Width + 10, buffer.Height + 10));
                        focal_length = buffer.Cols;
                        center = new Point(buffer.Cols / 2, buffer.Rows / 2);
                        camera_matrix_buffer = new double[,]
                        {
                            { focal_length, 0, center.X },
                            { 0, focal_length, center.Y },
                            { 0, 0, 1 }
                        };
                        break;
                    case '-':
                        buffer.Resize(new Size(buffer.Width - 10, buffer.Height - 10));
                        focal_length = buffer.Cols;
                        center = new Point(buffer.Cols / 2, buffer.Rows / 2);
                        camera_matrix_buffer = new double[,]
                        {
                            { focal_length, 0, center.X },
                            { 0, focal_length, center.Y },
                            { 0, 0, 1 }
                        };
                        break;
                    case 'r':
                        rvec[0] = 0;
                        rvec[1] = 0;
                        rvec[2] = 0;
                        tvec[0] = 0;
                        tvec[1] = 0;
                        tvec[2] = 250;
                        break;
                }

                if (updatetheta)
                {
                    theta %= Math.PI * 2;
                    rvec[0] = vec[0] * theta;
                    rvec[1] = vec[1] * theta;
                    rvec[2] = vec[2] * theta;
                }
            }
            sw.Stop();
            Core.Cv.CloseAllWindows();
        }

        private void RenderProc()
        {
            //clear buffer
            buffer.DrawRectangle(new Rect(0, 0, buffer.Width, buffer.Height), Scalar.BgrWhite, -1);

            //project points
            Point[] img2dPoints;
            double[,] jacobian;
            Core.Cv.ProjectPoints(objpoint, rvec.ToArray(), tvec.ToArray(), camera_matrix_buffer, dist_coeffs, out img2dPoints, out jacobian);

            //render obj points
            for (int i = 0; i < img2dPoints.Length / 2; i++)
            {
                Scalar s = Scalar.BgrBlack;
                if (i < objcolor.Count)
                    s = objcolor[i];
                Core.Cv.DrawLine(buffer, img2dPoints[i * 2], img2dPoints[i * 2 + 1], s, 2, LineType.AntiAlias);
            }

            //calc rotation vec
            theta = Math.Sqrt(rvec[0] * rvec[0] + rvec[1] * rvec[1] + rvec[2] * rvec[2]);
            vec = new double[] { rvec[0] / theta, rvec[1] / theta, rvec[2] / theta };

            string fmt = "0.0000";
            Core.Cv.DrawText(buffer, $"rvec:({rvec[0].ToString(fmt)},{rvec[1].ToString(fmt)},{rvec[2].ToString(fmt)}) | tvec:({tvec[0].ToString(fmt)},{tvec[1].ToString(fmt)},{tvec[2].ToString(fmt)})", new Point(0, 20), FontFace.HersheyPlain, 1, Scalar.BgrGreen, 1, LineType.AntiAlias);
            Core.Cv.DrawText(buffer, $"vec:({vec[0].ToString(fmt)},{vec[1].ToString(fmt)},{vec[2].ToString(fmt)}) | theta:({theta.ToString(fmt)})", new Point(0, 35), FontFace.HersheyPlain, 1, Scalar.BgrGreen, 1, LineType.AntiAlias);
            Core.Cv.ImgShow("window", buffer);

            //transform image
            if (!input.IsEmpty)
            {
                Profiler.Start("Transform");

                Core.Cv.Rodrigues(rvec.ToArray(), out rodriues);
                if (rod != null)
                    rod.Dispose();
                rod = VMat.New(new Size(3, 3), MatType.CV_64FC1, rodriues);
                if (invertROD)
                    rod = rod.Inv();
                using (VMat output = VMat.New())
                {
                    transform.Transform(input, output, rod);

                    Core.Cv.ImgShow("transform", output);
                }

                Profiler.End("Transform");
            }
        }
    }
}
