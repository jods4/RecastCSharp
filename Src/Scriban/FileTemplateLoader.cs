using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.IO;
using System.Threading.Tasks;

namespace RecastCSharp.Scriban;

class FileTemplateLoader : ITemplateLoader
{
  public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    => Path.Combine(Path.GetDirectoryName(callerSpan.FileName)!, templateName);

  public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    => File.ReadAllText(templatePath);

  public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    => ValueTask.FromResult(Load(context, callerSpan, templatePath));
}