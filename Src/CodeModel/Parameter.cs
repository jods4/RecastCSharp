using Microsoft.CodeAnalysis;

namespace RecastCSharp.CodeModel;

public class Parameter : IName
{
  private readonly IParameterSymbol symbol;

  public string Name => symbol.Name;
  public RoslynType Type => new(symbol.Type);

  public Parameter(IParameterSymbol symbol)
  {
    this.symbol = symbol;
  }
}
