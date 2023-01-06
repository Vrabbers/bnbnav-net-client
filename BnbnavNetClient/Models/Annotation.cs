using System.Text.Json;

namespace BnbnavNetClient.Models;
public sealed record Annotation(string Id, JsonElement Data);