using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;

namespace Vision
{
    public class Session : IDisposable
    {
        internal TFSession sess;
        public TFSession NativeSession { get => sess; set => sess = value; }

        public Graph Graph { get; set; }

        public Session()
        {
            Graph = new Graph();
            sess = new TFSession(Graph.graph);
        }

        public Session(Graph graph)
        {
            Graph = graph;
            sess = new TFSession(graph.graph);
        }
        
        public Tensor Run(Output output)
        {
            var runner = sess.GetRunner();
            runner = runner.Fetch(output.NativeOutput);
            TFTensor tensor = runner.Run();
            return new Tensor(tensor);
        }

        public Tensor[] Run(string[] Fetches, Dictionary<string, Tensor> Input)
        {
            var runner = sess.GetRunner();

            foreach(string key in Input.Keys)
            {
                runner = runner.AddInput(key, Input[key].NativeTensor);
            }

            foreach(string s in Fetches)
            {
                runner = runner.Fetch(Fetches);
            }

            TFTensor[] ret = runner.Run();
            Tensor[] retConv = new Tensor[ret.Length];
            for(int i =0; i<ret.Length; i++)
            {
                retConv[i] = new Tensor(ret[i]);
            }

            return retConv;
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
                tensor = null;
            }
        }
    }

    public class Graph : IDisposable
    {
        internal TFGraph graph;
        public TFGraph NativeGraph { get => graph; set => graph = value; }

        public Graph()
        {
            graph = new TFGraph();
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
            Tf.Graph = this;
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
