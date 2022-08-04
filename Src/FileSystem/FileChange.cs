namespace RecastCSharp.FileSystem;

struct FileChange
{
  public string Filename;
  public ChangeType Change;
}

enum ChangeType { Added, Changed, Removed }