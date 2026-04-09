using Microsoft.Win32;

namespace PrintAgent.Services;

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MakerPrintAgent";

    public static bool IsEnabled(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(AppName)?.ToString();
        return string.Equals(value, WrapArg(exePath), StringComparison.OrdinalIgnoreCase);
    }

    public static void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        key.SetValue(AppName, WrapArg(exePath));
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private static string WrapArg(string input) => $"\"{input}\"";
}
