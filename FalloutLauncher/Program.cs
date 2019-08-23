using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Ini;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FalloutLauncher
{
    enum AutoStart
    {
        None,
        Custom,
        FOSE,
        Launcher,
        MO
    }

    static class ExtensionMethods
    {
        public static string ToYesNo(this bool value)
        {
            return value ? "Yes" : "No";
        }
    }

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

        static bool Quiet = true;

        static string ArgumentsCustom = string.Empty;
        static string ArgumentsFOSE = string.Empty;
        static string ArgumentsLauncher = string.Empty;
        static string ArgumentsModOrganizer = string.Empty;

        static string NameCustom = "Custom";

        static string PathCustom = string.Empty;

        static bool _customEnabled = true;
        static ConsoleKeyInfo _input;
        static Logger Logger = new Logger(LogFile);

        static AutoStart AutoStart
        {
            get { return (AutoStart)arguments[FlagStart]; }
        }

        static string PathFOSE
        {
            get { return arguments[FlagFOSE].ToString(); }
            set { arguments[FlagFOSE] = value; }
        }
        static string PathLauncher
        {
            get { return arguments[FlagLauncher].ToString(); }
            set { arguments[FlagLauncher] = value; }
        }
        static string PathModOrganizer
        {
            get { return arguments[FlagMO].ToString(); }
            set { arguments[FlagMO] = value; }
        }

        static Dictionary<string, object> arguments = new Dictionary<string, object>()
        {
            { FlagFOSE, DefaultPathFOSE },
            { FlagLauncher, DefaultPathLauncher },
            { FlagMO, DefaultPathModOrganizer },
            { FlagStart, AutoStart.None }
        };

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

        static void WriteLogHeader()
        {
            Logger.LogLine("================ START ================", noPrefix: true);
            Logger.LogLine($"FalloutLauncher v{Version}");
        }

        static void WriteLogArguments()
        {
            // Print after processing arguments
            Logger.LogLine("[Fallout 3 Launcher]");
            Logger.LogLine("    Path: {0}", PathLauncher);
            Logger.LogLine("    Arguments: {0}", ArgumentsLauncher);
            Logger.LogLine("[FOSE]");
            Logger.LogLine("    Path: {0}", PathFOSE);
            Logger.LogLine("    Arguments: {0}", ArgumentsFOSE);
            Logger.LogLine("[Mod Organizer]");
            Logger.LogLine("    Path: {0}", PathModOrganizer);
            Logger.LogLine("    Arguments: {0}", ArgumentsModOrganizer);
            Logger.LogLine("[Custom]");
            Logger.LogLine("    Name: {0}", NameCustom);
            Logger.LogLine("    Path: {0}", PathCustom);
            Logger.LogLine("    Arguments: {0}", ArgumentsCustom);

            Logger.LogLine("Found Fallout3Launcher: " + File.Exists(PathLauncher).ToYesNo());
            Logger.LogLine("Found FOSE: " + File.Exists(PathFOSE).ToYesNo());
            Logger.LogLine("Found Mod Organizer: " + File.Exists(PathModOrganizer).ToYesNo());
            Logger.LogLine("Custom option enabled: " + _customEnabled.ToYesNo());

            if (_customEnabled)
                Logger.LogLine("Found custom option: " + File.Exists(PathCustom));
        }

        static void Main(string[] args)
        {
            // Setup console
            Console.Title = "FalloutLauncher " + Version;
            CenterConsole();

            // Write log header
            WriteLogHeader();

            // Process INI first, as arguments has priority
            if (File.Exists(IniFile))
            {
                if (!string.IsNullOrEmpty(File.ReadAllText(IniFile)))
                {
                    ProcessINI();
                }
                else
                {
                    // Empty INI found, create template INI
                    CreateEmptyIni();
                    Logger.WriteAndLogLine($"Created INI template at {IniFile}");
                    goto exit;
                }
            }

            try
            {
                ProcessArguments(args);
            }
            catch (ArgumentException ex)
            {
                Logger.WriteAndLogLine(ex.Message);
                goto exit; // Exit if application fails to process arguments
            }

            _customEnabled = !string.IsNullOrEmpty(PathCustom);

            WriteLogArguments();

            // Try to automatically find original launcher and Mod Organizer,
            // but don't if they have been changed, that means they was set with an argument.

            if (PathLauncher == DefaultPathLauncher)
            {
                PathLauncher = FindLauncher();

                if (PathLauncher != DefaultPathLauncher)
                {
                    Logger.LogLine("Found Fallout3Launcher at: {0}", PathLauncher);
                }
            }

            if (PathModOrganizer == DefaultPathModOrganizer)
            {
                PathModOrganizer = FindModOrganizer();

                if (PathModOrganizer != DefaultPathModOrganizer)
                {
                    Logger.LogLine("Found Fallout 3 Launcher at: {0}", PathLauncher);
                }
            }

            switch (AutoStart)
            {
                case AutoStart.None:
                    ShowMainPage();
                    break;
                case AutoStart.Custom:
                    if (!_customEnabled)
                    {
                        Logger.WriteAndLogLine("{0} was selected, but the path is empty.", NameCustom);
                        Console.WriteLine();
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                    }
                    Start(NameCustom, PathCustom, ArgumentsCustom);
                    break;
                case AutoStart.FOSE:
                    Start("FOSE", PathFOSE, ArgumentsFOSE);
                    break;
                case AutoStart.Launcher:
                    Start("Fallout 3 Launcher", PathLauncher, ArgumentsLauncher);
                    break;
                case AutoStart.MO:
                    Start("Mod Organizer", PathModOrganizer, ArgumentsModOrganizer);
                    break;
            }

        exit:
            Logger.LogLine();
            Logger.Close();
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
        static void CreateEmptyIni()
        {
            var ini = new IniManager(IniFile)
            {
                ReturnDefaultIfEmpty = true
            };

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
        static void ProcessArguments(string[] args)
        {
            if (args != null && args.Length > 0)
                Logger.LogLine("Arguments: {0}", string.Join(", ", args));

            for (int i = 0; i < args.Length; i++)
            {
                string flag = args[i];

                switch (flag.ToLower())
                {
                    case FlagFOSE:
                    case FlagLauncher:
                    case FlagMO:
                        arguments[flag] = args[++i];
                        break;
                    case FlagStart:
                        arguments[flag] = (AutoStart)Enum.Parse(typeof(AutoStart), args[++i].ToLower(), true);
                        break;
                    default:
                        throw new ArgumentException($"Unrecognized flag: {flag}");
                }
            }
        }

        /// <summary>
        /// Processes the INI file, applying any changes.
        /// </summary>
        static void ProcessINI()
        {
            var ini = new IniManager(IniFile)
            {
                ReturnDefaultIfEmpty = true
            };

            ini.Load();

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
                Console.WriteLine($"4:   {NameCustom}");

            Console.WriteLine();
            Console.WriteLine("Esc: Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            if (_input.KeyChar == '\0')
                _input = Console.ReadKey();

            Logger.LogLine($"Input character: {_input.Key.ToString()}");

            Console.Clear();

            switch (_input.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    Start("Fallout 3 Launcher", PathLauncher, ArgumentsLauncher);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Start("FOSE", PathFOSE, ArgumentsFOSE);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    Start("Mod Organizer", PathModOrganizer, ArgumentsModOrganizer);
                    break;
                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    if (!_customEnabled)
                        goto default;

                    Start(NameCustom, PathCustom, ArgumentsCustom);
                    break;
                case ConsoleKey.Escape:
                    Logger.LogLine("Exiting...");
                    break;
                default:
                    Logger.WriteAndLogLine($"Unrecognized input: {_input.Key.ToString()}");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to try again...");
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
        /// </summary>
        static void Start(string name, string path, string arguments)
        {
            if (!File.Exists(path))
            {
                Logger.WriteAndLogLine($"Couldn't find {name}, press any key to exit...");
                Console.ReadKey();
            }
            else
            {
                try
                {
                    var log = Quiet ? (Action<string>)Logger.LogLine : Logger.WriteAndLogLine;

                    log($"Attempting to start {name}...");

                    Process.Start(new ProcessStartInfo(path)
                    {
                        Arguments = string.IsNullOrEmpty(arguments) ? string.Empty : arguments
                    });

                    log("Successful! Now exiting...");
                }
                catch (Exception ex)
                {
                    Logger.WriteAndLogLine($"Error starting {name}:");
                    Logger.WriteAndLogLine(ex);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
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
}
