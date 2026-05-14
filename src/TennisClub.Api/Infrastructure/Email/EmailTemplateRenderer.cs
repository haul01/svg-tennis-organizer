using System.Collections.Concurrent;
using System.Text;
using Scriban;
using Scriban.Runtime;

namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// Rendered email body in both formats. Plain may be null when no
/// matching .txt.sbn template exists; the sender will then ship the
/// mail as HTML-only.
/// </summary>
public sealed record RenderedEmail(string Html, string? Plain);

/// <summary>
/// Renders Scriban-based email templates from
/// Infrastructure/Email/Templates/{name}.sbn. Templates are loaded once
/// per process and cached. Models are renamed with snake_case so
/// PascalCase C# properties match `{{ first_name }}` in templates.
///
/// Each logical email has two files:
///   {name}.sbn      - HTML body (required)
///   {name}.txt.sbn  - plain text body (optional, but boosts deliverability)
/// </summary>
public sealed class EmailTemplateRenderer
{
    private readonly string _templateDir;
    private readonly ConcurrentDictionary<string, Template> _cache = new();
    // Sentinel for "we looked, the .txt.sbn file does not exist" so we
    // don't hit the filesystem on every render.
    private readonly ConcurrentDictionary<string, bool> _plainExists = new();

    public EmailTemplateRenderer()
    {
        _templateDir = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure", "Email", "Templates");
    }

    /// <summary>
    /// Render the HTML body of an email template. Throws if {name}.sbn
    /// is missing or fails to parse.
    /// </summary>
    public Task<string> RenderAsync<T>(string templateName, T model, CancellationToken ct = default) =>
        RenderTemplateAsync(templateName, model, ct);

    /// <summary>
    /// Render both the HTML body and (when a {name}.txt.sbn sibling
    /// exists) the plain text body in one call.
    /// </summary>
    public async Task<RenderedEmail> RenderEmailAsync<T>(string templateName, T model, CancellationToken ct = default)
    {
        var html = await RenderTemplateAsync(templateName, model, ct);

        var plainName = $"{templateName}.txt";
        if (!_plainExists.TryGetValue(plainName, out var exists))
        {
            exists = File.Exists(Path.Combine(_templateDir, $"{plainName}.sbn"));
            _plainExists[plainName] = exists;
        }

        var plain = exists
            ? await RenderTemplateAsync(plainName, model, ct)
            : null;
        return new RenderedEmail(html, plain);
    }

    private async Task<string> RenderTemplateAsync<T>(string name, T model, CancellationToken ct)
    {
        var template = _cache.GetOrAdd(name, LoadTemplate);

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
