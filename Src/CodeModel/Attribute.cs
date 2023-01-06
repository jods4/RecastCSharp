using Microsoft.CodeAnalysis;
using Scriban.Runtime;

namespace RecastCSharp.CodeModel;

public class RoslynAttribute : IName
{
  private readonly AttributeData attr;

  public string Name
  {
    get
    {
      var name = attr.AttributeClass!.Name;
      return name.EndsWith("Attribute")
        ? name[..^"Attribute".Length]
        : name;
    }
  }

  public object? Value => GetValue(attr.ConstructorArguments[0]);

  public ScriptArray Values
  {
    get
    {
      var array = new ScriptArray(attr.ConstructorArguments.Select(GetValue)) { IsReadOnly = true };
      if (attr.NamedArguments.Length > 0)
      {
        var so = array.ScriptObject;
        foreach (var kvp in attr.NamedArguments)
          so.SetValue(kvp.Key, GetValue(kvp.Value), readOnly: true);
      }
      return array;
    }
  }

  internal IReadOnlyList<ITypeSymbol>? TypeArguments => attr.AttributeClass?.TypeArguments;

  public RoslynAttribute(AttributeData attr)
  {
    this.attr = attr;
  }

  private static object GetValue(TypedConstant t)
  {
    return t.Kind != TypedConstantKind.Array
      ? t.Value!
      : t.Values.Select(GetValue).ToArray();
  }
}
