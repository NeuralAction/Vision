using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public static class Util
    {
        public static byte[] ReadAll(this Stream s)
        {
            int len = (int)s.Length;
            byte[] buff = new byte[len];
            s.Read(buff, 0, len);
            return buff;
        }
    }
}
