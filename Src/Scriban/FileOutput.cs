using Scriban.Runtime;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RecastCSharp.Scriban;

class FileOutput : IScriptOutput
{
  private static readonly Dictionary<string, (int hash, int version)> codegenFiles = new();

  // There can be only one file written to at a time.
  // Before a new output is open, MainScript closes the current output.
  private static readonly StringBuilder builder = new(8 * 1024);

  private readonly string path;
  private readonly string folder;
  private readonly string name;


  public FileOutput(string fullPath, string folder, string name)
  {
    this.path = fullPath;
    this.folder = folder;
    this.name = name;
    builder.Clear();
  }

  public void Write(string text, int offset, int count)
    => builder.Append(text, offset, count);

  public ValueTask WriteAsync(string text, int offset, int count, CancellationToken cancellationToken)
  {
    Write(text, offset, count);
    return ValueTask.CompletedTask;
  }

  public void Save(int version)
  {
    // Codegen is pretty fast and overwriting all files is not a big deal for this tool
    // but it triggers downstream compilations (e.g. Webpack watching TS files),
    // so we avoid massively changing files if we don't have to.
    var content = builder.ToString();
    var hash = content.GetHashCode();
    if (!codegenFiles.TryGetValue(path, out var infos) || infos.hash != hash)
    {
      if (infos.version == version)
        ConsoleEx.Error($"File '{name}' is target of multiple code generations and was overwritten.");
      ConsoleEx.Info($"Write {folder}/{name}");
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, content, Encoding.UTF8); // TODO: configurable encoding?
    }
    codegenFiles[path] = (hash, version);
  }

  public static void DeleteUnwrittenFiles(int version)
  {
    // Deletes all files that were not updated at this version
    var remove = codegenFiles
      .Where(pair => pair.Value.version != version)
      .Select(pair => pair.Key)
      .ToList();
    foreach (var file in remove)
    {
      codegenFiles.Remove(file);
      File.Delete(file);
      ConsoleEx.Info("Deleted " + file);
    }
  }
}