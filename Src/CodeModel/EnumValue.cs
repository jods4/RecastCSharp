using Microsoft.CodeAnalysis;

namespace RecastCSharp.CodeModel;

public class EnumValue
{
  private readonly IFieldSymbol symbol;

  public string Name => symbol.Name; // PascalCase enum values

  public string Value => symbol.ConstantValue!.ToString()!;

  public EnumValue(IFieldSymbol symbol)
  {
    this.symbol = symbol;
  }

  public bool HasAttribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return symbol.GetAttributes().Any(a => a.AttributeClass!.Name == name);
  }

  public RoslynAttribute? Attribute(string name)
  {
    if (!name.EndsWith("Attribute")) name += "Attribute";
    return symbol.GetAttributes()
      .Where(a => a.AttributeClass!.Name == name)
      .Select(a => new RoslynAttribute(a))
      .FirstOrDefault();
  }
}
