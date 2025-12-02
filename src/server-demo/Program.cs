using System.Text.Json;
using System.Text.Json.Nodes;

var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("/appsettings", () =>
{
    var path = "appsettings.json";
    if (!File.Exists(path)) return Results.Problem("appsettings.json not found", statusCode:500);
    
    var node = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
    if (node == null) return Results.Json(new { });

    var flat = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    void Walk(string p, JsonNode? n)
    {
        if (n is JsonObject o)
        {
            foreach (var kv in o)
                Walk(string.IsNullOrEmpty(p) ? kv.Key : p + ":" + kv.Key, kv.Value);
        }
        else if (n is JsonArray a)
            flat[p] = JsonSerializer.Serialize(a);
        else
            flat[p] = n?.ToString() ?? "";
    }

    Walk("", node);
    return Results.Json(flat);
});

app.MapGet("/health", () => "ok");

app.Run();