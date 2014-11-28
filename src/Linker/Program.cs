using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Linker
{
    class Program
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        static int Main(string[] args)
        {
            Console.WriteLine("linker (c) Steve Hansen");
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: linker.exe \"full path\"");
                return 1;
            }

            var existingPath = args[0];
            if (!File.Exists(existingPath))
            {
                Console.Error.WriteLine("File doesn't exist.");
                return 2;
            }

            var existingFileName = Path.GetFileName(existingPath);
            if (existingFileName == null)
            {
                Console.Error.WriteLine("Not a file.");
                return 2;
            }

            var path = Path.Combine(Path.GetPathRoot(existingPath) ?? ".", "_New_", existingFileName);
            CreateHardLink(path, existingPath, IntPtr.Zero);

            return 0;
        }
    }
}