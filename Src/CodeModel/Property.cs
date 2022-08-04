using Microsoft.CodeAnalysis;

namespace RecastCSharp.CodeModel;

public class Property : IModelSymbol
{
  private readonly IPropertySymbol symbol;

  public string Name => symbol.Name;
  public bool IsAbstract => symbol.IsAbstract;
  public bool IsVirtual => symbol.IsVirtual;
  public bool IsOverride => symbol.IsOverride;
  public bool IsNew => false;
  public bool IsPublic => symbol.DeclaredAccessibility == Accessibility.Public;
  public bool IsProtected => symbol.DeclaredAccessibility == Accessibility.Protected;
  public bool IsPrivate => symbol.DeclaredAccessibility == Accessibility.Private;

  public bool HasAttribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return symbol.GetAttributes().Any(a => a.AttributeClass!.Name.Equals(name, StringComparison.Ordinal));
  }

  public RoslynType Type => new RoslynType(symbol.Type);

  public IEnumerable<RoslynAttribute> Attributes => symbol.GetAttributes().Select(a => new RoslynAttribute(a));

  public RoslynAttribute? Attribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return symbol.GetAttributes()
      .Where(a => a.AttributeClass!.Name == name)
      .Select(a => new RoslynAttribute(a))
      .FirstOrDefault();
  }

  public Property(IPropertySymbol symbol)
  {
    this.symbol = symbol;
  }
}
