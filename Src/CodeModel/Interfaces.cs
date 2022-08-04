namespace RecastCSharp.CodeModel;

public interface IName
{
  string Name { get; }
}

public interface IModelSymbol : IName
{
  bool IsAbstract { get; }
  bool IsVirtual { get; }
  bool IsOverride { get; }
  bool IsNew { get; }
  bool IsPublic { get; }
  bool IsPrivate { get; }
  bool IsProtected { get; }

  bool HasAttribute(string name);
}