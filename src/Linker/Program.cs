using System;
using System.Collections.Generic;
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
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        //x [DllImport("kernel32.dll", SetLastError = true)]
        //x static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        static int Main(string[] args)
        {
            var assembly = typeof(Program).Assembly;
            Console.WriteLine("linker v{0}.{1} (c) Steve Hansen 2015", assembly.GetName().Version.Major, assembly.GetName().Version.Minor);
            if (args.Length == 0)
            {
                var self = Path.GetFileName(assembly.Location);
                Console.WriteLine(" Usage: {0} \"full file path\"", self);
                Console.WriteLine("    or: {0} --dir C:\\Code\\", self);
                Console.WriteLine("    or: {0} --dirs DIR-1 DIR-2 DIR-N", self);
                return 1;
            }

            var existingPath = args[0];
            if (existingPath == "/dirs" || existingPath == "--dirs")
                return HandleDirs(args.Skip(1));

            var isDirArg = existingPath == "/dir" || existingPath == "--dir";
            if (isDirArg || Directory.Exists(existingPath))
                return HandlePackageDir(args, isDirArg, existingPath);

            return HandleFile(existingPath);
        }

        private static int HandleDirs(IEnumerable<string> args)
        {
            var dirs = args.Select(arg => new DirectoryInfo(arg)).ToArray();
            var missingDirs = dirs.Where(d => !d.Exists).ToArray();
            if (missingDirs.Length > 0)
            {
                Console.Error.WriteLine("Directories don't exist: {0}", string.Join<DirectoryInfo>(", ", missingDirs));
                return 2;
            }

            UnduplicateDirectories(dirs);

            return 0;
        }

        private static int HandlePackageDir(string[] args, bool isDirArg, string existingPath)
        {
            var dir = new DirectoryInfo(isDirArg ? (args.Length >= 2 ? args[1] : Environment.CurrentDirectory) : existingPath);
            if (!dir.Exists)
            {
                Console.Error.WriteLine("Directory doesn't exist.");
                return 2;
            }

            var packageDirs = dir.GetDirectories()
                .Where(d => d.Name != "System Volume Information" && d.Name != "$RECYCLE.BIN")
                .SelectMany(d => d.GetDirectories("packages", SearchOption.AllDirectories))
                .ToArray();
            Console.WriteLine(" Found {0} packages directories.", packageDirs.Length);

            UnduplicateDirectories(packageDirs);

            return 0;
        }

        private static void UnduplicateDirectories(DirectoryInfo[] dirs)
        {
            const long maxSize = 1024 * 1024 * 10;
            var fileGroups = dirs
                .SelectMany(d => d.GetFiles("*.*", SearchOption.AllDirectories))
                .Where(f => f.Name != "repositories.config" && f.Length < maxSize)
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
            Console.WriteLine("  Found {0} duplicate files (based on file size and md5).", fileGroups.Length);
            var errors = new List<string>();
            for (var i = 0; i < fileGroups.Length; i++)
            {
                Console.Write("  {0} / {1}", i, fileGroups.Length);
                Console.CursorLeft = 0;

                var fileGroup = fileGroups[i];
                var firstFile = fileGroup[0].FullName;
                foreach (var otherFile in fileGroup.Skip(1))
                {
                    try
                    {
                        otherFile.Delete();
                        CreateHardLink(otherFile.FullName, firstFile, IntPtr.Zero);
                    }
                    catch (Exception e)
                    {
                        errors.Add(string.Format("{0} => {1} ({2})", otherFile.FullName, e.Message, e.GetType().FullName));
                    }
                }
            }
            Console.WriteLine("  {0} / {0}", fileGroups.Length);

            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Got {0} errors:", errors.Count);
                foreach (var error in errors)
                {
                    Console.Write("  ");
                    Console.WriteLine(error);
                }
                Console.ResetColor();
            }
        }

        private static int HandleFile(string existingPath)
        {
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