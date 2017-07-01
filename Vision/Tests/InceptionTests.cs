using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;

namespace Vision.Tests
{
    public class InceptionTests : IDisposable
    {
        static Graph graph;
        static string[] resultTag;

        int index = -1;
        string filepath;
        Capture cap;

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

            cap.FrameReady += Cap_FrameReady;
            cap.Start();
        }

        public void Join()
        {
            cap.Join();
        }

        string inference = "";
        private void Cap_FrameReady(object sender, FrameArgs e)
        {
            if(e.LastKey == 'e')
            {
                e.Break = true;
                Core.Cv.CloseAllWindows();
                return;
            }

            Update(e.VMat);

            e.VMat.DrawText(0, 50, $"Result: {inference}", Scalar.Black);

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
            using (Session sess = new Session(graph))
            {
                Profiler.Start("InferenceALL");

                Profiler.Start("InferenceDecodeImg");
                Tensor img = Tools.VMatRGB2Tensor(mat, 224, 224, new long[] { 1, 224, 224, 3 });
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
                float max = -1;
                int maxInd = -1;
                for (int i = 0; i < list.Length; i++)
                {
                    if (max < list[i])
                    {
                        max = list[i];
                        maxInd = i;
                    }
                }

                if (maxInd > -1)
                {
                    inference = $"{resultTag[maxInd]}, {(max*100).ToString("0.00")}%";
                }
                else
                {
                    inference = "None";
                }

                Profiler.End("InferenceALL");
            }
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
        }
    }
}
