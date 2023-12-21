using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RecastCSharp.Scriban;
using System.IO;
using System.Threading.Tasks;

namespace RecastCSharp.CodeModel;

public class Project : IName
{
  private readonly ProjectId id;
  private Microsoft.CodeAnalysis.Project project = null!; // Never null after Build
  private Compilation compilation = null!; // Never null after Build

  public string Name => project.Name;

  public IEnumerable<RoslynFile> Files
  {
    get
    {
      string root = Path.GetDirectoryName(project.Solution.FilePath)!;
      return from tree in compilation.SyntaxTrees
             select new RoslynFile(tree, compilation, root);
    }
  }

  public IEnumerable<Class> Classes
    => Nodes<ClassDeclarationSyntax>().Select(x => new Class(x, compilation));

  internal IEnumerable<NamespaceNode> NamespaceNodes
    => Nodes<NamespaceDeclarationSyntax>().Select(x => new NamespaceNode(x, compilation));

  public IEnumerable<RoslynType> Enums
    => from node in Nodes<EnumDeclarationSyntax>()
       let symbol = compilation.GetSemanticModel(node.SyntaxTree).GetDeclaredSymbol(node)
       select new RoslynType(symbol);

  public Project(ProjectId id)
  {
    this.id = id;
  }

  public async Task Build(Microsoft.CodeAnalysis.Solution solution)
  {
    project = solution.GetProject(id)!;
    compilation = (await project.GetCompilationAsync())!;

    // Not checking errors seems to be faster, prob. less is analyzed?
    // Roslyn is resilient to invalid code (e.g. when you're in the middle of coding)
    // and presents a somewhat coherent view of the code anyway (assuming some automatic error "correction").
    if (MainScript.Verbose)
    {
      foreach (var diag in compilation.GetDiagnostics())
      {
        if (diag.Severity == DiagnosticSeverity.Error)
          ConsoleEx.Error("Compilation error @ " + diag.Location + ": " + diag.GetMessage());
      }
    }
  }

  private IEnumerable<T> Nodes<T>() where T : CSharpSyntaxNode
    => compilation.SyntaxTrees
      .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<T>());
}
