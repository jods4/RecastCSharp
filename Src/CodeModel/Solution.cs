using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using RecastCSharp.FileSystem;
using RecastCSharp.Scriban;
using System.IO;
using System.Threading.Tasks;

namespace RecastCSharp.CodeModel;

class Solution : IDisposable
{
  private readonly string slnFile;
  private readonly MSBuildWorkspace workspace = MSBuildWorkspace.Create();
  private Microsoft.CodeAnalysis.Solution solution = null!; // Not null once LoadProjects is called
  private List<Project> projects = null!; // Not null once LoadProjects is called

  public Code Code => new Code(projects);

  static Solution()
    => MSBuildLocator.RegisterDefaults();

  public Solution(string slnFile)
  {
    this.slnFile = slnFile;

    workspace.WorkspaceFailed += (sender, e) =>
    {
      if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure || MainScript.Verbose)
        ConsoleEx.Error("Workspace failure: " + e.Diagnostic.Message);
    };
  }

  public async Task LoadProjects(string glob)
  {
    solution = await workspace.OpenSolutionAsync(slnFile);

    var matcher = new Matcher();
    matcher.AddIncludePatterns(glob.Split(','));

    projects = solution.Projects
      .Where(p => matcher.Match(Path.GetDirectoryName(slnFile)!, p.FilePath!).HasMatches)
      .Select(p => new Project(p.Id))
      .ToList();
  }

  public Task RebuildAll()
    => Task.WhenAll(projects.Select(p => p.Build(solution)));

  public void ProcessChange(ChangeType change, string file)
  {
    switch (change)
    {
      case ChangeType.Added:
        var project = solution.Projects.FirstOrDefault(p => file.StartsWith(Path.GetDirectoryName(p.FilePath)!));
        if (project == null)
          ConsoleEx.Info($"File '{file}' belongs to no project and was ignored.");
        else
          solution = solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            Path.GetFileName(file),
            File.ReadAllText(file),
            filePath: file
          );
        break;

      case ChangeType.Removed:
        foreach (var id in solution.GetDocumentIdsWithFilePath(file))
          solution = solution.RemoveDocument(id);
        break;

      case ChangeType.Changed:
        var ids = solution.GetDocumentIdsWithFilePath(file);
        if (ids.Length == 0) break;
        var src = SourceText.From(File.ReadAllText(file));
        foreach (var id in ids)
          solution = solution.WithDocumentText(id, src);
        break;
    };
  }

  public void Dispose() => workspace.Dispose();
}
