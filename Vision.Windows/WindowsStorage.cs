using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Windows
{
    public class WindowsStorage : Storage
    {
        string absolutePath;

        public WindowsStorage()
        {
            absolutePath = Path.Combine(Environment.CurrentDirectory, "Vision");
            DirectoryInfo di = new DirectoryInfo(absolutePath);
            if (!di.Exists)
            {
                di.Create();
            }
        }

        protected override string GetAbsoluteRoot()
        {
            return absolutePath;
        }

        protected override void InternalCopy(Stream source, FileNode dist)
        {
            using(Stream output = dist.Open())
            {
                source.CopyTo(output);
            }
        }

        protected override void InternalMove(FileNode source, FileNode dist)
        {
            File.Move(source.AbosolutePath, dist.AbosolutePath);
        }

        protected override void InternalDelete(FileNode path)
        {
            File.Delete(path.AbosolutePath);
        }

        protected override Stream InternalGetFileStream(FileNode node)
        {

            return File.Open(node.AbosolutePath, FileMode.Open);
        }

        protected override bool InternalIsExist(StorageNode path)
        {
            if (path.IsFile)
            {
                return File.Exists(path.AbosolutePath);
            }
            return Directory.Exists(path.AbosolutePath);
        }

        protected override DirectoryNode InternalCreateDirectory(DirectoryNode node)
        {
            Directory.CreateDirectory(node.AbosolutePath);
            return node;
        }

        protected override FileNode InternalCreateFile(FileNode node)
        {
            using(File.Create(node.AbosolutePath))
            {
            }
            return node;
        }

        protected override string InternalPathCombine(params string[] pathes)
        {
            return base.InternalPathCombine(pathes).TrimStart('/').Replace("/", "\\");
        }

        protected override char[] GetInvalidPathChars()
        {
            List<char> c = new List<char>();
            c.AddRange(Path.GetInvalidPathChars());
            c.Add(Path.VolumeSeparatorChar);
            return c.ToArray();
        }

        protected override DirectoryNode[] InternalGetDirectories(DirectoryNode node)
        {
            if (node.IsExist)
            {
                DirectoryInfo di = new DirectoryInfo(node.AbosolutePath);
                DirectoryInfo[] dis = di.GetDirectories();
                if (dis.Length == 0)
                    return null;

                List<DirectoryNode> nodes = new List<DirectoryNode>();
                foreach (DirectoryInfo dir in dis)
                    nodes.Add(node.GetDirectory(dir.Name));
                return nodes.ToArray();
            }
            return null;
        }

        protected override FileNode[] InternalGetFiles(DirectoryNode node)
        {
            if (node.IsExist)
            {
                DirectoryInfo di = new DirectoryInfo(node.AbosolutePath);
                FileInfo[] dis = di.GetFiles();
                if (dis.Length == 0)
                    return null;

                List<FileNode> nodes = new List<FileNode>();
                foreach (FileInfo dir in dis)
                    nodes.Add(node.GetFile(dir.Name));
                return nodes.ToArray();
            }
            return null;
        }

        protected override void InternalUnZip(FileNode zipfile, DirectoryNode outputdir, bool overwrite)
        {
            using (var zip = ZipFile.Open(zipfile.AbosolutePath, ZipArchiveMode.Read))
            {
                foreach (var item in zip.Entries)
                {
                    var abs = PathCombine(outputdir.AbosolutePath, item.FullName);
                    if (abs.EndsWith("\\") || abs.EndsWith("/"))
                        CreateDirectory(abs, true);
                    else
                        item.ExtractToFile(abs, overwrite);
                }
            }
        }
    }
}
