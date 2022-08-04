using Microsoft.Extensions.FileSystemGlobbing;
using RecastCSharp.CodeModel;
using RecastCSharp.FileSystem;
using RecastCSharp.Functions;
using Scriban;
using Scriban.Runtime;
using System.IO;

namespace RecastCSharp.Scriban;

class MainScript
{
  public static bool Verbose;

  private readonly string root;
  private readonly Template template;

  private bool firstRun = true;
  private int version = 0; // Incremented at each codegen
  public Solution? Solution { get; private set; }

  public MainScript(FileInfo file)
  {
    template = Template.Parse(File.ReadAllText(file.FullName), file.FullName);
    root = Path.GetDirectoryName(file.FullName)!;
  }

  public void Run() => Run(false, out _);

  public void Run(bool watch, out Watcher? watcher)
  {
    Watcher? _watcher = null;

    var context = new TemplateContext
    {
      TemplateLoader = new FileTemplateLoader(),
      AutoIndent = false,
    };

    var globals = new ScriptObject();
    // First run only: load the solution and specific projects
    globals.Import("solution", new Action<string, string>(LoadSolution));
    // First run only: delete all files matching glob
    globals.Import("clean", new Action<string>(Clean));
    // Main template can render into multiple output files by calling `output "path" "file"` before rendering content as usual
    globals.Import("output", new Action<string, string>(OpenFile));
    // This can help debug problems in template, it logs a message on the console
    globals.Import("log", new Action<string>(ConsoleEx.Debug));

    context.PushGlobal(globals);
    context.BuiltinObject.SetValue("iter", new IterBuiltin(), readOnly: true);
    context.BuiltinObject.SetValue("mvc", new MvcBuiltin(), readOnly: true);
    context.BuiltinObject.SetValue("type", new TypeBuiltin(), readOnly: true);
    context.BuiltinObject.SetValue("url", new UrlBuiltin(), readOnly: true);
    context.BuiltinObject.SetValue("where", new WhereBuiltin(), readOnly: true);
    ((ScriptObject)context.BuiltinObject["string"]).Import(typeof(StringExtensions));

    version++;

    try
    {
      template.Render(context);
    }
    catch (Exception ex)
    {
      ConsoleEx.Error(ex.InnerException?.ToString() ?? ex.ToString());
    }

    CloseFile();

    firstRun = false;

    FileOutput.DeleteUnwrittenFiles(version);

    watcher = _watcher;

    void CloseFile()
    {
      if (context.Output is FileOutput output)
      {
        context.PopOutput();
        output.Save(version);
      }
    }

    void OpenFile(string path, string file)
    {
      CloseFile();
      var fullPath = Path.Combine(root, path, file);
      context.PushOutput(new FileOutput(fullPath, path, file));
    }

    void LoadSolution(string sln, string csprojGlob)
    {
      if (firstRun)
      {
        sln = Path.GetFullPath(sln, root);
        Solution = new Solution(sln);

        if (watch)
          _watcher = new Watcher(Path.GetDirectoryName(sln)!, this, Solution);

        Solution.LoadProjects(csprojGlob).Wait();
      }

      Solution!.RebuildAll().Wait();

      // Code model intended to be used by templates
      globals.SetValue("code", Solution!.Code, readOnly: true);
    }
  }

  public void Clean(string glob)
  {
    if (!firstRun) return;
    var matcher = new Matcher();
    matcher.AddIncludePatterns(glob.Split(','));
    foreach (var file in matcher.GetResultsInFullPath(root))
      File.Delete(file);
  }
}