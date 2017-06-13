using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class ManifestResource
    {
        public string FileName { get; set; }
        public string Namespace { get; set; }
        public string Resource
        {
            get
            {
                return Namespace + "." + FileName;
            }
        }

        public ManifestResource(string nameSpace, string filename)
        {
            Namespace = nameSpace;
            FileName = filename;
        }

        public override string ToString()
        {
            return Resource;
        }
    }
}
