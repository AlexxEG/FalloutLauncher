using System;
using System.Diagnostics;
using System.IO;

namespace FalloutLauncher
{
    class Program
    {
        private const string LOG_FILE = "FalloutLauncher.log";
        private const string VERSION = "1.2";

        private const string FLAG_FOSE = "--fose";
        private const string FLAG_LAUNCHER = "--launcher";
        private const string FLAG_MO = "--mo";
        private const string FLAG_START = "--start";

        private static string FOSE_PATH = "fose_loader.exe";
        private static string LAUNCHER_PATH = "FalloutLauncher_ORG.exe";
        private static string MOD_ORGANIZER_PATH = @"Mod Organizer\ModOrganizer.exe";

        static ConsoleKeyInfo _input;
        static StreamWriter _log;

        static void Main(string[] args)
        {
            Console.Title = "FalloutLauncher " + VERSION;

            _log = new StreamWriter(LOG_FILE, true)
            {
                AutoFlush = true
            };

            _log.WriteLine("=================START=================");
            _log.WriteLine(DateTime.Now);
            _log.WriteLine("Steam FalloutLauncher ({0})", VERSION);

            if (!ProcessArguments(args))
                goto exit;

            _log.WriteLine();
            _log.WriteLine("Fallout 3 Launcher path: \"{0}\"", LAUNCHER_PATH);
            _log.WriteLine("FOSE path: \"{0}\"", FOSE_PATH);
            _log.WriteLine("Mod Organizer path: \"{0}\"", MOD_ORGANIZER_PATH);
            _log.WriteLine();

            Console.WriteLine("1:   Fallout 3 Launcher");
            Console.WriteLine("2:   FOSE");
            Console.WriteLine("3:   Mod Organizer");
            Console.WriteLine("Esc: Exit");
            Console.WriteLine();
            Console.Write("What do you want to start: ");

            if (_input.KeyChar == '\0')
                _input = Console.ReadKey();

            _log.WriteLine("Input character: " + _input.KeyChar);

            Console.Clear();

            switch (_input.Key)
            {
                case ConsoleKey.D1:
                    Start("Fallout 3 Launcher", LAUNCHER_PATH);
                    break;
                case ConsoleKey.D2:
                    Start("FOSE", FOSE_PATH);
                    break;
                case ConsoleKey.D3:
                    Start("Mod Organizer", MOD_ORGANIZER_PATH);
                    break;
                case ConsoleKey.Escape:
                    WriteAndLogLine("Existing...");
                    break;
                default:
                    WriteAndLogLine("Unrecognized input: {{{0}}}", _input.Key);
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    break;
            }

        exit:

            _log.WriteLine("==================END==================");
            _log.WriteLine();

            _log.Flush();
            _log.Close();
        }

        static bool ProcessArguments(string[] args)
        {
            if (args != null && args.Length > 0)
                _log.WriteLine("Arguments: {0}", string.Join(" ", args));

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg.ToLower())
                {
                    case FLAG_FOSE:
                        i++;
                        FOSE_PATH = args[i];
                        break;
                    case FLAG_LAUNCHER:
                        i++;
                        LAUNCHER_PATH = args[i];
                        break;
                    case FLAG_MO:
                        i++;
                        MOD_ORGANIZER_PATH = args[i];
                        break;
                    case FLAG_START:
                        i++;
                        switch (args[i].ToLower())
                        {
                            case "launcher":
                                _input = new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false);
                                break;
                            case "fose":
                                _input = new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false);
                                break;
                            case "mo":
                                _input = new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false);
                                break;
                        }
                        break;
                    default:
                        // Handle unrecognized flag
                        WriteAndLogLine("Unrecognized flag: " + arg);
                        Console.ReadKey();
                        return false;
                }
            }

            return true;
        }

        static void Start(string name, string path)
        {
            if (!File.Exists(path))
            {
                WriteAndLogLine("Couldn't find {0}, press any key to exit...", name);
                Console.ReadKey();
            }
            else
            {
                try
                {
                    WriteAndLogLine("Attempting to start {0}...", name);
                    Process.Start(path);
                    WriteAndLogLine("Successful! Now exiting...");
                }
                catch (Exception ex)
                {
                    WriteAndLogLine("Error starting {0}:", name);
                    WriteAndLogLine(ex);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
        }

        static void WriteAndLogLine(string message, params object[] args)
        {
            _log.WriteLine(message, args);
            Console.WriteLine(message, args);
        }

        static void WriteAndLogLine(object value)
        {
            _log.WriteLine(value);
            Console.WriteLine(value);
        }
    }
}
