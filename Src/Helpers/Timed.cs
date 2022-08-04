using System.Diagnostics;

namespace RecastCSharp.Helpers;

class Timed : IDisposable
{
  private readonly string message;
  private readonly bool success;
  private readonly Stopwatch stopwatch = new();

  public Timed(string message, bool success = true)
  {
    this.message = message;
    this.success = success;
    stopwatch.Start();
  }

  public void Dispose()
  {
    stopwatch.Stop();
    if (success)
      ConsoleEx.Success(message, stopwatch.Elapsed);
    else
      ConsoleEx.Info(message, stopwatch.Elapsed);
  }
}