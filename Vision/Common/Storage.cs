using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision
{
    public abstract class Storage
    {
        private static Storage Current;

        public static DirectoryNode Root => new DirectoryNode("/");
        
        public static string AbsoluteRoot => Current.GetAbsoluteRoot();
        public static char[] InvalidPathChars => Current.GetInvalidPathChars();
        public static string[] SupportedImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };

        public static void Init(Storage storage)
        {
            if(Current == null)
            {
                Logger.Log("Use Native Storage: " + storage.ToString());

                Current = storage;
            }
            else
            {
                Logger.Log("Already Inited");
            }
        }

        public static void Copy(Stream source, FileNode dist)
        {
            Current.InternalCopy(source, dist);
        }
        public static void Copy(Stream source, string dist)
        {
            Current.InternalCopy(source, new FileNode(dist));
        }
        protected abstract void InternalCopy(Stream source, FileNode dist);

        public static void Move(FileNode source, FileNode dist)
        {
            Current.InternalMove(source, dist);
        }
        protected abstract void InternalMove(FileNode source, FileNode dist);

        public static void Delete(string path)
        {
            Current.InternalDelete(new FileNode(path));
        }
        public static void Delete(FileNode path)
        {
            Current.InternalDelete(path);
        }
        protected abstract void InternalDelete(FileNode path);
        
        protected abstract string GetAbsoluteRoot();

        public static string GetAbsolutePath(string path)
        {
            return PathCombine(AbsoluteRoot, path);
        }
        
        public static Stream GetFileStream(FileNode node)
        {
            return Current.InternalGetFileStream(node);
        }
        protected abstract Stream InternalGetFileStream(FileNode node);

        public static DirectoryNode[] GetDirectories(DirectoryNode node)
        {
            return Current.InternalGetDirectories(node);
        }
        protected abstract DirectoryNode[] InternalGetDirectories(DirectoryNode node);

        public static FileNode[] GetFiles(DirectoryNode node)
        {
            return Current.InternalGetFiles(node);
        }
        protected abstract FileNode[] InternalGetFiles(DirectoryNode node);

        public static FileNode CreateFile(string path, bool isAbsolute = false)
        {
            return Current.InternalCreateFile(new FileNode(path, isAbsolute));
        }
        public static FileNode CreateFile(FileNode node)
        {
            return Current.InternalCreateFile(node);
        }
        protected abstract FileNode InternalCreateFile(FileNode path);

        public static DirectoryNode CreateDirectory(string path, bool isAbsolute = false)
        {
            return Current.InternalCreateDirectory(new DirectoryNode(path, isAbsolute));
        }
        public static DirectoryNode CreateDirectory(DirectoryNode node)
        {
            return Current.InternalCreateDirectory(node);
        }
        protected abstract DirectoryNode InternalCreateDirectory(DirectoryNode path);

        protected abstract char[] GetInvalidPathChars();

        public static bool CheckPathChars(StorageNode node)
        {
            return CheckPathChars(node.Path);
        }

        public static bool CheckPathChars(string path)
        {
            char[] invalid = InvalidPathChars;
            foreach (char c in path)
            {
                if (invalid.Contains(c))
                    return false;
            }
            return true;
        }

        public static void FixPathChars(StorageNode node)
        {
            node.Path = FixPathChars(node.Path);
        }

        public static string FixPathChars(string path)
        {
            StringBuilder builder = new StringBuilder();
            char[] invalid = InvalidPathChars;
            int start = 0;

            if(path.Length > 1 && path[1] == ':')
            {
                builder.Append(path[0]);
                builder.Append(path[1]);

                start = 2;
            }

            for (int i = start; i < path.Length; i++)
            {
                char s = path[i];
                if (invalid.Contains(s))
                    builder.Append('-');
                else
                    builder.Append(s);
            }
            return builder.ToString();
        }

        public static string PathCombine(params string[] pathes)
        {
            return Current.InternalPathCombine(pathes);
        }
        protected virtual string InternalPathCombine(params string[] pathes)
        {
            string ret = "";
            foreach (string s in pathes)
            {
                ret = ret.TrimEnd('\\', '/') + "/" + s.TrimStart('\\', '/');
            }
            return ret;
        }

        public static bool IsExist(StorageNode path)
        {
            return Current.InternalIsExist(path);
        }
        protected abstract bool InternalIsExist(StorageNode path);

        public static FileNode LoadResource(ManifestResource resource, bool overwrite = false)
        {
            Logger.Log("Load Resource: " + resource);
            var assembly = typeof(Core).GetTypeInfo().Assembly;

            FileNode node = Root.GetFile(resource.FileName);
            if (!overwrite && node.IsExist)
            {
                Logger.Log("resource finded! " + node.AbosolutePath);
            }
            else
            {
                if (overwrite)
                {
                    Logger.Log("resource is overwriting. copy to: " + node.AbosolutePath);
                    if (node.IsExist)
                    {
                        node.Delete();
                    }
                }
                else
                {
                    Logger.Log("resource not found. copy to : " + node.AbosolutePath);
                }

                using (Stream stream = assembly.GetManifestResourceStream(resource.Resource))
                {
                    if (stream == null)
                        throw new FileNotFoundException("Resource is not founded: " + resource.Resource);
                    if (!node.IsExist)
                        node.Create();
                    Copy(stream, node);
                }
            }

            return node;
        }

        public static void UnZip(FileNode zipfile, DirectoryNode outputdir, bool overwrite = false)
        {
            Current.InternalUnZip(zipfile, outputdir, overwrite);
        }
        protected abstract void InternalUnZip(FileNode zipfile, DirectoryNode outputdir, bool overwrite);

        public static bool IsImage(FileNode node)
        {
            return IsImage(node.AbosolutePath);
        }

        public static bool IsImage(string path)
        {
            foreach (var ext in SupportedImageExtensions)
            {
                if (path.ToLower().EndsWith(ext))
                    return true;
            }
            return false;
        }
    }

    public abstract class StorageNode
    {
        public bool IsAbsolute { get; set; }
        public string Path { get; set; }
        public string AbosolutePath
        {
            get
            {
                if (IsAbsolute)
                    return Path;
                else
                    return Storage.GetAbsolutePath(Path);
            }
        }

        public virtual string Name { get => System.IO.Path.GetFileName(Path); }

        public abstract bool IsFile { get; }
        public abstract bool IsDirectory { get; }
        public abstract bool IsExist { get; }

        public override string ToString()
        {
            if (IsFile)
                return string.Format("File, {0}, AbsolutePath: {1}", Path, AbosolutePath);
            else
                return string.Format("Directory, {0}, AbsolutePath: {1}", Path, AbosolutePath);
        }
    }

    public class FileNode : StorageNode
    {
        public override bool IsFile => true;
        public override bool IsDirectory => false;
        public override bool IsExist => Storage.IsExist(this);

        public FileNode()
        {

        }

        public FileNode(string path, bool isAbsolute = false)
        {
            IsAbsolute = isAbsolute;
            Path = path;
        }

        public Stream Open()
        {
            return Storage.GetFileStream(this);
        }

        public void Create()
        {
            Storage.CreateFile(this);
        }

        public void Delete()
        {
            Storage.Delete(this);
        }

        public void Move(FileNode dist)
        {
            Storage.Move(this, dist);
        }

        public byte[] ReadBytes()
        {
            using(Stream stream = Open())
            {
                return stream.ReadAll();
            }
        }

        public string[] ReadLines()
        {
            List<string> lines = new List<string>();
            using (Stream filestream = Open())
            {
                using(StreamReader reader = new StreamReader(filestream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        lines.Add(line);
                    }
                }
            }
            return lines.ToArray();
        }

        public void WriteLines(string[] lines)
        {
            using(Stream stream = Open())
            {
                using(StreamWriter writer = new StreamWriter(stream))
                {
                    foreach(string l in lines)
                    {
                        writer.WriteLine(l);
                    }
                }
            }
        }

        public void WriteText(StringBuilder builder)
        {
            WriteText(builder.ToString());
        }

        public void WriteText(string text)
        {
            using(Stream stream = Open())
            {
                using(StreamWriter write = new StreamWriter(stream))
                {
                    write.Write(text);
                }
            }
        }

        public void WriteBytes(byte[] buffer)
        {
            using(Stream stream = Open())
            {
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }

    public class DirectoryNode : StorageNode
    {
        public override bool IsFile => false;
        public override bool IsDirectory => true;
        public override bool IsExist => Storage.IsExist(this);

        public DirectoryNode()
        {

        }

        public DirectoryNode(string path, bool isAbsolute = false)
        {
            IsAbsolute = isAbsolute;
            Path = path;
        }

        public void Create()
        {
            Storage.CreateDirectory(this);
        }

        public FileNode GetFile(string name)
        {
            return new FileNode(Storage.PathCombine(Path, name), IsAbsolute);
        }

        public DirectoryNode GetDirectory(string name)
        {
            return new DirectoryNode(Storage.PathCombine(Path, name), IsAbsolute);
        }

        public FileNode[] GetFiles()
        {
            return Storage.GetFiles(this);
        }

        public DirectoryNode[] GetDirectories()
        {
            return Storage.GetDirectories(this);
        }

        public FileNode NewFile(string filename)
        {
            var newnode = new FileNode(Storage.PathCombine(Path, filename), IsAbsolute);

            if (newnode.IsExist)
            {
                return null;
            }

            return Storage.CreateFile(newnode);
        }

        public DirectoryNode NewDirectory(string directoryName)
        {
            var newnode = new DirectoryNode(Storage.PathCombine(Path, directoryName), IsAbsolute);

            if (newnode.IsExist)
            {
                return null;
            }

            return Storage.CreateDirectory(newnode);
        }
    }
}
