using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecastCSharp.CodeModel;

public class Method : IModelSymbol
{
  internal readonly IMethodSymbol symbol;
  private readonly Compilation compilation;

  public string Name => symbol.Name;

  public bool IsAbstract => symbol.IsAbstract;
  public bool IsVirtual => symbol.IsVirtual;
  public bool IsOverride => symbol.IsOverride;
  public bool IsOverload => symbol.ContainingType.GetMembers(symbol.Name).Count() > 1;
  public bool IsNew => symbol.DeclaringSyntaxReferences.Any(decl => ((MethodDeclarationSyntax)decl.GetSyntax()).Modifiers.Any(m => m.IsKind(SyntaxKind.NewKeyword)));
  public bool IsPublic => symbol.DeclaredAccessibility == Accessibility.Public;
  public bool IsPrivate => symbol.DeclaredAccessibility == Accessibility.Private;
  public bool IsProtected => symbol.DeclaredAccessibility == Accessibility.Protected;

  public IEnumerable<Parameter> Parameters
    => symbol.Parameters.Select(p => new Parameter(p));

  public RoslynType Returns
  {
    get
    {
      var ret = symbol.ReturnType;

      // Perform code analysis to determine the real shape of `object` and `Task<object>` methods.
      if (ret.SpecialType == SpecialType.System_Object ||
          (ret.Name == "Task" && ret is INamedTypeSymbol ts && ts.IsGenericType && ts.TypeArguments[0].SpecialType == SpecialType.System_Object))
        ret = AnalyzeCode();

      return new RoslynType(ret);
    }
  }

  public Method(IMethodSymbol symbol, Compilation compilation)
  {
    this.symbol = symbol;
    this.compilation = compilation;
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

  private ITypeSymbol AnalyzeCode()
  {
    // A method, we assume a single declaration (no partial or weird stuff?)
    var syntax = (MethodDeclarationSyntax)symbol.DeclaringSyntaxReferences[0].GetSyntax();

    // We must ask for the right semantic model for this tree.
    // If we are analyzing an inherited method, we might be in a different source tree
    // than the class currently being analyzed (would be true of partials as well)
    var model = compilation.GetSemanticModel(syntax.SyntaxTree);

    // Methods can have either an expression body, or a body (block)
    var type = syntax.ExpressionBody != null ?
      model.GetTypeInfo(syntax.ExpressionBody.Expression).Type :
      // We look for return statements, but be careful not to descend into lambdas or local functions (those have unrelated return statements)
      syntax.Body!
            .DescendantNodes(node => !node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
                                  && !node.IsKind(SyntaxKind.SimpleLambdaExpression)
                                  && !node.IsKind(SyntaxKind.LocalFunctionStatement))
            .OfType<ReturnStatementSyntax>()
            .Where(r => r.Expression != null)
            .Select(r => model.GetTypeInfo(r.Expression!).Type)
            // We don't try to merge different types into a union type.
            // For now we just consider the first typed return as the shape of the API.
            .FirstOrDefault(t => t != null && t.SpecialType != SpecialType.System_Object);

    // Its nullability is determined by the method signature: is it `object` or `object?`
    return type?.WithNullableAnnotation(symbol.ReturnType.NullableAnnotation)
        ?? symbol.ReturnType; // Object -> unknown
  }
}