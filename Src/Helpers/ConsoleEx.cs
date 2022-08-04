namespace RecastCSharp.Helpers;

static class ConsoleEx
{
  public static void Error(string message) => WriteColor(message, ConsoleColor.Red);

  public static void Success(string message) => WriteColor(message, ConsoleColor.Green);

  public static void Success(string format, params object[] args) => Success(string.Format(format, args));

  public static void Info(string message) => Console.WriteLine(message);

  public static void Info(string format, params object[] args) => Info(string.Format(format, args));

  public static void Debug(string message) => WriteColor(message, ConsoleColor.Cyan);

  static void WriteColor(string message, ConsoleColor color)
  {
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ResetColor();
  }
}