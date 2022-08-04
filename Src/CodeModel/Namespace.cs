using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecastCSharp.CodeModel;

class NamespaceNode
{
  private readonly NamespaceDeclarationSyntax syntax;
  private readonly Compilation compilation;

  public NamespaceNode(NamespaceDeclarationSyntax syntax, Compilation compilation)
  {
    this.syntax = syntax;
    this.compilation = compilation;
  }

  public string Name => syntax.Name.ToString();

  public IEnumerable<Class> Classes
  {
    get
    {
      return from declaration in syntax.DescendantNodes().OfType<ClassDeclarationSyntax>()
             select new Class(declaration, compilation);
    }
  }

  public IEnumerable<RoslynType> Enums
  {
    get
    {
      var semantic = compilation.GetSemanticModel(syntax.SyntaxTree);
      return from declaration in syntax.DescendantNodes().OfType<EnumDeclarationSyntax>()
             select new RoslynType(semantic.GetDeclaredSymbol(declaration)!);
    }
  }
}

public class Namespace : IName
{
  private readonly IEnumerable<NamespaceNode> nodes;

  public string Name { get; init; }

  public IEnumerable<Class> Classes => nodes.SelectMany(n => n.Classes);

  public IEnumerable<RoslynType> Enums => nodes.SelectMany(n => n.Enums);

  internal Namespace(string name, IEnumerable<NamespaceNode> nodes)
  {
    Name = name;
    this.nodes = nodes;
  }
}