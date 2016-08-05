using System;
using System.Diagnostics;
using System.Drawing;
using System.Ini;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FalloutLauncher
{
    class Program
    {
        const string IniFile = "FalloutLauncher.ini";
        const string LogFile = "FalloutLauncher.log";

        // Path constants for comparing to determine if paths has been changed from arguments
        const string DefaultPathFOSE = "fose_loader.exe";
        const string DefaultPathLauncher = "FalloutLauncher_ORG.exe";
        const string DefaultPathModOrganizer = @"Mod Organizer\ModOrganizer.exe";

        const string FlagFOSE = "--fose";
        const string FlagLauncher = "--launcher";
        const string FlagMO = "--mo";
        const string FlagStart = "--start";

        const string IniKeyArguments = "Arguments";
        const string IniKeyName = "Name";
        const string IniKeyPath = "Path";

        const string IniSectionLauncher = "Fallout Launcher";
        const string IniSectionFOSE = "FOSE";
        const string IniSectionMO = "Mod Organizer";
        const string IniSectionCustom = "Custom";

        static string ArgumentsCustom = string.Empty;
        static string ArgumentsFOSE = string.Empty;
        static string ArgumentsLauncher = string.Empty;
        static string ArgumentsModOrganizer = string.Empty;

        static string NameCustom = "Custom";

        static string PathCustom = string.Empty;
        static string PathFOSE = DefaultPathFOSE;
        static string PathLauncher = DefaultPathLauncher;
        static string PathModOrganizer = DefaultPathModOrganizer;

        static bool _customEnabled = true;
        static AutoStart _autoStart = AutoStart.None;
        static ConsoleKeyInfo _input;
        static StreamWriter _log;

        static string Version
        {
            get
            {
                return string.Format("{0}.{1}",
                    Assembly.GetEntryAssembly().GetName().Version.Major,
                    Assembly.GetEntryAssembly().GetName().Version.Minor);
            }
        }

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
            Console.Title = "FalloutLauncher " + Version;

            CenterConsole();

            _log = new StreamWriter(LogFile, true)
            {
                AutoFlush = true
            };

            _log.WriteLine("================ START ================");
            _log.WriteLine(DateTime.Now);
            _log.WriteLine("v" + Version);

            // Process INI first, as arguments has priority
            ProcessINI();

            if (!ProcessArguments(args))
                goto exit; // Exit if application fails to process arguments

            // Print after processing arguments
            _log.WriteLine("Fallout 3 Launcher");
            _log.WriteLine("    path: {0}", PathLauncher);
            _log.WriteLine("    arguments: {0}", ArgumentsLauncher);
            _log.WriteLine("FOSE");
            _log.WriteLine("    path: {0}", PathFOSE);
            _log.WriteLine("    arguments: {0}", ArgumentsFOSE);
            _log.WriteLine("Mod Organizer");
            _log.WriteLine("    path: {0}", PathModOrganizer);
            _log.WriteLine("    arguments: {0}", ArgumentsModOrganizer);
            _log.WriteLine("Custom");
            _log.WriteLine("    name: {0}", NameCustom);
            _log.WriteLine("    path: {0}", PathCustom);
            _log.WriteLine("    arguments: {0}", ArgumentsCustom);
            _log.WriteLine("-");

            _customEnabled = !string.IsNullOrEmpty(PathCustom);
            _log.WriteLine("Fallout3Launcher found: " + File.Exists(PathLauncher));
            _log.WriteLine("FOSE found: " + File.Exists(PathFOSE));
            _log.WriteLine("Mod Organizer found: " + File.Exists(PathModOrganizer));
            _log.WriteLine("custom option enabled: " + _customEnabled);

            if (_customEnabled)
                _log.WriteLine("custom option found: " + File.Exists(PathCustom));

            // Try to automatically find original launcher and Mod Organizer,
            // but don't if they have been changed, that means they was set with an argument.

            if (PathLauncher == DefaultPathLauncher)
            {
                PathLauncher = FindLauncher();

                if (PathLauncher != DefaultPathLauncher)
                {
                    _log.WriteLine("found Fallout3Launcher at: {0}", PathLauncher);
                }
            }

            if (PathModOrganizer == DefaultPathModOrganizer)
            {
                PathModOrganizer = FindModOrganizer();

                if (PathModOrganizer != DefaultPathModOrganizer)
                {
                    _log.WriteLine("found Fallout 3 Launcher at: {0}", PathLauncher);
                }
            }

            _log.WriteLine("-");

            switch (_autoStart)
            {
                case AutoStart.None:
                    ShowMainPage();
                    break;
                case AutoStart.Custom:
                    if (!_customEnabled)
                    {
                        WriteAndLogLine("{0} was selected, but the path is empty.", NameCustom);
                        Console.WriteLine();
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                    }
                    Start(NameCustom, PathCustom, ArgumentsCustom, true);
                    break;
                case AutoStart.FOSE:
                    Start("FOSE", PathFOSE, ArgumentsFOSE, true);
                    break;
                case AutoStart.Launcher:
                    Start("Fallout 3 Launcher", PathLauncher, ArgumentsLauncher, true);
                    break;
                case AutoStart.ModOrganizer:
                    Start("Mod Organizer", PathModOrganizer, ArgumentsModOrganizer, true);
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
        /// Creates an empty INI file for the user to configure.
        /// </summary>
        static void CreateEmptyIni(IniManager ini)
        {
            // Fallout Launcher
            ini.GetSection(IniSectionLauncher)
                .InsertComment("Leave empty to ignore")
                .Add(IniKeyPath, string.Empty)
                .Add(IniKeyArguments, string.Empty)
                .InsertEmptyLine();

            // FOSE
            ini.GetSection(IniSectionFOSE)
                .InsertComment("Leave empty to ignore")
                .Add(IniKeyPath, string.Empty)
                .Add(IniKeyArguments, string.Empty)
                .InsertEmptyLine();

            // Mod Organizer
            ini.GetSection(IniSectionMO)
                .InsertComment("Leave empty to ignore")
                .Add(IniKeyPath, string.Empty)
                .Add(IniKeyArguments, string.Empty)
                .InsertEmptyLine();

            // Custom
            ini.GetSection(IniSectionCustom)
                .InsertComment("Leave empty to ignore")
                .Add(IniKeyName, string.Empty)
                .Add(IniKeyPath, string.Empty)
                .Add(IniKeyArguments, string.Empty);

            ini.Save();
        }

        /// <summary>
        /// Processes the application arguments, setting the static variables.
        /// </summary>
        static bool ProcessArguments(string[] args)
        {
            if (args != null && args.Length > 0)
                _log.WriteLine("arguments: {0}", string.Join(" ", args));

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg.ToLower())
                {
                    case FlagFOSE:
                        i++;
                        PathFOSE = args[i];
                        break;
                    case FlagLauncher:
                        i++;
                        PathLauncher = args[i];
                        break;
                    case FlagMO:
                        i++;
                        PathModOrganizer = args[i];
                        break;
                    case FlagStart:
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
                            case "custom":
                                _autoStart = AutoStart.Custom;
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
        /// Processes the INI file, applying any changes.
        /// </summary>
        static void ProcessINI()
        {
            if (!File.Exists(IniFile))
                return;

            var ini = new IniManager(IniFile);

            ini.ReturnDefaultIfEmpty = true;

            // Create template INI if the file exists AND is empty
            if (string.IsNullOrEmpty(File.ReadAllText(IniFile)))
            {
                CreateEmptyIni(ini);
                return;
            }
            else
            {
                ini.Load();
            }

            if (ini.Contains(IniSectionLauncher))
            {
                // Fallout Launcher
                PathLauncher = ini.GetString(IniSectionLauncher, IniKeyPath, PathLauncher);
                ArgumentsLauncher = ini.GetString(IniSectionLauncher, IniKeyArguments, ArgumentsLauncher);
            }

            if (ini.Contains(IniSectionFOSE))
            {
                // FOSE
                PathFOSE = ini.GetString(IniSectionFOSE, IniKeyPath, PathFOSE);
                ArgumentsFOSE = ini.GetString(IniSectionFOSE, IniKeyArguments, ArgumentsFOSE);
            }

            if (ini.Contains(IniSectionMO))
            {
                // Mod Organizer
                PathModOrganizer = ini.GetString(IniSectionMO, IniKeyPath, PathModOrganizer);
                ArgumentsModOrganizer = ini.GetString(IniSectionMO, IniKeyArguments, ArgumentsModOrganizer);
            }

            if (ini.Contains(IniSectionCustom))
            {
                // Custom
                NameCustom = ini.GetString(IniSectionCustom, IniKeyName, NameCustom);
                PathCustom = ini.GetString(IniSectionCustom, IniKeyPath, PathCustom);
                ArgumentsCustom = ini.GetString(IniSectionCustom, IniKeyArguments, ArgumentsCustom);
            }
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

            if (_customEnabled)
                Console.WriteLine("4:   {0}", NameCustom);

            Console.WriteLine();
            Console.WriteLine("Esc: Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            if (_input.KeyChar == '\0')
                _input = Console.ReadKey();

            _log.WriteLine("input character: " + _input.Key.ToString());

            Console.Clear();

            switch (_input.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    Start("Fallout 3 Launcher", PathLauncher, ArgumentsLauncher, true);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Start("FOSE", PathFOSE, ArgumentsFOSE, true);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    Start("Mod Organizer", PathModOrganizer, ArgumentsModOrganizer, true);
                    break;
                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    if (!_customEnabled)
                        goto default;

                    Start(NameCustom, PathCustom, ArgumentsCustom, true);
                    break;
                case ConsoleKey.Escape:
                    _log.WriteLine("exiting...");
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
        static void Start(string name, string path, string arguments, bool quiet)
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
                        _log.WriteLine("attempting to start {0}...", name);
                    else
                        WriteAndLogLine("Attempting to start {0}...", name);

                    var psi = new ProcessStartInfo(path);

                    if (!string.IsNullOrEmpty(arguments))
                        psi.Arguments = arguments;

                    Process.Start(psi);

                    if (quiet)
                        _log.WriteLine("successful! Now exiting...");
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

            foreach (var file in directory.GetFiles("*FalloutLauncher*.exe", SearchOption.TopDirectoryOnly))
            {
                // Ignore files under 1 MB
                if (file.Length >= 1000000)
                    return file.FullName;
            }

            return DefaultPathLauncher;
        }

        /// <summary>
        /// Attempts to find Mod Organizer using a mix of common places and search pattern.
        /// </summary>
        static string FindModOrganizer()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            var foundDirs = directory.GetDirectories("*Mod*Organizer*", SearchOption.TopDirectoryOnly);

            foreach (var dir in foundDirs)
            {
                var foundExes = dir.GetFiles("*Mod*Organizer*.exe", SearchOption.TopDirectoryOnly);

                if (foundExes.Length > 0)
                    return foundExes[0].FullName;
            }

            return DefaultPathModOrganizer;
        }
    }

    enum AutoStart
    {
        None,
        Custom,
        FOSE,
        Launcher,
        ModOrganizer
    }
}
