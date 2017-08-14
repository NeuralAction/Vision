using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPIGazeAnnotaionReader
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var reader = new AnnotationReader(new DirectoryInfo(@"C:\Library\정올 2017\Source\MPIIGaze\Data\Original\"));
            reader.Read();

            NoteData ordered = reader.Datas.OrderByDescending(x => x.OnScreenPoint.X).First();
            Console.WriteLine($"MaxX: {ordered}");

            ordered = reader.Datas.OrderByDescending(x => x.OnScreenPoint.Y).First();
            Console.WriteLine($"MaxY: {ordered}");

            while (true)
            {
                Console.Write(">>> ");
                var cmd = Console.ReadLine();
                switch (cmd)
                {
                    case "show":
                        bool br = false;
                        bool skip = false;
                        bool framebyframe = false;
                        foreach (var d in reader.Datas)
                        {
                            if (br)
                                break;
                            d.ImShow(false);
                            int sleep = 500;
                            if (skip)
                                sleep = 1;
                            if (framebyframe)
                                sleep = 0;
                            char c = (char)Cv2.WaitKey(sleep);
                            switch (c)
                            {
                                case ' ':
                                    framebyframe = !framebyframe;
                                    break;
                                case 's':
                                    skip = !skip;
                                    break;
                                case 'e':
                                    br = true;
                                    break;
                            }
                        }
                        break;
                    case "save":
                        int startInd = -1;
                        try
                        {
                            Console.Write("ind?>>> ");
                            startInd = Convert.ToInt32(Console.ReadLine());
                        }
                        catch
                        {

                        }

                        if (startInd > -1 && startInd < reader.Datas.Count)
                        {
                            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Environment.CurrentDirectory, "save"));
                            DirectoryInfo diRight = new DirectoryInfo(Path.Combine(di.FullName, "right"));
                            DirectoryInfo diLeft = new DirectoryInfo(Path.Combine(di.FullName, "left"));
                            if (!di.Exists)
                                di.Create();
                            else
                                Console.WriteLine("already did");
                            if (!diRight.Exists)
                                diRight.Create();
                            if (!diLeft.Exists)
                                diLeft.Create();

                            List<CascadeClassifier> cascades = new List<CascadeClassifier>();
                            for (int i = 0; i < Environment.ProcessorCount; i++)
                            {
                                cascades.Add(new CascadeClassifier("haarcascade_eye.xml"));
                            }

                            object countLocker = new object();
                            int count = 0;
                            Parallel.For(startInd, reader.Datas.Count, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, (i) =>
                            {
                                CascadeClassifier filter;
                                NoteData d;
                                int id; 
                                lock (countLocker)
                                {
                                    count++;
                                    filter = cascades[count % cascades.Count];
                                    id = count - 1 + startInd;
                                    d = reader.Datas[id];
                                }

                                using (Mat frame = Cv2.ImRead(d.File.FullName))
                                {
                                    var rects = filter.DetectMultiScale(frame, 1.2, 3, HaarDetectionType.ScaleImage, new Size(30, 30), new Size(308, 308));
                                    if (d.GetEye(rects, true) != null && d.GetEye(rects, false) != null)
                                    {
                                        d.Save(diLeft, true, frame, rects);
                                        d.Save(diRight, false, frame, rects);
                                    }
                                }

                                Console.WriteLine($"Extracted({id})[{count}/{reader.Datas.Count - startInd}] {d}");
                            });
                        }
                        break;
                }
            }
        }
    }

    class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3D()
        {

        }

        public Point3D AsUnitVector()
        {
            var l2norm = Math.Sqrt(X * X + Y * Y + Z * Z);

            return new Point3D(X / l2norm, Y / l2norm, Z / l2norm);
        }

        public override string ToString()
        {
            return $"{{{X}, {Y}, {Z}}}";
        }

        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Point3D operator -(Point3D a, Point3D b)
        {
            return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
    }

    class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point()
        {

        }

        public override string ToString()
        {
            return $"{{{X}, {Y}}}";
        }

        public static double EucliudLength(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
    }

    class NoteData
    {
        static CascadeClassifier EyeCascade = new CascadeClassifier("haarcascade_eye.xml");
        static double UnitPerMM = 5;
        static int IDCounter = 0;

        public int ID { get; set; }
        public FileInfo File { get; set; }
        public int ParticipantID { get; set; }
        public int Day { get; set; }
        public Point[] LandmarkPoints { get; set; }
        public Point OnScreenPoint { get; set; }
        public Point3D RelativePoint { get; set; }
        public Point3D Translation { get; set; }
        public Point3D Rotation { get; set; }

        public Point3D NewTranslation { get; set; }
        public Point3D NewRelativePoint { get; set; }
        public Point3D LookVector { get; set; }

        public NoteData(int pid, int day, double[] values, FileInfo file)
        {
            if (values.Length != 35)
                throw new ArgumentOutOfRangeException("values should be 35 values");

            if (!(file ?? throw new ArgumentNullException(nameof(file))).Exists)
                throw new FileNotFoundException(nameof(file));

            ID = IDCounter;
            IDCounter++;

            ParticipantID = pid;
            Day = day;
            File = file;

            List<Point> landmarks = new List<Point>();
            for (int i = 0; i < 12; i++)
            {
                landmarks.Add(new Point(values[i * 2], values[i * 2 + 1]));
            }
            LandmarkPoints = landmarks.ToArray();
            OnScreenPoint = new Point(values[24], values[25]);
            RelativePoint = new Point3D(values[26], values[27], values[28]);
            Rotation = new Point3D(values[29], values[30], values[31]);
            Translation = new Point3D(values[32], values[33], values[34]);
            var m = UnitPerMM;

            //170 unit / 5(unit/mm) = 34 mm
            NewTranslation = new Point3D(Translation.X * m, Translation.Y * m, Translation.Z * m);
            NewRelativePoint = new Point3D(RelativePoint.X * m, RelativePoint.Y * m, RelativePoint.Z * m);

            LookVector = NewRelativePoint - NewTranslation;
            LookVector = LookVector.AsUnitVector();
        }

        public void Save(DirectoryInfo di, bool useleft, Mat frame, Rect[] eyes)
        {
            var img = Path.Combine(di.FullName, $"{ID},{LookVector.X},{LookVector.Y},{LookVector.Z}.jpg");
            if (frame != null && !frame.Empty())
            {
                try
                {
                    var rects = eyes;
                    if (rects != null)
                    {
                        var left = GetEye(rects, useleft);
                        if (left != null)
                        {
                            using (Mat eye = new Mat(frame, (Rect)left))
                            {
                                Cv2.ImWrite(img, eye, new ImageEncodingParam(ImwriteFlags.JpegChromaQuality, 92));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public Rect? GetEye(Rect[] Eyes, bool left)
        {
            List<Point> lefts = new List<Point>();
            List<Point> rights = new List<Point>();
            for (int i = 0; i < 6; i++)
            {
                lefts.Add(LandmarkPoints[i]);
                rights.Add(LandmarkPoints[i + 6]);
            }
            var avgLeft = CalcPointAvg(lefts.ToArray());
            var avgRight = CalcPointAvg(rights.ToArray());

            Point useAvg = null;
            if (left)
                useAvg = avgLeft;
            else
                useAvg = avgRight;

            foreach (var r in Eyes)
            {
                if(HasPoint(r, useAvg))
                {
                    var width = Point.EucliudLength(avgRight, avgLeft) * 0.8;
                    var center = useAvg;

                    return new Rect((int)(center.X - width / 2), (int)(center.Y - width / 2), (int)width, (int)width);
                }
            }

            return null;
        }

        public void ImShow(bool useleft)
        {
            using (Mat img = Cv2.ImRead(File.FullName))
            {
                Rect[] r = EyeCascade.DetectMultiScale(img);
                if (r != null)
                {
                    foreach(Rect eye in r)
                    {
                        img.Rectangle(eye, Scalar.Red, 2);
                    }
                }

                foreach (var pt in LandmarkPoints)
                {
                    Cv2.Circle(img, new OpenCvSharp.Point(pt.X, pt.Y), 2, Scalar.Red, -1);
                }

                var left = GetEye(r, useleft);
                if(left != null)
                {
                    Cv2.Rectangle(img, (Rect)left, Scalar.Lime, 2);
                    using (Mat eyeROI = new Mat(img, (Rect)left))
                    {
                        Cv2.ImShow("eye", eyeROI);
                    }
                }

                Cv2.Circle(img, 300, 300, 4, Scalar.Blue, -1);
                Cv2.Circle(img, (int)RelativePoint.X + 300, (int)RelativePoint.Y + 300, 4, Scalar.Cyan, -1);

                Cv2.PutText(img, $"NoteData[p{ParticipantID}, Day{Day}] Look: {LookVector} NT: {NewTranslation}", new OpenCvSharp.Point(5, 20), HersheyFonts.HersheyPlain, 1, Scalar.White, 1, LineTypes.AntiAlias);
                Cv2.PutText(img, $"NR: {NewRelativePoint} T: {Translation} R: {RelativePoint}", new OpenCvSharp.Point(5, 40), HersheyFonts.HersheyPlain, 1, Scalar.White, 1, LineTypes.AntiAlias);
                Cv2.ImShow("img", img);
            }
        }

        public override string ToString()
        {
            return $"NoteData[p{ParticipantID}, Day{Day}] Look: {LookVector} Trans: {NewTranslation} File: {File.Name}";
        }

        private Point CalcPointAvg(Point[] pts)
        {
            double xsum = 0;
            double ysum = 0;
            foreach (var pt in pts)
            {
                xsum += pt.X;
                ysum += pt.Y;
            }
            return new Point(xsum / pts.Length, ysum / pts.Length);
        }

        private bool HasPoint(Rect rect, Point pt)
        {
            return (rect.X <= pt.X && rect.Y <= pt.Y && rect.X + rect.Width >= pt.X && rect.Y + rect.Height >= pt.Y);
        }
    }

    class AnnotationReader
    {
        public DirectoryInfo DirectoryPath { get; set; }
        public List<NoteData> Datas { get; private set; } = new List<NoteData>();

        public AnnotationReader(DirectoryInfo path)
        {
            DirectoryPath = path;
        }

        public void Read()
        {
            Datas.Clear();
            
            var pdis = DirectoryPath.GetDirectories();
            foreach(DirectoryInfo pdi in pdis)
            {
                if (pdi.Name.ToLower().StartsWith("p"))
                {
                    int pid = Convert.ToInt32(pdi.Name.TrimStart('p'));
                    var ddis = pdi.GetDirectories();
                    Parallel.ForEach(ddis, new ParallelOptions() { MaxDegreeOfParallelism =  Environment.ProcessorCount * 2}, (ddi) =>
                    {
                        if (ddi.Name.ToLower().StartsWith("day"))
                        {
                            int day = Convert.ToInt32(ddi.Name.TrimStart('d', 'a', 'y'));
                            string annotation = Path.Combine(ddi.FullName, "annotation.txt");

                            Console.WriteLine("Add Annotation: " + annotation);
                            AddNotation(pid, day, annotation);
                        }
                    });
                }
            }
        }

        private void AddNotation(int pid, int dayId, string filepath)
        {
            int count = 0;
            List<char> decodeBuffer = new List<char>();
            List<double> valueBuffer = new List<double>();
            List<NoteData> dataBuffer = new List<NoteData>();

            string directoryPath = Path.GetDirectoryName(filepath);

            using (StreamReader reader = new StreamReader(filepath))
            {
                while (!reader.EndOfStream)
                {
                    char c = (char)reader.Read();
                    if (c == ' ' || c == '\n' || reader.EndOfStream)
                    {
                        string decodedString = new string(decodeBuffer.ToArray());
                        decodeBuffer.Clear();
                        double decodedValue = Convert.ToDouble(decodedString);
                        valueBuffer.Add(decodedValue);
                        if (c == '\n')
                        {
                            count++;
                            try
                            {
                                dataBuffer.Add(new NoteData(pid, dayId, valueBuffer.ToArray(), new FileInfo(Path.Combine(directoryPath, count.ToString("0000") + ".jpg"))));
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine($"ERROR while process, File:{filepath} Line:{count}\n"+ex.ToString());
                            }
                            valueBuffer.Clear();
                        }
                    }
                    else
                    {
                        decodeBuffer.Add(c);
                    }
                }
            }

            lock (Datas)
            {
                Datas.AddRange(dataBuffer);
            }

            Console.WriteLine($"{count} counts annotaioned image is decoded");
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Annotation Data[{DirectoryPath.FullName}]");
            builder.AppendLine($"{Datas.Count} counts notes");
            builder.AppendLine("===Notes===");
            foreach (NoteData data in Datas)
            {
                builder.AppendLine(data.ToString());
            }

            return builder.ToString();
        }
    }
}
