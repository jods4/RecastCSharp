using Scriban.Runtime;
using System.Collections;

namespace RecastCSharp.Functions;

public class IterBuiltin : ScriptObject
{
  public static object First(IEnumerable src) => src.Cast<object>().First();
  public static object? FirstOrDefault(IEnumerable src) => src.Cast<object>().FirstOrDefault();
  public static object Single(IEnumerable src) => src.Cast<object>().Single();
  public static object? SingleOrDefault(IEnumerable src) => src.Cast<object>().SingleOrDefault();

  public static object[] ToArray(IEnumerable src) => src.Cast<object>().ToArray();
}