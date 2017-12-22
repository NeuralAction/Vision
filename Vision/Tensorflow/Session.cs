using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TensorFlow;

namespace Vision.Tensorflow
{
    public class StatSummarizer
    {
        public class Detail
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Shape { get; set; }
            public double Time => timeSum / timeCount;

            double timeSum = 0;
            int timeCount = 0;

            public void UpdateTime(double time)
            {
                timeSum += time;
                timeCount++;
            }

            public override string ToString()
            {
                return $"({Type}) \"{Name}\" {Shape} {Time.ToString("0.0000")}ms";
            }
        }

        Dictionary<string, Detail> data = new Dictionary<string, Detail>();

        public void Update(Proto.RunMetadata meta)
        {
            //void StatSummarizer::ProcessStepStats(const StepStats&step_stats) {
            //    int64 curr_total_us = 0;
            //    int64 mem_total = 0;

            //    int64 first_node_start_us =
            //        step_stats.dev_stats(0).node_stats(0).all_start_micros();

            //    int node_num = 0;
            //    for (const auto&ds : step_stats.dev_stats()) {
            //        for (const auto&ns : ds.node_stats()) {
            //            ++node_num;
            //            const int64 curr_time = ns.all_end_rel_micros();
            //            curr_total_us += curr_time;
            //            auto result = details_.emplace(ns.node_name(), Detail());
            //            Detail* detail = &(result.first->second);

            //            detail->start_us.UpdateStat(ns.all_start_micros() - first_node_start_us);
            //            detail->rel_end_us.UpdateStat(curr_time);

            //            // If this is the first pass, initialize some values.
            //            if (result.second)
            //            {
            //                detail->name = ns.node_name();
            //                detail->type = OpType(ds, ns);

            //                detail->run_order = node_num;

            //                detail->outputs.resize(ns.output_size());
            //                for (const auto&output : ns.output()) {
            //                    const int32 slot = output.slot();
            //                    if ((slot < 0) || (slot >= ns.output_size()))
            //                    {
            //                        LOG(ERROR) << "Bad output slot '" << slot << "' for '"
            //                                   << ns.node_name() << "'";
            //                        continue;
            //                    }
            //                    detail->outputs[slot] = output.tensor_description();
            //                }
            //            }

            //            int64 curr_node_mem = 0;
            //            for (const auto&mem : ns.memory()) {
            //                const int64 mem_usage = mem.total_bytes();
            //                curr_node_mem += mem_usage;
            //            }
            //            detail->mem_used.UpdateStat(curr_node_mem);
            //            mem_total += curr_node_mem;

            //            Validate(detail, ns);
            //        }
            //    }

            //    run_total_us_.UpdateStat(curr_total_us);
            //    memory_.UpdateStat(mem_total);
            //}

            foreach(var ds in meta.StepStats.DevStats)
            {
                foreach(var ns in ds.NodeStats)
                {
                    var name = ns.NodeName;
                    var time = ns.AllEndRelMicros / 1000.0;

                    if (!data.ContainsKey(name))
                    {
                        var type = OpType(ds, ns);
                        Proto.TensorShapeProto.Types.Dim[] shape = null;

                        if(ns.Output.Count > 0)
                        {
                            shape = ns.Output.First().TensorDescription.Shape.Dim.ToArray();
                        }

                        string shapeText = "[";
                        if (shape != null)
                        {
                            foreach (var d in shape)
                            {
                                shapeText += $"{d.Size}, ";
                            }
                            shapeText.TrimEnd(' ', ',');
                        }
                        shapeText += "]";

                        data.Add(name, new Detail());
                        data[name].Name = name;
                        data[name].Type = type;
                        data[name].Shape = shapeText;
                    }
                    data[name].UpdateTime(time);
                }
            }
        }

        public void Clear()
        {
            data.Clear();
        }

        public string ReportByTime(int max = 1000000)
        {
            var data = this.data.Values.ToArray();
            data = data.OrderByDescending((d) => { return d.Time; }).ToArray();

            StringBuilder builder = new StringBuilder();
            int line = 0;
            foreach (var l in data)
            {
                builder.AppendLine($"{l.ToString()}");
                line++;
                if (line > max)
                {
                    break;
                }
            }
            return builder.ToString();
        }

