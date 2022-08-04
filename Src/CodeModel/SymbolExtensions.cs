using Microsoft.CodeAnalysis;

namespace RecastCSharp.CodeModel;

static class SymbolExtensions
{
  // Simple types are primitives that map to JS number, bool, Date, string, enums...
  public static bool IsComplex(this ITypeSymbol symbol)
  {
    if (symbol.Name == "Nullable")
      return ((INamedTypeSymbol)symbol).TypeArguments[0].IsComplex();

    return
      symbol.SpecialType == SpecialType.None &&
      symbol.Name is not ("DateTimeOffset" or "TimeOnly" or "DateOnly" or "TimeSpan" or "Guid") &&
      symbol.TypeKind != TypeKind.Enum;
  }
}
