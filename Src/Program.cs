using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using RecastCSharp.FileSystem;
using RecastCSharp.Scriban;

internal class Program
{
  private static async Task<int> Main(string[] args)
  {
    var command = new RootCommand("Generates code from a C# solution")
    {
      Arguments =
      {
        new Argument<FileInfo>("script") { Description = "Scriban script to execute" }.AcceptExistingOnly(),
      },
      Options =
      {
        new Option<bool>("--watch", "-w") { Description = "Watch for file changes and incrementally update generated code" },
        new Option<bool>("--verbose", "-v") { Description = "Display source compilation warnings" },
      }
    };
    command.SetAction(Run);

    return await command.Parse(args).InvokeAsync();

    static async Task Run(ParseResult parsed)
    {
      var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
      ConsoleEx.Info("RecastCSharp Code Generator " + version.ToString(2));

      MainScript.Verbose = parsed.GetValue<bool>("--verbose");
      Watcher? watcher = null;

      using (new Timed("Code generated in {0}"))
      {
        new MainScript(parsed.GetRequiredValue<FileInfo>("script"))
          .Run(parsed.GetValue<bool>("--watch"), out watcher);
      }

      if (watcher != null)
        await watcher.Watch();  // Never completes
    }
  }
}