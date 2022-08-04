using Scriban.Runtime;
using System.Text;

namespace RecastCSharp.Functions;

public class UrlBuiltin : ScriptObject
{
  public static string Join(params string?[] fragments)
  {
    var sb = new StringBuilder();
    foreach (var fragment in fragments)
    {
      if (string.IsNullOrEmpty(fragment)) continue;
      if (fragment.StartsWith("/"))
        sb.Clear().Append(fragment);
      else
        sb.Append('/').Append(fragment);
    }
    return sb.ToString();
  }
}
