using System.Text.Json;
using System.Text.Json.Nodes;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var opts = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--")) continue;
                var key = args[i].Substring(2);
                opts[key] = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : "true";
            }

            if (!opts.TryGetValue("local", out var localPath) || string.IsNullOrWhiteSpace(localPath))
            {
                Console.Error.WriteLine("Missing --local <path>");
                return 2;
            }

            opts.TryGetValue("remote-file", out var remoteFile);
            opts.TryGetValue("remote-url", out var remoteUrl);

            if (remoteFile == null && remoteUrl == null)
            {
                Console.Error.WriteLine("Missing --remote-file or --remote-url");
                return 2;
            }

            JsonObject localJson;
            {
                await using var fs = File.OpenRead(localPath);
                var node = await JsonNode.ParseAsync(fs);
                localJson = node?.AsObject() ?? new JsonObject();
            }

            JsonObject remoteJson;
            if (remoteFile != null)
            {
                await using var fs = File.OpenRead(remoteFile);
                var node = await JsonNode.ParseAsync(fs);
                remoteJson = node?.AsObject() ?? new JsonObject();
            }
            else
            {
                using var http = new HttpClient();
                var text = await http.GetStringAsync(remoteUrl!);
                var node = JsonNode.Parse(text);
                remoteJson = node?.AsObject() ?? new JsonObject();
            }

            var left = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var right = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void Flat(string prefix, JsonNode? n, Dictionary<string, string> bag)
            {
                if (n is JsonObject o)
                {
                    foreach (var kv in o)
                    {
                        var p = string.IsNullOrEmpty(prefix) ? kv.Key : prefix + ":" + kv.Key;
                        Flat(p, kv.Value, bag);
                    }
                }
                else if (n is JsonArray arr)
                {
                    bag[prefix] = JsonSerializer.Serialize(arr);
                }
                else
                {
                    bag[prefix] = n?.ToString() ?? "";
                }
            }

            Flat("", localJson, left);
            Flat("", remoteJson, right);

            var allKeys = new HashSet<string>(left.Keys.Concat(right.Keys), StringComparer.OrdinalIgnoreCase);
            var diffs = new List<(string Key, string Local, string Remote)>();

            foreach (var k in allKeys)
            {
                left.TryGetValue(k, out var lv);
                right.TryGetValue(k, out var rv);
                lv ??= "<MISSING>";
                rv ??= "<MISSING>";
                if (lv != rv) diffs.Add((k, lv, rv));
            }

            if (diffs.Count == 0)
            {
                Console.WriteLine("AppSettings are the SAME.");
                return 0;
            }

            Console.WriteLine("AppSettings DIFFER:");
            foreach (var d in diffs)
            {
                Console.WriteLine($"- {d.Key}");
                Console.WriteLine($"    Local : {d.Local}");
                Console.WriteLine($"    Remote: {d.Remote}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
