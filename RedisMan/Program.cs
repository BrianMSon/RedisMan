using RedisMan.Library.Commands;
using System;
using System.Runtime.InteropServices;

namespace RedisMan;

#if WINDOWS
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;
#endif // WINDOWS

/// <summary>
/// TODO:
///     - [ ] Implement custom argument parser
///     - [X] args[] parser, for connection and command to execute
///     - [-] Repl.cs
///     - [-] CommandBuilder
///     - [ ] Connection.cs
///     - [X] RESPParser.cs
///     - [X] Fix "Trim unused code" for Reflection
/// </summary>
public class Program
{
    public static string CONSOLE_TITLE = $"RedisMan";

    static int Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 6379;
        string? command = null;
        string? username = null;
        string? password = null;
        int dbnum = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
            case "-h":
            case "--host":
                if (i + 1 < args.Length) host = args[++i];
                else Console.WriteLine("Missing value for --host or -h");
                break;
            case "-p":
            case "--port":
                if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedPort))
                    port = parsedPort;
                else Console.WriteLine("Invalid or missing value for --port or -p");
                break;
            case "-n":
            case "--db":
                if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedDbnum))
                    dbnum = parsedDbnum;
                else Console.WriteLine("Invalid or missing value for --db or -n");
                break;
            case "-c":
            case "--command":
                if (i + 1 < args.Length) command = args[++i];
                else Console.WriteLine("Missing value for --command or -c");
                break;
            case "-u":
            case "--username":
                if (i + 1 < args.Length) username = args[++i];
                else Console.WriteLine("Missing value for --username or -u");
                break;
            case "-a":
            case "--password":
                if (i + 1 < args.Length) password = args[++i];
                else Console.WriteLine("Missing value for --password or -P");
                break;
            case "-H":
            case "--help":
                printHelpMessage();
                return 1;
            default:
                Console.WriteLine($"Unknown argument: {args[i]}");
                printHelpMessage();
                return 1;
            }
        }

        startWinForm();

        //password = "redisDev9876"; //test password
        //port = 6379; //test
        //dbnum = 1; //test

        _ = Repl.Run(host, port, dbnum, command, username, password);
        return 0;
    }

    private static void printHelpMessage()
    {
        Console.WriteLine("Usage: RedisMan [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -h <hostname>       Hostname of the Redis server (default:127.0.0.1)");
        Console.WriteLine("  --host <hostname>");
        Console.WriteLine("  -p <port>           Port of the Redis server (default: 6379)");
        Console.WriteLine("  --port <port>");
        Console.WriteLine("  -c <cmd>            Command to execute on the Redis server");
        Console.WriteLine("  --command <cmd>");
        Console.WriteLine("  -u <user>           Username for authentication (optional)");
        Console.WriteLine("  --username <user>");
        Console.WriteLine("  -a <pass>           password for Authentication (optional)");
        Console.WriteLine("  --password <pass>");
        Console.WriteLine("  -n <dbnum>          database Number to connect to (default: 0)");
        Console.WriteLine("  --db <dbnum>");
        Console.WriteLine("  -H                  show this Help message");
        Console.WriteLine("  --help");
    }

    private static void startWinForm()
    {
#if WINDOWS
        // 콘솔 이벤트 핸들러 등록
        SetConsoleCtrlHandler(ConsoleEventCallback, true);

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        // 콘솔 타이틀을 고정시켜 핸들 찾기 용이하게 함
        // 타이틀 뒤에 PID 추가
        //
        Console.Title = CONSOLE_TITLE;

        Console.WriteLine("WINDOWS 환경에서 실행 중입니다.");

        // 폼을 띄우기
        new Thread(() =>
        {
            ApplicationConfiguration.Initialize();
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1)) // Windows7 이상
            {
                // 폼 인스턴스 생성
                var form = new WinTestForm.MainForm();

                System.Windows.Forms.Application.Run(form);
            }

            Console.WriteLine("\n폼이 닫혔습니다.");

            // 프로그램 종료
            Environment.Exit(0);
        }).Start();
#else
        ConsoleLogger.Log($"LINUX 환경에서 실행 중입니다.", ConsoleColor.Cyan);
#endif
    }

    private static bool ConsoleEventCallback(int eventType)
    {
        // eventType
        // 0: CTRL_C_EVENT
        // 1: CTRL_BREAK_EVENT  
        // 2: CTRL_CLOSE_EVENT (닫기 버튼)
        // 5: CTRL_LOGOFF_EVENT
        // 6: CTRL_SHUTDOWN_EVENT
        if (eventType == 2 || eventType == 5 || eventType == 6)
        {
            //Console.WriteLine("\n콘솔 창을 닫습니다...");
            throw new OperationCanceledException("\n콘솔 창이 닫혔습니다.");
        }

        return false;
    }

    //////////////////////////////////////////////////////////////////
    private delegate bool ConsoleEventDelegate(int eventType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOSIZE = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
    //////////////////////////////////////////////////////////////////

}
