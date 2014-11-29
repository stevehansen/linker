using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Linker
{
    class Program
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        static int Main(string[] args)
        {
            Console.WriteLine("linker v1.1 (c) Steve Hansen 2014");
            if (args.Length == 0)
            {
                var self = Path.GetFileName(typeof(Program).Assembly.Location);
                Console.WriteLine(" Usage: {0} \"full file path\"", self);
                Console.WriteLine("    or: {0} --dir C:\\Code\\", self);
                return 1;
            }

            var existingPath = args[0];
            if (existingPath == "/dir" || existingPath == "--dir")
            {
                var dir = new DirectoryInfo(args.Length >= 2 ? args[1] : Environment.CurrentDirectory);
                if (!dir.Exists)
                {
                    Console.Error.WriteLine("Directory doesn't exist.");
                    return 2;
                }

                var packageDirs = dir.GetDirectories()
                    .Where(d => d.Name != "System Volume Information")
                    .SelectMany(d => d.GetDirectories("packages", SearchOption.AllDirectories))
                    .ToArray();
                Console.WriteLine(" Found {0} packages directories.", packageDirs.Length);

                var fileGroups = packageDirs
                    .SelectMany(d => d.GetFiles("*.*", SearchOption.AllDirectories))
                    .Where(f => f.Name != "repositories.config")
                    .GroupBy(f => Tuple.Create(f.Name, f.Length))
                    .Where(g => g.Count() > 1)
                    .SelectMany(g =>
                    {
                        return g.GroupBy(file =>
                        {
                            using (var stream = file.OpenRead())
                            using (var md5 = MD5.Create())
                                return Convert.ToBase64String(md5.ComputeHash(stream));
                        });
                    })
                    .Select(g => g.ToArray())
                    .Where(g => g.Length > 1)
                    .ToArray();
                foreach (var fileGroup in fileGroups)
                {
                    var firstFile = fileGroup[0].FullName;
                    foreach (var otherFile in fileGroup.Skip(1))
                    {
                        otherFile.Delete();
                        CreateHardLink(otherFile.FullName, firstFile, IntPtr.Zero);
                    }
                }

                return 2;
            }

            if (!File.Exists(existingPath))
            {
                Console.Error.WriteLine("File doesn't exist.");
                return 2;
            }

            // NOTE: Build target path based on configuration
            var fileName = ConfigurationManager.AppSettings["PathFormat"] ?? ":\\_New_\\%FILENAME%";

            // Use same root
            if (fileName.StartsWith(":\\"))
                fileName = Path.GetPathRoot(existingPath) + fileName.Substring(2);
            // Replace filename
            fileName = fileName.Replace("%FILENAME%", Path.GetFileName(existingPath));
            // Replace full path
            fileName = fileName.Replace("%FULLPATH%", existingPath.Substring(3).Replace("\\", " - "));

            CreateHardLink(fileName, existingPath, IntPtr.Zero);
            Console.WriteLine(" Linked '{0}' to '{1}'", existingPath, fileName);

            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    // ReSharper disable once InconsistentNaming
    struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public FILETIME CreationTime;
        public FILETIME LastAccessTime;
        public FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}