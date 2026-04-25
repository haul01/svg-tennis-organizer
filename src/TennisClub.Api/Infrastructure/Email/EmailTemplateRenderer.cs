using System.Collections.Concurrent;
using System.Text;
using Scriban;
using Scriban.Runtime;

namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// Renders Scriban-based email templates from
/// Infrastructure/Email/Templates/{name}.sbn. Templates are loaded once
/// per process and cached. Models are renamed with snake_case so
/// PascalCase C# properties match `{{ first_name }}` in templates.
/// </summary>
public sealed class EmailTemplateRenderer
{
    private readonly string _templateDir;
    private readonly ConcurrentDictionary<string, Template> _cache = new();

    public EmailTemplateRenderer()
    {
        _templateDir = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure", "Email", "Templates");
    }

    public async Task<string> RenderAsync<T>(string templateName, T model, CancellationToken ct = default)
    {
        var template = _cache.GetOrAdd(templateName, LoadTemplate);

        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: m => ToSnakeCase(m.Name));
        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        context.CancellationToken = ct;
        return await template.RenderAsync(context);
    }

    private Template LoadTemplate(string name)
    {
        var path = Path.Combine(_templateDir, $"{name}.sbn");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Email template '{name}.sbn' not found at {path}.", path);
        }
        var source = File.ReadAllText(path, Encoding.UTF8);
        var parsed = Template.Parse(source, path);
        if (parsed.HasErrors)
        {
            throw new InvalidOperationException(
                $"Failed to parse email template '{name}': "
                + string.Join("; ", parsed.Messages.Select(m => m.ToString())));
        }
        return parsed;
    }

    private static string ToSnakeCase(string name)
    {
        // Simple PascalCase -> snake_case ("FirstName" -> "first_name")
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c)) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
