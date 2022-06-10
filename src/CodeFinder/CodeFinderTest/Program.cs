using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CodeFinderTest
{
    class Program
    {
        static string path = "";
        static int findMatchesVersion = 2;

        static void Main(string[] args)
        {
            var printHelp = true;

            ParseArgs(args);

            // Run tests using the insights from the given file (or files if given a directory).
            if (string.IsNullOrEmpty(path) == false)
            {
                var files = new List<string>();

                printHelp = false;

                if (Directory.Exists(path))
                {
                    files.AddRange(Directory.GetFiles(path));
                }
                else if (File.Exists(path))
                {
                    files.Add(path);
                }

                if (files.Count() == 0)
                {
                    Console.Error.WriteLine("No files found.");
                }
                else
                {
                    var insightTest = new InsightTest(findMatchesVersion);
                    foreach (var file in files)
                    {
                        insightTest.RunTest(file);
                    }
                }
            }

            if (printHelp)
            {
                Console.WriteLine("CodeFinderTest - Tests the CodeFinder class.");
                Console.WriteLine("Usage:");
                Console.WriteLine("\tCodeFinder [-v1|-v2] <Path>");
                Console.WriteLine("\t\tFinds matches based on the insights for file(s) found in the given <Path>.");
                Console.WriteLine("\t\t[-v1|-v2] specifies the version of FindMatches to use. If not specified, v2 is used.");
                Console.WriteLine("\t\t<Path> is the path to a file or directory within a git repo.");
            }

            // If we're debugging, break so that we can inspect the results before the console window closes.
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        static void ParseArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-v"))
                {
                    if (arg == "-v1")
                    {
                        findMatchesVersion = 1;
                    }
                    else if (arg == "-v2")
                    {
                        findMatchesVersion = 2;
                    }
                }
                else
                {
                    path = arg;
                }
            }
        }
    }
}
