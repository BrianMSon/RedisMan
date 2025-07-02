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
        startWinForm();

        string host = "127.0.0.1";
        int port = 6379;
        string? command = null;
        string? username = null;
        string? password = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host":
                case "-h":
                    if (i + 1 < args.Length) host = args[++i];
                    else Console.WriteLine("Missing value for --host");
                    break;
                case "--port":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedPort))
                        port = parsedPort;
                    else Console.WriteLine("Invalid or missing value for --port");
                    break;
                case "--command":
                case "-c":
                    if (i + 1 < args.Length) command = args[++i];
                    else Console.WriteLine("Missing value for --command");
                    break;
                case "--username":
                case "-u":
                    if (i + 1 < args.Length) username = args[++i];
                    else Console.WriteLine("Missing value for --username");
                    break;
                case "--password":
                    if (i + 1 < args.Length) password = args[++i];
                    else Console.WriteLine("Missing value for --password");
                    break;
                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    break;
            }
        }

        password = "idoladmin9876";

        _ = Repl.Run(host, port, command, username, password);
        return 0;
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
