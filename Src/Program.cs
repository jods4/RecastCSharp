using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using RecastCSharp.FileSystem;
using RecastCSharp.Scriban;

var file = new Argument<FileInfo>("script", "Scriban script to execute").ExistingOnly();
var watch = new Option<bool>(new[] { "--watch", "-w" }, "Watch for file changes and incrementally update generated code");
var verbose = new Option<bool>(new[] { "--verbose", "-v" }, "Display source compilation warnings");

var command = new RootCommand("Generates code from a C# solution") { file, watch, verbose };
command.SetHandler<FileInfo, bool, bool>(Run, file, watch, verbose);

return await command.InvokeAsync(args);

static async Task Run(FileInfo script, bool watch, bool verbose)
{
  var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
  ConsoleEx.Info("RecastCSharp Code Generator " + version.ToString(2));

  MainScript.Verbose = verbose;
  Watcher? watcher = null;

  using (new Timed("Code generated in {0}"))
  {
    new MainScript(script).Run(watch, out watcher);
  }

  if (watcher != null)
    await watcher.Watch();  // Never completes
}