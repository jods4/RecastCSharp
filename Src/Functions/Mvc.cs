using Microsoft.CodeAnalysis;
using RecastCSharp.CodeModel;
using Scriban.Runtime;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace RecastCSharp.Functions;

public class MvcBuiltin : ScriptObject
{
  private static string? RouteFromAttribute(INamedTypeSymbol symbol)
  {
    for (var iter = symbol; iter != null; iter = iter.BaseType)
    {
      var route = iter.GetAttributes()
        .FirstOrDefault(a =>
          a.AttributeClass
            is { Name: "RouteAttribute" }
            or { BaseType.Name: "HttpMethodAttribute" } &&
          a.ConstructorArguments.Length > 0)
        ?.ConstructorArguments[0].Value?.ToString();
      if (route != null) return route;
    }
    return null;
  }

  private static string? RouteFromAttribute(IMethodSymbol symbol)
  {
    for (var iter = symbol; iter != null; iter = iter.OverriddenMethod)
    {
      var route = iter.GetAttributes()
        .FirstOrDefault(a =>
          a.AttributeClass
            is { Name: "RouteAttribute" }
            or { BaseType.Name: "HttpMethodAttribute" } &&
          a.ConstructorArguments.Length > 0)
        ?.ConstructorArguments[0].Value?.ToString();
      if (route != null) return route;
    }
    return null;
  }

  public static IEnumerable<Method> Actions(Class controller)
    => controller.Methods.Where(m => m.IsPublic && m.symbol.GetAttributes().Any(a => a.AttributeClass?.BaseType?.Name == "HttpMethodAttribute"));

  // Contains ${param} for uri parameters, so that it can directly be used in JS interpolated string `...`
  public static string Url(
    global::Scriban.TemplateContext ctx,
    Method method,
    Class controller,
    global::Scriban.Syntax.ScriptFunction? controllerRenamer = null)
  {
    var controllerName = controllerRenamer == null
      ? controller.Name
      : (string)controllerRenamer.Invoke(ctx, null, new ScriptArray(new[] { controller.Name }), null);
    if (controllerName.EndsWith("Controller")) controllerName = controllerName[..^"Controller".Length];

    var path = UrlBuiltin
      .Join(RouteFromAttribute(controller.Symbol), RouteFromAttribute(method.symbol))
      .Replace("[controller]", controllerName)
      .Replace("{", "${"); // Syntax for parameters in route segments, e.g. [HttpGet("user/{id}")]

    var routeTokens = RouteTokens(method);

    var query = string.Join("&",
      from p in method.symbol.Parameters
      let qname = IsQuery(p, routeTokens)
      where qname != null
      select $"{ qname }=${{{ p.Name }}}");

    return query.Length > 0
      ? path + "?" + query
      : path;
  }

  private static ConditionalWeakTable<Method, List<string>?> routeTokensCache = new();

  private static List<string>? RouteTokens(Method method)
  {
    if (routeTokensCache.TryGetValue(method, out var cached))
      return cached;

    var tokens = RouteFromAttribute(method.symbol) is not string route
      ? null
      : Regex
        .Matches(route, @"\{([^}]+)\}")
        .Cast<Match>()
        .Select(m => m.Groups[1].Value)
        .ToList();

    routeTokensCache.Add(method, tokens);

    return tokens;
  }

  private static string? IsQuery(IParameterSymbol p, List<string>? routeTokens = null)
  {
    foreach (var attr in p.GetAttributes())
    {
      var attrName = attr.AttributeClass!.Name;
      if (attrName.Equals("FromQueryAttribute", StringComparison.Ordinal))
      {
        var nameArg = attr.NamedArguments.FirstOrDefault(pair => pair.Key == "Name");
        // Note that KeyValuePair is a struct, FirstOrDefault never returns null
        // and so is TypedConstant (KVP.Value)
        return nameArg.Value.Value?.ToString() ?? p.Name;
      }
      if (attrName.StartsWith("From", StringComparison.Ordinal))
        return null; // Assuming a parameter can't have multiple FromAttribute sources
    }

    // If there's no attribute, ASP.NET 6 binds simple types from query and complex ones from body.
    // In theory, types with a converter than can Convert from String are also included in the previous list,
    // but this is hard to determine in general so it's best to use an attribute for the code generator's sake
    return routeTokens?.Contains(p.Name, StringComparer.Ordinal) != true
      && (!p.Type.IsComplex()
        // Byte arrays can be parsed from a query string parameter as base64 strings
        || p.Type is IArrayTypeSymbol { ElementType: { SpecialType: SpecialType.System_Byte } })
      ? p.Name
      : null;
  }