        string OpType(Proto.DeviceStepStats ds, Proto.NodeExecStats ns)
        {
            if(ds.Device.Contains("/stream") || ds.Device.Contains("/memcpy"))
            {
                return "<>";
            }

            var label = ns.TimelineLabel;

            if (!label.Contains(" = "))
            {
                return "<>";
            }
            var start = label.IndexOf(" = ");
            start += 3;
            var end = label.IndexOf("(", start);
            return label.Substring(start, end - start);
        }
    }

    public class Session : IDisposable
    {
        internal TFSession sess;
        public TFSession NativeSession { get => sess; set => sess = value; }

        public Graph Graph { get; set; }
        TFSession.Runner runner;

        public bool EnableSummary { get; set; } = true;
        public StatSummarizer Summary { get; set; } = new StatSummarizer();

        public Session() : this(new Graph())
        {

        }

        public Session(Graph graph)
        {
            Graph = graph ?? throw new ArgumentNullException("graph");
            try
            {
                sess = new TFSession(Graph.graph);
                runner = sess.GetRunner();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                throw ex;
            }
        }
        
        public Tensor Run(Output output)
        {
            runner.Fetch(output.NativeOutput);
            TFTensor tensor = runner.Run();
            return new Tensor(tensor);
        }

        byte[] TraceAll = new byte[] { 0x08, 0x03 };
        public Tensor[] Run(string[] Fetches, Dictionary<string, Tensor> Input)
        {
            try
            {
                foreach (string key in Input.Keys)
                {
                    runner.AddInput(key, Input[key].NativeTensor);
                }

                foreach (string s in Fetches)
                {
                    runner.Fetch(Fetches);
                }

                if (EnableSummary)
                {
                    runner.RunOptions = new TFBuffer(TraceAll);
                    runner.RunMetadata = new TFBuffer();
                }

                TFTensor[] ret = runner.Run();
                Tensor[] retConv = new Tensor[ret.Length];
                for (int i = 0; i < ret.Length; i++)
                {
                    retConv[i] = new Tensor(ret[i]);
                }

                if (EnableSummary)
                {
                    var buf = runner.RunMetadata.ToArray();
                    var b = Proto.RunMetadata.Parser.ParseFrom(buf);

                    Summary.Update(b);

                    runner.RunOptions.Dispose();
                    runner.RunMetadata.Dispose();

                    var str = Summary.ReportByTime();
                }

                return retConv;
            }
            catch (Exception ex)
            {
                Logger.Throw("Error occur while run TFSession", ex);
            }
            finally
            {
                runner = NativeSession.GetRunner();
            }
            return null;
        }

        public void Dispose()
        {
            if (sess != null)
            {
                sess.CloseSession();
                sess.Dispose();
                sess = null;
            }
        }
    }

    public class Output
    {
        internal TFOutput output;
        public TFOutput NativeOutput { get => output; set => output = value; }

        internal Output(TFOutput output)
        {
            this.output = output;
        }
    }

    public class Operation
    {
        internal TFOperation operation;
        public TFOperation NativeOperation { get => operation; set => operation = value; }
        public string Name => operation.Name;
        public string Type => operation.OpType;

        public Operation(TFOperation op)
        {
            operation = op;
        }
    }

    public class Tensor : IDisposable
    {
        internal TFTensor tensor;
        public TFTensor NativeTensor { get => tensor; set => tensor = value; }

        public Tensor(TFTensor tensor)
        {
            this.tensor = tensor;
        }

        public object GetValue(bool arrayOfArray = false)
        {
            return tensor.GetValue(arrayOfArray);
        }

        public void Dispose()
        {
            if(tensor != null)
            {
                tensor.Dispose();
            }
        }
    }

    public class Graph : IDisposable
    {
        internal TFGraph graph;
        public TFGraph NativeGraph { get => graph; set => graph = value; }

        public Graph()
        {
            try
            {
                graph = new TFGraph();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        public void ImportPb(Stream stream, string prefix="")
        {
            graph.Import(stream.ReadAll(), prefix);
        }

        public void ImportPb(FileNode node, string prefix="")
        {
            graph.Import(node.ReadBytes(), prefix);
        }

        public void SetAsDefault()
        {
            TF.Graph = this;
        }

        public void Dispose()
        {
            if(graph != null)
            {
                graph.Dispose();
                graph = null;
            }
        }
    }
}
