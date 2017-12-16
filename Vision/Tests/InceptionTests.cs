using OpenCvSharp;
using OpenCvSharp.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;
using Vision.Cv;
using Vision.Tensorflow;

namespace Vision.Tests
{
    public class InceptionTests : IDisposable
    {
        static Graph graph;
        static string[] resultTag;

        int index = -1;
        string filepath;
        Capture cap;
        Session sess;

        public InceptionTests(int index)
        {
            this.index = index;
            cap = Capture.New(index);
            cap.FrameReady += Cap_FrameReady;

            Load();
        }

        public InceptionTests(string filepath)
        {
            this.filepath = filepath;
            cap = Capture.New(filepath);
            cap.FrameReady += Cap_FrameReady;

            Load();
        }

        public void Load()
        {
            Logger.Log("Download model");
            FileNode f = Storage.Root.GetFile("inception5h.zip");
            DirectoryNode dir = Storage.Root.GetDirectory("Inception5h");
            if (!f.IsExist)
            {
                f.Create();
                string webPath = "https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip";
                using (HttpClient hc = new HttpClient())
                {
                    byte[] buffer = hc.GetByteArrayAsync(webPath).Result;
                    f.WriteBytes(buffer);
                }
                if (!dir.IsExist)
                    dir.Create();
                else
                {
                    FileNode[] files = dir.GetFiles();
                    if (files != null)
                    {
                        foreach (FileNode existsfile in files)
                        {
                            existsfile.Delete();
                        }
                    }
                }
            }
            Logger.Log("Finish download model");

            FileNode graphfile = dir.GetFile("tensorflow_inception_graph.pb");
            FileNode indexfile = dir.GetFile("imagenet_comp_graph_label_strings.txt");
            if (!graphfile.IsExist || !indexfile.IsExist)
                Storage.UnZip(f, dir);

            Logger.Log("Load graph");
            if (graph == null)
            {
                graph = new Graph();
                graph.ImportPb(graphfile);
                Logger.Log("Graph load finished");
            }
            else
            {
                Logger.Log("Graph is loaded");
            }

            Logger.Log("Load Index File");
            if (resultTag == null)
            {
                resultTag = indexfile.ReadLines();
                Logger.Log($"{resultTag.Length} Counts indexes are loaded");
            }
            else
            {
                Logger.Log("Index file already loaded");
            }

            sess = new Session(graph);
        }

        public void Run()
        {
            Start();
            Join();
        }

        public void Start()
        {
            cap.Start();
        }

        public void Stop()
        {
            cap.Stop();
        }

        public void Join()
        {
            cap.Wait();
        }

        InferenceResult[] inferences;
        private void Cap_FrameReady(object sender, FrameArgs e)
        {
            if(e.LastKey == 'e')
            {
                e.Break = true;
                Core.Cv.CloseAllWindows();
                return;
            }

            var pt1 = LayoutHelper.ResizePoint(new Point(0, 0), new Size(10000, 10000), e.Mat.Size().ToSize(), Stretch.Uniform);
            var pt2 = LayoutHelper.ResizePoint(new Point(10000 - 1, 10000 - 1), new Size(10000, 10000), e.Mat.Size().ToSize(), Stretch.Uniform);
            var rect = new Rect(pt1, pt2);
            using (Mat roi = new Mat(e.Mat, rect.ToCvRect()))
            {
                Update(roi);

                e.Mat.DrawRectangle(rect, Scalar.BgrMagenta);

                var roiDrawRect = new Rect(e.Mat.Width - 150 - 50, e.Mat.Height - 150 - 50, 150, 150);
                roi.Resize(roiDrawRect.Size);
                Core.Cv.DrawMatAlpha(e.Mat, roi, roiDrawRect.Point);
                e.Mat.DrawRectangle(roiDrawRect, Scalar.BgrWhite);

                if (inferences != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        InferenceResult r = inferences[i];
                        e.Mat.DrawText(30, 50 + 40 * i, $"Top {i + 1}: {resultTag[r.Id]} ({(r.Result * 100).ToString("0.00")}%)", Scalar.BgrGreen);
                    }
                }
                else
                {
                    e.Mat.DrawText(30, 50, $"Result: Wait for inference...", Scalar.BgrGreen);
                }
                var fps = Profiler.Get("InferenceFPS");
                var time = Profiler.Get("InferenceRun");
                e.Mat.DrawText(30, e.Mat.Height - 50, $"Inference FPS: {fps} ({time.ToString("0.0")}ms / RealFPS: {(1000 / time).ToString("0.0")})", Scalar.BgrGreen);

                Core.Cv.ImgShow("result", e.Mat);
            }
        }

        Task inferenceTask;
        private void Update(Mat mat)
        {
            if(inferenceTask==null || inferenceTask.IsCompleted)
            {
                Mat cloned = mat.Clone();
                inferenceTask = Task.Factory.StartNew(() =>
                {
                    Inference(cloned);
                }, TaskCreationOptions.LongRunning);
            }
        }

        private void Inference(Mat mat)
        {
            Profiler.Start("InferenceALL");

            Profiler.Start("InferenceDecodeImg");
            Tensor img = Tools.MatBgr2Tensor(mat, NormalizeMode.None, 224, 224, new long[] { 1, 224, 224, 3 });
            Profiler.End("InferenceDecodeImg");

            Profiler.Start("InferenceNormalizeImg");
            Tensor normalized;
            using (Session convert = new Session())
            {
                TFGraph g = convert.Graph.NativeGraph;
                var input = g.Placeholder(TFDataType.Float);
                var output = g.Sub(input, g.Const(117.0f));
                var normfetch = convert.NativeSession.Run(new[] { input }, new[] { img.NativeTensor }, new[] { output });
                normalized = new Tensor(normfetch[0]);
            }
            Profiler.End("InferenceNormalizeImg");

            Profiler.Start("InferenceRun");
            Tensor[] fetches = sess.Run(new[] { "output" }, new Dictionary<string, Tensor>() { { "input", normalized } });
            Profiler.End("InferenceRun");

            Tensor result = fetches[0];
            float[] list = ((float[][])result.GetValue(true))[0];
            InferenceResult[] resultList = new InferenceResult[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                resultList[i] = new InferenceResult(i, list[i]);
            }
            resultList = resultList.OrderByDescending(x => x.Result).ToArray();

            inferences = resultList;

            Profiler.End("InferenceALL");
            Profiler.Count("InferenceFPS");
            foreach (var item in fetches)
            {
                item.Dispose();
            }
            normalized.Dispose();
            img.Dispose();
            mat.Dispose();
        }

        public void Dispose()
        {
            if (cap != null)
            {
                cap.Stop();
                cap.Dispose();
                cap = null;
            }

            if(inferenceTask != null)
            {
                inferenceTask.Wait();
                inferenceTask = null;
            }

            if(sess != null)
            {
                sess.Dispose();
                sess = null;
            }
        }
    }

    public class InferenceResult
    {
        public int Id;
        public float Result;
        public InferenceResult(int id, float result)
        {
            Id = id;
            Result = result;
        }
    }
}
