namespace Plus.Core;

internal static class BootProbe
{
    private static readonly string BootLogPath = Path.Join(Directory.GetCurrentDirectory(), "boot.log");

    public static void Write(string message)
    {
        var line = $"[boot] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        try
        {
            Console.WriteLine(line);
            Console.Out.Flush();
        }
        catch
        {
        }

        try
        {
            File.AppendAllText(BootLogPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
