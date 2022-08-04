using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecastCSharp.CodeModel;

public class Class : IModelSymbol
{
  private readonly ClassDeclarationSyntax syntax;
  private readonly Compilation compilation;

  private INamedTypeSymbol? _symbol;

  internal INamedTypeSymbol Symbol
    => _symbol ??= compilation.GetSemanticModel(syntax.SyntaxTree)
                              .GetDeclaredSymbol(syntax)!;

  public string Name => syntax.Identifier.ValueText;

  private bool HasModifier(SyntaxKind keyword) => syntax.Modifiers.Any(m => m.IsKind(keyword));

  public bool IsAbstract => HasModifier(SyntaxKind.AbstractKeyword);
  public bool IsVirtual => HasModifier(SyntaxKind.VirtualKeyword);
  public bool IsOverride => HasModifier(SyntaxKind.OverrideKeyword);
  public bool IsNew => HasModifier(SyntaxKind.NewKeyword);
  public bool IsPublic => HasModifier(SyntaxKind.PublicKeyword);
  public bool IsPrivate => HasModifier(SyntaxKind.PrivateKeyword);
  public bool IsProtected => HasModifier(SyntaxKind.ProtectedKeyword);

  public bool HasAttribute(string name)
  {
    return syntax.AttributeLists
                 .SelectMany(l => l.Attributes)
                 .Any(a => a.Name is IdentifierNameSyntax n && n.Identifier.Text == name);
  }

  public RoslynAttribute? Attribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return Symbol.GetAttributes()
      .Where(a => a.AttributeClass!.Name == name)
      .Select(a => new RoslynAttribute(a))
      .FirstOrDefault();
  }

  public RoslynType AsType => new RoslynType(Symbol);

  public IEnumerable<Field> Consts
  {
    get
    {
      return from f in syntax.Members.OfType<FieldDeclarationSyntax>()
             where f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword))
             from v in f.Declaration.Variables
             select new Field(f, v, compilation);
    }
  }

  public IEnumerable<Method> Methods
  {
    get
    {
      return from m in BaseTypes()
              .Reverse()
              .SelectMany(t => t.GetMembers())
              .OfType<IMethodSymbol>()
             where !m.IsOverride
             select new Method(m, compilation);
    }
  }

  public Class(ClassDeclarationSyntax syntax, Compilation compilation)
  {
    this.syntax = syntax;
    this.compilation = compilation;
    _symbol = null;
  }

  public bool Implements(string interfaceName) => Symbol.AllInterfaces.Any(i => i.Name == interfaceName);

  // More performant alternative to Implements() but only checks syntax (direct implementation), no semantics (inheritance)
  public bool DirectlyImplements(string interfaceName)
  {
    return syntax.BaseList?.Types.OfType<SimpleBaseTypeSyntax>()
      .Any(t => (t.Type as IdentifierNameSyntax)?.Identifier.Text == interfaceName)
      ?? false;
  }

  private IEnumerable<INamedTypeSymbol> BaseTypes()
  {
    var i = Symbol;
    while (i != null && i.SpecialType != SpecialType.System_Object)
    {
      yield return i;
      i = i.BaseType;
    }
  }
}