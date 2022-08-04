using System.Text.RegularExpressions;

namespace RecastCSharp.Functions;

public static class StringExtensions
{
  public static string Camelcase(this string name) => char.ToLower(name[0]) + name[1..];
  // Camecase variant that handles SNAKE_CASE better
  public static string CamelcaseEx(this string name) => Regex.Replace(name, @"(?<=^|_)[A-Z]|(?<=[A-Z])[A-Z](?![a-z])", match => match.Value.ToLower());
}