  public static IEnumerable<QueryParameter> Query(Method method)
  {
    var routeTokens = RouteTokens(method);
    return from p in method.symbol.Parameters
           let qname = IsQuery(p, routeTokens)
           where qname != null
           select new QueryParameter(p, qname);
  }

  private static bool IsBody(IParameterSymbol p)
  {
    foreach (var attr in p.GetAttributes())
    {
      var name = attr.AttributeClass!.Name;
      if (name.Equals("FromBodyAttribute", StringComparison.Ordinal)) return true;
      if (name.StartsWith("From", StringComparison.Ordinal)) return false; // Assuming a parameter can't have multiple FromAttribute sources
    }
    // If there's no attribute, ASP.NET 6 binds simple types from query and complex ones from body.
    // In theory, types with a converter than can Convert from String are also included in the previous list,
    // but this is hard to determine in general so it's best to use an attribute for the code generator's sake
    return
      p.Type.IsComplex()
      // Byte arrays can be parsed from a query string parameter as base64 strings
      && p.Type is not IArrayTypeSymbol { ElementType: { SpecialType: SpecialType.System_Byte } };
  }

  public static Parameter? Body(Method method)
  {
    var p = method.symbol.Parameters.FirstOrDefault(IsBody);
    return p == null ? null : new Parameter(p);
  }

  private static bool IsForm(IParameterSymbol p)
  {
    foreach (var attr in p.GetAttributes())
    {
      var name = attr.AttributeClass!.Name;
      if (name.Equals("FromFormAttribute", StringComparison.Ordinal)) return true;
      if (name.StartsWith("From", StringComparison.Ordinal)) return false; // Assuming a parameter can't have multiple FromAttribute sources
    }
    return false;
  }

  public static Parameter? Form(Method method)
  {
    var p = method.symbol.Parameters.FirstOrDefault(IsForm);
    return p == null ? null : new Parameter(p);
  }

  public static IEnumerable<Parameter> Parameters(Method method, params string[] sources)
  {
    sources = sources.Length == 0
      ? new[] { "FromRouteAttribute", "FromQueryAttribute", "FromBodyAttribute", "FromFormAttribute" }
      : Array.ConvertAll(sources, x => "From" + x + "Attribute");

    bool includeNoAttribute = sources.Contains("FromQueryAttribute");

    return from p in method.symbol.Parameters
           let fromAttr = p.GetAttributes().FirstOrDefault(a => a.AttributeClass!.Name.StartsWith("From", StringComparison.Ordinal))
           where fromAttr != null
            ? sources.Contains(fromAttr.AttributeClass!.Name)
            : includeNoAttribute
           select new Parameter(p);
  }

  private static readonly string[] verbs = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE", "CONNECT" };

  public static string Verb(Method method)
  {
    // HttpMethodAttributes: [HttpGet], [HttpPost], etc.
    var attr = method.symbol.GetAttributes()
                     .FirstOrDefault(a => a.AttributeClass is { BaseType.Name: "HttpMethodAttribute" });
    if (attr != null)
      return attr.AttributeClass!.Name switch
      {
        "HttpGetAttribute" => "GET",
        "HttpPostAttribute" => "POST",
        "HttpDeleteAttribute" => "DELETE",
        "HttpPutAttribute" => "PUT",
        "HttpHeadAttribute" => "HEAD",
        "HttpOptionsAttribute" => "OPTIONS",
        "HttpPatch" => "PATCH",
        string s => "Unknown attribute " + s,
      };

    // Convention: if name starts with an http verb, it's used.
    // If nothing matches ASP.NET default is POST
    return verbs.FirstOrDefault(x => method.Name.StartsWith(x, StringComparison.OrdinalIgnoreCase), "POST");
  }

  public class QueryParameter
  {
    private readonly IParameterSymbol symbol;

    public string QueryName { get; }
    public string ParameterName => symbol.Name;
    public RoslynType Type => new RoslynType(symbol.Type);

    public QueryParameter(IParameterSymbol symbol, string name)
    {
      this.symbol = symbol;
      QueryName = name;
    }
  }
}
