using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using OS = Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util.Zip;

namespace Vision.Android
{
    public class AndroidStorage : Storage
    {
        string absolutepath;
        public AndroidStorage()
        {
            //absolutepath = InternalPathCombine((((AndroidCv)Core.Cv).AppContext).FilesDir.AbsolutePath, "Vision");
            absolutepath = "/sdcard/Vision";
            DirectoryInfo di = new DirectoryInfo(absolutepath);
            if (!di.Exists)
            {
                di.Create();
            }
        }

        protected override string GetAbsoluteRoot()
        {
            return absolutepath;
        }

        protected override void InternalCopy(Stream source, FileNode dist)
        {
            FileInfo fi = new FileInfo(dist.AbosolutePath);
            using (FileStream target = fi.Open(FileMode.OpenOrCreate))
            {
                source.CopyTo(target);
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
            if (path.IsDirectory)
            {
                return Directory.Exists(path.AbosolutePath);
            }
            return File.Exists(path.AbosolutePath);
        }

        protected override DirectoryNode InternalCreateDirectory(DirectoryNode path)
        {
            Directory.CreateDirectory(path.AbosolutePath);
            return path;
        }

        protected override FileNode InternalCreateFile(FileNode path)
        {
            using (File.Create(path.AbosolutePath))
            {

            }
            return path;
        }

        protected override char[] GetInvalidPathChars()
        {
            return Path.GetInvalidFileNameChars();
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

        protected override void InternalUnZip(FileNode zipfile, DirectoryNode outputdir)
        {
            ZipFile zip = new ZipFile(zipfile.AbosolutePath, outputdir.AbosolutePath);
            zip.UnZip();
        }
    }
}