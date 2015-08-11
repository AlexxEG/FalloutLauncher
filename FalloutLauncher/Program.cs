﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FalloutLauncher
{
    class Program
    {
        const string LOG_FILE = "FalloutLauncher.log";
        const string VERSION = "1.2";

        const string FLAG_FOSE = "--fose";
        const string FLAG_LAUNCHER = "--launcher";
        const string FLAG_MO = "--mo";
        const string FLAG_START = "--start";

        // Path constants for comparing to determine if paths has been changed from arguments
        const string DEFAULT_PATH_FOSE = "fose_loader.exe";
        const string DEFAULT_PATH_LAUNCHER = "FalloutLauncher_ORG.exe";
        const string DEFAULT_PATH_MOD_ORGANIZER = @"Mod Organizer\ModOrganizer.exe";

        static string FOSE_PATH = DEFAULT_PATH_FOSE;
        static string LAUNCHER_PATH = DEFAULT_PATH_LAUNCHER;
        static string MOD_ORGANIZER_PATH = DEFAULT_PATH_MOD_ORGANIZER;

        static AutoStart _autoStart = AutoStart.None;
        static ConsoleKeyInfo _input;
        static StreamWriter _log;

        #region Console Window Position & Size

        // This region contains methods for setting console window's size and position.

        const int SWP_NOSIZE = 0x0001;

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        private static IntPtr MyConsole = GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        #endregion

        static void Main(string[] args)
        {
            Console.Title = "FalloutLauncher " + VERSION;

            CenterConsole();

            _log = new StreamWriter(LOG_FILE, true)
            {
                AutoFlush = true
            };

            _log.WriteLine("================ START ================");
            _log.WriteLine(DateTime.Now);
            _log.WriteLine("Steam FalloutLauncher ({0})", VERSION);

            if (!ProcessArguments(args))
                goto exit; // Exit if application fails to process arguments

            // Print after processing arguments
            _log.WriteLine();
            _log.WriteLine("Fallout 3 Launcher path: \"{0}\"", LAUNCHER_PATH);
            _log.WriteLine("FOSE path: \"{0}\"", FOSE_PATH);
            _log.WriteLine("Mod Organizer path: \"{0}\"", MOD_ORGANIZER_PATH);
            _log.WriteLine();

            // Try to automatically find original launcher and Mod Organizer,
            // but don't if they have been changed, that means they was set with an argument.

            if (LAUNCHER_PATH == DEFAULT_PATH_LAUNCHER)
                LAUNCHER_PATH = FindLauncher();

            if (MOD_ORGANIZER_PATH == DEFAULT_PATH_MOD_ORGANIZER)
                MOD_ORGANIZER_PATH = FindModOrganizer();

            switch (_autoStart)
            {
                case AutoStart.None:
                    ShowMainPage();
                    break;
                case AutoStart.FOSE:
                    Start("FOSE", FOSE_PATH, true);
                    break;
                case AutoStart.Launcher:
                    Start("Fallout 3 Launcher", LAUNCHER_PATH, true);
                    break;
                case AutoStart.ModOrganizer:
                    Start("Mod Organizer", MOD_ORGANIZER_PATH, true);
                    break;
            }

        exit:

            _log.WriteLine("================= END =================");
            _log.WriteLine();

            _log.Flush();
            _log.Close();
        }

        /// <summary>
        /// Centers the console window in the current Screen.
        /// </summary>
        static void CenterConsole()
        {
            // Get screen rectangle of current screen
            var screenRect = Screen.FromPoint(new Point(Console.WindowLeft, Console.WindowTop)).WorkingArea;

            // Get window rect of the console window
            Rect windowRect = new Rect();
            GetWindowRect(MyConsole, ref windowRect);

            SetWindowPos(MyConsole,
                0,
                (screenRect.Width / 2) - ((windowRect.Right - windowRect.Left + 1) / 2),
                (screenRect.Height / 2) - ((windowRect.Bottom - windowRect.Top + 1) / 2),
                0,
                0,
                SWP_NOSIZE);
        }

        /// <summary>
        /// Processes the application arguments, setting the static variables.
        /// </summary>
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
                                _autoStart = AutoStart.Launcher;
                                break;
                            case "fose":
                                _autoStart = AutoStart.FOSE;
                                break;
                            case "mo":
                                _autoStart = AutoStart.ModOrganizer;
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

        /// <summary>
        /// Shows the main page after clearing.
        /// </summary>
        static void ShowMainPage()
        {
            Console.Clear();

            Console.WriteLine("1:   Fallout 3 Launcher");
            Console.WriteLine("2:   FOSE");
            Console.WriteLine("3:   Mod Organizer");
            Console.WriteLine();
            Console.WriteLine("Esc: Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            if (_input.KeyChar == '\0')
                _input = Console.ReadKey();

            _log.WriteLine("Input character: " + _input.KeyChar);

            Console.Clear();

            switch (_input.Key)
            {
                case ConsoleKey.D1:
                    Start("Fallout 3 Launcher", LAUNCHER_PATH, true);
                    break;
                case ConsoleKey.D2:
                    Start("FOSE", FOSE_PATH, true);
                    break;
                case ConsoleKey.D3:
                    Start("Mod Organizer", MOD_ORGANIZER_PATH, true);
                    break;
                case ConsoleKey.Escape:
                    WriteAndLogLine("Exiting...");
                    break;
                default:
                    WriteAndLogLine("Unrecognized input: {{{0}}}", _input.Key);
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();

                    // Reset input so it wont auto start when going back
                    _input = new ConsoleKeyInfo();

                    ShowMainPage();
                    break;
            }
        }

        /// <summary>
        /// Starts an executable from the <paramref name="path"/> variable.
        /// <paramref name="name"/> variable is used only in the console to inform the user of progress.
        /// <param name="quiet">Set to true to quietly start process, unless there is an error.</param>
        /// </summary>
        static void Start(string name, string path, bool quiet)
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
                    if (quiet)
                        _log.WriteLine("Attempting to start {0}...", name);
                    else
                        WriteAndLogLine("Attempting to start {0}...", name);

                    Process.Start(path);

                    if (quiet)
                        _log.WriteLine("Successful! Now exiting...");
                    else
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

        /// <summary>
        /// Writes message to both log and console window.
        /// </summary>
        static void WriteAndLogLine(string message, params object[] args)
        {
            _log.WriteLine(message, args);
            Console.WriteLine(message, args);
        }

        /// <summary>
        /// Writes message to both log and console window.
        /// </summary>
        static void WriteAndLogLine(object value)
        {
            _log.WriteLine(value);
            Console.WriteLine(value);
        }

        /// <summary>
        /// Attempts to find normal Fallout Launcher using a mix of common places and search pattern.
        /// </summary>
        static string FindLauncher()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            //System.AppDomain.CurrentDomain.FriendlyName.ToString();

            foreach (var file in directory.GetFiles("*Launcher*.exe", SearchOption.TopDirectoryOnly))
            {
                // Ignore files under 1 MB
                if (file.Length >= 1000000)
                    return file.FullName;
            }

            return DEFAULT_PATH_LAUNCHER;
        }

        /// <summary>
        /// Attempts to find Mod Organizer using a mix of common places and search pattern.
        /// </summary>
        static string FindModOrganizer()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            var foundDirs = directory.GetDirectories("Mod*Organizer", SearchOption.TopDirectoryOnly);

            foreach (var dir in foundDirs)
            {
                var foundExes = dir.GetFiles("Mod*Organizer.exe", SearchOption.TopDirectoryOnly);

                if (foundExes.Length > 0)
                    return foundExes[0].FullName;
            }

            return DEFAULT_PATH_MOD_ORGANIZER;
        }
    }

    enum AutoStart
    {
        None,
        FOSE,
        Launcher,
        ModOrganizer
    }
}
