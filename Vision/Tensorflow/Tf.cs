using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;

namespace Vision.Tensorflow
{
    public static class TF
    {
        public static Graph Graph;

        static TF()
        {
            Graph = new Graph();
        }
    }
}
