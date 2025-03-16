using System;
using System.Runtime.InteropServices;

public static class ConsoleManager
{
    // Import functions to allocate and free console
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private static bool _consoleAllocated = false;

    // Enum for log levels
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR
    }

    /// <summary>
    /// Initializes the console (allocates if not already allocated).
    /// </summary>
    public static void Initialize()
    {
        if (!_consoleAllocated)
        {
            AllocConsole();
            _consoleAllocated = true;
            Console.Title = "Client Console";
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Console initialized.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Frees the console (use when closing the app if needed).
    /// </summary>
    public static void Shutdown()
    {
        if (_consoleAllocated)
        {
            FreeConsole();
            _consoleAllocated = false;
        }
    }

    /// <summary>
    /// Logs a message with the appropriate color.
    /// </summary>
    public static void Log(string message, LogLevel level = LogLevel.INFO)
    {
        if (!_consoleAllocated) Initialize();
        Console.ResetColor();

        switch (level)
        {
            case LogLevel.DEBUG:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case LogLevel.INFO:
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case LogLevel.WARNING:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogLevel.ERROR:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] [{level}] {message}");
        Console.ResetColor();
    }
}
