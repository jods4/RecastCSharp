namespace RecastCSharp.CodeModel;

public class Code
{
  private readonly List<Project> projects;

  public Code(List<Project> projects)
  {
    this.projects = projects;
  }

  public IEnumerable<Project> Projects => projects;

  public IEnumerable<Class> Classes => projects.SelectMany(p => p.Classes);

  public IEnumerable<Namespace> Namespaces
    => from node in projects.SelectMany(p => p.NamespaceNodes)
       group node by node.Name into g
       select new Namespace(g.Key, g);

  public IEnumerable<RoslynFile> Files => projects.SelectMany(p => p.Files);
}