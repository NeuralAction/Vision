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
        }

        public InceptionTests(string filepath)
        {
            this.filepath = filepath;
            cap = Capture.New(filepath);
        }

        public void Run()
        {
            Start();
            Join();
        }

        public void Start()
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

            cap.FrameReady += Cap_FrameReady;
            cap.Start();
        }

        public void Join()
        {
            cap.Join();
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

            Update(e.VMat);

            if(inferences != null)
            {
                for(int i=0; i<3; i++)
                {
                    InferenceResult r = inferences[i];
                    e.VMat.DrawText(0, 50 + 40 * i, $"Top {i + 1}: {resultTag[r.Id]} ({(r.Result*100).ToString("0.00")}%)", Scalar.BgrGreen);
                }
            }
            else
            {
                e.VMat.DrawText(0, 50, $"Result: Wait for inference...", Scalar.BgrGreen);
            }
            e.VMat.DrawText(0, e.VMat.Height - 50, $"Inference FPS: {Profiler.Get("InferenceFPS")} ({Profiler.Get("InferenceALL").ToString("0")}ms)", Scalar.BgrGreen);

            Core.Cv.ImgShow("result", e.VMat);
        }

        Task inferenceTask;
        private void Update(VMat mat)
        {
            if(inferenceTask==null || inferenceTask.IsCompleted)
            {
                VMat cloned = mat.Clone();
                inferenceTask = new Task(() =>
                {
                    Inference(cloned);
                });
                inferenceTask.Start();
            }
        }

        private void Inference(VMat mat)
        {
            Profiler.Start("InferenceALL");

            Profiler.Start("InferenceDecodeImg");
            Tensor img = Tools.VMatBgr2Tensor(mat, 224, 224, new long[] { 1, 224, 224, 3 });
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
            normalized.Dispose();
            img.Dispose();

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
