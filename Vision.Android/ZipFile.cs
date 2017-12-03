using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util.Zip;
using Java.IO;
using System.IO;

namespace Vision.Android
{
    public class ZipFile
    {
        String _zipFile;
        String _location;

        public ZipFile(String zipFile, String location)
        {
            _zipFile = zipFile;
            _location = location;
            DirChecker("");
        }

        void DirChecker(String dir)
        {
            var file = new Java.IO.File(_location + dir);

            if (!file.IsDirectory)
            {
                file.Mkdirs();
            }
        }

        const int bufferlen = 32000;
        public void UnZip()
        {
            try
            {
                using (Stream fileInputStream = System.IO.File.OpenRead(_zipFile))
                using (var zipInputStream = new ZipInputStream(fileInputStream))
                {
                    while (true)
                    {
                        using(ZipEntry entry = zipInputStream.NextEntry)
                        {
                            if (entry == null)
                                break;

                            Logger.Log("Decompress", "UnZipping : " + entry.Name);

                            if (entry.IsDirectory)
                            {
                                DirChecker(entry.Name);
                            }
                            else
                            {
                                var fileNode = new FileNode(_location + entry.Name, true);
                                if (!fileNode.IsExist)
                                {
                                    using (var fileOutputStream = new FileOutputStream(_location + entry.Name))
                                    {
                                        byte[] buffer = new byte[bufferlen];
                                        int bindex = 0;
                                        int bcount = 0;
                                        for (int i = zipInputStream.Read(); i != -1; i = zipInputStream.Read())
                                        {
                                            if (bcount >= bufferlen)
                                            {
                                                fileOutputStream.Write(buffer, 0, bcount);
                                                bcount = 0;
                                                bindex = 0;
                                            }
                                            buffer[bindex] = (byte)i;
                                            bindex++;
                                            bcount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Decompress", ex);
            }
        }

    }
}