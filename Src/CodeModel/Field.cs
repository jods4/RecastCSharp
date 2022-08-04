using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecastCSharp.CodeModel;

public class Field : IModelSymbol
{
  private readonly FieldDeclarationSyntax syntax;
  private readonly Compilation compilation;

  private ISymbol? _symbol;

  internal ISymbol Symbol
    => _symbol ??= compilation.GetSemanticModel(syntax.SyntaxTree)
                              .GetDeclaredSymbol(syntax)!;

  public string Name { get; }

  public string? Value { get; }

  public bool IsAbstract => HasModifier(SyntaxKind.AbstractKeyword);

  public bool IsVirtual => HasModifier(SyntaxKind.VirtualKeyword);

  public bool IsOverride => HasModifier(SyntaxKind.OverrideKeyword);

  public bool IsNew => HasModifier(SyntaxKind.NewKeyword);

  public bool IsPublic => HasModifier(SyntaxKind.PublicKeyword);

  public bool IsPrivate => HasModifier(SyntaxKind.PrivateKeyword);

  public bool IsProtected => HasModifier(SyntaxKind.ProtectedKeyword);

  private bool HasModifier(SyntaxKind keyword) => syntax.Modifiers.Any(m => m.IsKind(keyword));

  public Field(FieldDeclarationSyntax syntax, VariableDeclaratorSyntax variable, Compilation compilation)
  {
    this.syntax = syntax;
    this.compilation = compilation;
    Name = variable.Identifier.Text;
    Value = variable.Initializer?.Value.ToString();
  }

  public bool HasAttribute(string name)
  {
    return syntax.AttributeLists
      .SelectMany(list => list.Attributes)
      .Any(attr => (attr.Name is IdentifierNameSyntax n) && n.Identifier.Text == name);
  }

  public RoslynAttribute? Attribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return Symbol.GetAttributes()
      .Where(a => a.AttributeClass!.Name == name)
      .Select(a => new RoslynAttribute(a))
      .FirstOrDefault();
  }
}
