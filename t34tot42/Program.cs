using System;
using System.Collections.Generic;
using System.IO;
using TeletextSharedResources;

namespace T34ToT42
{
    /// <summary>
    /// Command-line front end for T34Converter.
    ///
    ///   t34tot42 capture.t34                 -> writes capture.t42 next to it
    ///   t34tot42 a.t34 b.t34 ...             -> converts each file
    ///   t34tot42 folder                      -> converts every .t34 in the folder
    ///   t34tot42 capture.t34 -o out.t42      -> chooses the output name (one input only)
    ///   --force                              -> overwrite existing .t42 files
    ///
    /// On Windows, dragging .t34 files onto the EXE file will also work.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Supplied files and folders
            var inputs = new List<string>();
            string outputPath = null;
            bool force = false;

            // ~ Argument loop
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // ^ "Help" command
                if (arg == "-h" || arg == "--help" || arg == "/?")
                {
                    PrintUsage();
                    return 0;
                }

                // ^ "Version" command
                if (arg == "-v" || arg == "--version")
                {
                    Console.WriteLine("t34tot42 " + typeof(Program).Assembly.GetName().Version.ToString(3));
                    return 0;
                }

                // ^ "Force" overwrite command
                if (arg == "-f" || arg == "--force")
                {
                    force = true;
                }

                // ^ "Output" command
                else if (arg == "-o" || arg == "--output")
                {
                    // If no filename is provided, throw an error
                    if (i + 1 >= args.Length)
                        return Fail("-o needs a file name after it.");
                    outputPath = args[++i];
                }

                // ^ Check for unknown command options
                else if (arg.StartsWith("-") && !File.Exists(arg) && !Directory.Exists(arg))
                {
                    return Fail($"Unknown option: {arg}  (use --help to see the options)");
                }

                // ^ Argument is an input otherwise
                else
                {
                    inputs.Add(arg);
                }
            }

            // Make sure at least one input file was provided
            if (inputs.Count == 0)
            {
                PrintUsage();
                PauseIfLaunchedByDoubleClick();
                return 1;
            }

            // Create file list
            var files = new List<string>();
            foreach (string input in inputs)
            {
                // If using a directory
                if (Directory.Exists(input))
                {
                    // The file extension is case-insensitive, so if the file extension happens to be uppercase (.T34), it can also be found on macOS and Linux
                    // * This will likely never be the case, but it doesn't hurt to add a failsafe
                    var options = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive };

                    // Find all T34 files, then sort
                    string[] found = Directory.GetFiles(input, "*.t34", options);
                    Array.Sort(found, StringComparer.OrdinalIgnoreCase);

                    // If no files are found in the directory
                    if (found.Length == 0)
                        Console.WriteLine($"No .t34 files in folder: {input}");
                    files.AddRange(found);
                }

                // If input file exists, add it
                else if (File.Exists(input))
                {
                    files.Add(input);
                }

                // If the path doesn't exist
                else
                {
                    Console.Error.WriteLine($"Not found: {input}");
                }
            }

            // Check if the output command was used correctly
            if (outputPath != null && files.Count != 1)
                return Fail("-o can only be used with a single input file.");

            // Success/fail counters
            int converted = 0, failed = 0;

            // Convert every provided file
            foreach (string file in files)
            {
                string target = outputPath ?? Path.ChangeExtension(file, ".t42");
                if (ConvertFile(file, target, force)) converted++;
                else failed++;
            }

            // Display results if more than 1 file is provided
            if (files.Count > 1)
                Console.WriteLine($"\nDone: {converted} converted, {failed} failed.");

            PauseIfLaunchedByDoubleClick();
            return failed > 0 || files.Count == 0 ? 1 : 0;
        }

        // & Conversion block
        private static bool ConvertFile(string inputPath, string outputPath, bool force)
        {
            try
            {
                // Print error if input and output files are the same
                if (Path.GetFullPath(inputPath) == Path.GetFullPath(outputPath))
                {
                    Console.Error.WriteLine($"Skipped {inputPath}: output would overwrite the input.");
                    return false;
                }

                // If output file already exists, skip it and don't override it
                if (File.Exists(outputPath) && !force)
                {
                    Console.Error.WriteLine($"Skipped {inputPath}: {outputPath} already exists (use --force to overwrite).");
                    return false;
                }

                // Read the T34 file
                byte[] t34Data = File.ReadAllBytes(inputPath);

                // Check packet size
                if (t34Data.Length == 0 || t34Data.Length % T34Converter.T34PacketSize != 0)
                {
                    // If the file size is an exact multiple of the T42 packet size but not of the T34 packet size, print an error
                    if (t34Data.Length % T34Converter.T42PacketSize == 0 && t34Data.Length % T34Converter.T34PacketSize != 0)
                    {
                        Console.Error.WriteLine($"Skipped {inputPath}: this looks like a 42-byte (.t42) file, not a .t34.");
                        return false;
                    }
                    // Warn about partial packet
                    Console.Error.WriteLine($"Warning: {inputPath} is not a whole number of 34-byte packets; the last partial packet is ignored.");
                }

                // Convert the T34 to T42
                byte[] t42Data = T34Converter.ToT42(t34Data);

                // If no teletext pages were found, print an error
                if (t42Data.Length == 0)
                {
                    Console.Error.WriteLine($"Skipped {inputPath}: no teletext pages found. Is this a 525-line WST (.t34) capture?");
                    return false;
                }

                // Write the T42 and print the result
                File.WriteAllBytes(outputPath, t42Data);
                Console.WriteLine($"{inputPath} -> {outputPath}  " +
                                  $"({t34Data.Length / T34Converter.T34PacketSize:N0} packets in, " +
                                  $"{t42Data.Length / T34Converter.T42PacketSize:N0} out)");
                return true;
            }
            // Exception handling
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed {inputPath}: {e.Message}");
                return false;
            }
        }

        // & Helper function for failures
        private static int Fail(string message)
        {
            Console.Error.WriteLine(message);
            return 2;
        }

        // Print help text to the terminal
        private static void PrintUsage()
        {
            Console.WriteLine(
@"t34tot42 - convert NTSC (525-line) World System Teletext .t34 captures to .t42

Usage:
  t34tot42 capture.t34              writes capture.t42 next to the input
  t34tot42 a.t34 b.t34 ...          converts several files
  t34tot42 folder                   converts every .t34 in a folder
  t34tot42 capture.t34 -o out.t42   chooses the output file name

Options:
  -o, --output FILE   output file (only with a single input)
  -f, --force         overwrite existing .t42 files
  -v, --version       show the version
  -h, --help          show this help

Columns 32-39 of each row, carried in separate row-extension packets on
525-line services, are merged back in, producing standard 40-column pages.");
        }

        /*
        * When someone double-clicks the EXE or drags files into it, the terminal window will close when the program ends.
        * Keep the console window open on Windows so the user can read the printed output.
        * If running from the terminal, this will not be needed.
       */
        private static void PauseIfLaunchedByDoubleClick()
        {
            if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected)
                return;
            try
            {
                // A console opened just for this program has only this process attached
                if (GetConsoleProcessList(new uint[2], 2) <= 1)
                {
                    Console.WriteLine("\nPress Enter to close.");
                    Console.ReadLine();
                }
            }
            catch
            {
                // Process best effort only.
            }
        }

        // Windows API declaration
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}