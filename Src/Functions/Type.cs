using Microsoft.CodeAnalysis;
using RecastCSharp.CodeModel;
using Scriban.Runtime;

namespace RecastCSharp.Functions;

public class TypeBuiltin : ScriptObject
{
  public static bool HasAttr(object target, string name)
  {
    return target switch
    {
      Class c => c.HasAttribute(name),
      Method m => m.HasAttribute(name),
      Property p => p.HasAttribute(name),
      EnumValue e => e.HasAttribute(name),
      Field f => f.HasAttribute(name),
      _ => false,
    };
  }

  public static RoslynAttribute? Attr(object target, string name)
  {
    return target switch
    {
      Class c => c.Attribute(name),
      Method m => m.Attribute(name),
      Property p => p.Attribute(name),
      EnumValue e => e.Attribute(name),
      Field f => f.Attribute(name),
      _ => null,
    };
  }

  // Note that `name` is a MetadataName so we can check for specific generic types with suffixes, e.g. IEnumerable`1
  public static RoslynType? Implements(RoslynType type, string name)
  {
    // Check if the symbol is the requested interface itself
    if (type.MetadataName == name) return type;

    // Otherwise check if it implements the requested interface
    var implemented = type.Interfaces.FirstOrDefault(i => i.MetadataName == name);
    return implemented != null ? new RoslynType(implemented) : null;
  }

  public static RoslynType? Argument(RoslynType? type, int position)
  {
    var argument = type?.TypeArguments?[position];
    return argument != null ? new RoslynType(argument) : null;
  }

  private static readonly Queue<RoslynType> queue = new();
  private static readonly HashSet<RoslynType> seenTypes = new();

  public static IEnumerable<RoslynType> Queue()
  {
    while (queue.TryDequeue(out var next))
      yield return next;
  }

  public static string Enqueue(RoslynType type)
  {
    if (type.IsGeneric)
      type = type.ConstructedFrom;

    if (seenTypes.Add(type))
      queue.Enqueue(type);
    return type.Name;
  }

  public static void ClearQueue()
  {
    seenTypes.Clear();
    RoslynType.ClearAnonymousNames();
  }

  public static HashSet<RoslynType> NewSet() => new HashSet<RoslynType>();

  public static bool Add(HashSet<RoslynType> set, RoslynType value) => set.Add(value);

  public static void Clear(HashSet<RoslynType> set) => set.Clear();

  public static IEnumerable<RoslynType> SortByName(IEnumerable<RoslynType> types) => types.OrderBy(t => t.Name);
}
