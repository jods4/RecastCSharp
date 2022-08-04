using Polly;
using RecastCSharp.CodeModel;
using RecastCSharp.Scriban;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RecastCSharp.FileSystem;

class Watcher
{
  private readonly FileSystemWatcher watcher;
  private readonly Channel<FileChange> channel = Channel.CreateUnbounded<FileChange>();
  private readonly MainScript main;
  private readonly Solution solution;

  public Watcher(string folder, MainScript main, Solution solution)
  {
    watcher = new FileSystemWatcher(folder, "*.cs")
    {
      IncludeSubdirectories = true,
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
    };
    watcher.Created += (source, e) => channel.Writer.WriteAsync(new FileChange { Filename = e.FullPath, Change = ChangeType.Added });
    watcher.Changed += (source, e) => channel.Writer.WriteAsync(new FileChange { Filename = e.FullPath, Change = ChangeType.Changed });
    watcher.Deleted += (source, e) => channel.Writer.WriteAsync(new FileChange { Filename = e.FullPath, Change = ChangeType.Removed });
    watcher.Renamed += async (source, e) =>
    {
        // Note: Visual Studio writes changes by renaming the old .cs file to a temp name, writing a new temp file, deleting the old one, renaming the new one.
        //       Lots of renaming.
        if (e.OldFullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        await channel.Writer.WriteAsync(new FileChange { Filename = e.OldFullPath, Change = ChangeType.Removed });
      if (e.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        await channel.Writer.WriteAsync(new FileChange { Filename = e.FullPath, Change = ChangeType.Added });
    };

    watcher.EnableRaisingEvents = true;
    this.main = main;
    this.solution = solution;
  }

  public async Task Watch()
  {
    // Sometimes when we get a notification the process hasn't finished writing / closed the file, etc.
    // For robustness we just wait a bit and try again
    var retry = Policy
      .Handle<Exception>()
      .Fallback(() => { }, ex => ConsoleEx.Error("Incremental update failed: " + ex.Message))
      .Wrap(Policy
        .Handle<IOException>()
        .WaitAndRetry(4, _ => TimeSpan.FromMilliseconds(150))
      );

    while (true)
    {
      await channel.Reader.WaitToReadAsync();

      using (var timed = new Timed("Code updated in {0}"))
      {
        while (channel.Reader.TryRead(out var file))
          retry.Execute(() => solution.ProcessChange(file.Change, file.Filename));

        await solution.RebuildAll();
        main.Run();
      }
    }
  }
}
