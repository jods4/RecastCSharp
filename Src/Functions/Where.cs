using RecastCSharp.CodeModel;
using Scriban.Runtime;
using System.Text.RegularExpressions;

namespace RecastCSharp.Functions;

class WhereBuiltin : ScriptObject
{
  public static IEnumerable<RoslynFile> DirectoryIs(IEnumerable<RoslynFile> models, string name) => models.Where(x => x.Directory == name);
  public static IEnumerable<RoslynFile> DirectoryIsNot(IEnumerable<RoslynFile> models, string name) => models.Where(x => x.Directory != name);
  public static IEnumerable<RoslynFile> DirectoryMatches(IEnumerable<RoslynFile> models, string pattern) => models.Where(x => Regex.IsMatch(x.Directory, pattern));
  public static IEnumerable<RoslynFile> DirectoryMatchesNot(IEnumerable<RoslynFile> models, string pattern) => models.Where(x => !Regex.IsMatch(x.Directory, pattern));

  public static IEnumerable<IName> NameIs(IEnumerable<object> models, string name) => models.Cast<IName>().Where(x => x.Name == name);
  public static IEnumerable<IName> NameIsNot(IEnumerable<object> models, string name) => models.Cast<IName>().Where(x => x.Name != name);
  public static IEnumerable<IName> NameMatches(IEnumerable<object> models, string pattern) => models.Cast<IName>().Where(x => Regex.IsMatch(x.Name, pattern));
  public static IEnumerable<IName> NameMatchesNot(IEnumerable<object> models, string pattern) => models.Cast<IName>().Where(x => !Regex.IsMatch(x.Name, pattern));

  public static IEnumerable<IModelSymbol> Public(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsPublic);
  public static IEnumerable<IModelSymbol> Protected(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsProtected);
  public static IEnumerable<IModelSymbol> Private(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsPrivate);
  public static IEnumerable<IModelSymbol> Abstract(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsAbstract);
  public static IEnumerable<IModelSymbol> Virtual(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsVirtual);
  public static IEnumerable<IModelSymbol> Override(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsOverride);
  public static IEnumerable<IModelSymbol> New(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => x.IsNew);

  public static IEnumerable<IModelSymbol> NotPublic(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsPublic);
  public static IEnumerable<IModelSymbol> NotProtected(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsProtected);
  public static IEnumerable<IModelSymbol> NotPrivate(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsPrivate);
  public static IEnumerable<IModelSymbol> NotAbstract(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsAbstract);
  public static IEnumerable<IModelSymbol> NotVirtual(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsVirtual);
  public static IEnumerable<IModelSymbol> NotOverride(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsOverride);
  public static IEnumerable<IModelSymbol> NotNew(IEnumerable<object> models) => models.Cast<IModelSymbol>().Where(x => !x.IsNew);

  public static IEnumerable<IModelSymbol> Attribute(IEnumerable<object> models, string attr) => models.Cast<IModelSymbol>().Where(x => x.HasAttribute(attr));
  public static IEnumerable<IModelSymbol> NotAttribute(IEnumerable<object> models, string attr) => models.Cast<IModelSymbol>().Where(x => !x.HasAttribute(attr));

  public static IEnumerable<Class> Implements(IEnumerable<Class> classes, string interfaceName) => classes.Where(c => c.Implements(interfaceName));
  public static IEnumerable<Class> ImplementsNot(IEnumerable<Class> classes, string interfaceName) => classes.Where(c => !c.Implements(interfaceName));
  public static IEnumerable<Class> DirectlyImplements(IEnumerable<Class> classes, string interfaceName) => classes.Where(c => c.DirectlyImplements(interfaceName));
  public static IEnumerable<Class> DirectlyImplementsNot(IEnumerable<Class> classes, string interfaceName) => classes.Where(c => !c.DirectlyImplements(interfaceName));
}