using Microsoft.CodeAnalysis;

namespace RecastCSharp.CodeModel;

public class RoslynType : IEquatable<RoslynType>, IName
{
  private readonly ITypeSymbol symbol;

  public RoslynType(ITypeSymbol symbol)
  {
    this.symbol = symbol;
  }

  private static readonly Dictionary<RoslynType, string> anonymous = new();

  internal static void ClearAnonymousNames() => anonymous.Clear();

  public string Name 
  {
    get
    {
      if (!IsAnonymous) return symbol.Name;

      if (!anonymous.TryGetValue(this, out var name))
      {
        name = "$" + (anonymous.Count + 1);
        anonymous.Add(this, name);
      }
      return name;
    }
  }

  public string MetadataName => symbol.MetadataName;

  public string Kind => symbol.TypeKind.ToString();

  public bool IsAnonymous => symbol.IsAnonymousType;

  public bool IsComplex => symbol.IsComplex();

  public bool IsNullableRef => symbol.NullableAnnotation == NullableAnnotation.Annotated;

  public IEnumerable<Property> Properties
  {
    get
    {
      HashSet<IPropertySymbol>? overrides = null;

      bool IsOverriden(IPropertySymbol p)
      {
        if (p.IsOverride)
        {
          overrides ??= new (SymbolEqualityComparer.Default);
          overrides.Add(p.OverriddenProperty!);
          return overrides.Contains(p);
        }

        if (p.IsVirtual || p.IsAbstract)
          return overrides?.Contains(p) ?? false;

        return false;
      }

      // Emit base properties first. For that we need to reverse the inheritance chain.
      return InheritedTypes(symbol)
        .SelectMany(type => type.GetMembers())
        .OfType<IPropertySymbol>()
        .Where(p => p.DeclaredAccessibility == Accessibility.Public 
                 && !p.IsStatic 
                 && !IsOverriden(p))
        .Select(p => new Property(p));
    }
  }

  #region Enum types

  public bool IsEnum => symbol.TypeKind == TypeKind.Enum;

  public IEnumerable<EnumValue> EnumValues
    => from m in symbol.GetMembers().OfType<IFieldSymbol>()
       select new EnumValue(m);

  #endregion

  #region Generic types

  public bool IsGeneric => (symbol as INamedTypeSymbol)?.IsGenericType ?? false;

  internal IReadOnlyList<ITypeSymbol>? TypeArguments => (symbol as INamedTypeSymbol)?.TypeArguments;

  #endregion

  private static IEnumerable<ITypeSymbol> InheritedTypes(ITypeSymbol symbol)
  {
    // iter == null can happen if we walk up unbounded generic types
    for (var iter = symbol; iter != null && iter.SpecialType != SpecialType.System_Object; iter = iter.BaseType)
      yield return iter;
  }

  // Does not include the type when it is an interface itself.
  internal IEnumerable<INamedTypeSymbol> Interfaces => symbol.AllInterfaces;

  #region IEquatable

  public bool Equals(RoslynType? other) => symbol.Equals(other?.symbol, SymbolEqualityComparer.Default);

  public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(symbol);

  #endregion
}
