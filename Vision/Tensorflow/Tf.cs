using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;

namespace Vision.Tensorflow
{
    public static class Tf
    {
        public static Graph Graph;

        static Tf()
        {
            Graph = new Graph();
        }
    }
}
