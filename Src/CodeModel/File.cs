using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace RecastCSharp.CodeModel;

public class RoslynFile : IName
{
  private readonly SyntaxTree tree;
  private readonly Compilation compilation;
  private readonly string root;

  public string Directory => Path.GetRelativePath(root, Path.GetDirectoryName(tree.FilePath)!).Replace('\\', '/');

  public string Name => Path.GetFileNameWithoutExtension(tree.FilePath);

  public IEnumerable<Class> Classes
  {
    get
    {
      return from declaration in tree.GetRoot()
                                     .DescendantNodes()
                                     .OfType<ClassDeclarationSyntax>()
             select new Class(declaration, compilation);
    }
  }

  public RoslynFile(SyntaxTree tree, Compilation compilation, string root)
  {
    this.tree = tree;
    this.compilation = compilation;
    this.root = root;
  }
}
