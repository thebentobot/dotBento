using dotBento.CommandDocs;

if (args.Length != 3 ||
    (args[0], args[1]) is not (("export", "--output") or ("check", "--input")))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/dotBento.CommandDocs -- <export --output|check --input> <path>");
    return 2;
}

var manifest = ManifestJson.Serialize(await CommandManifestBuilder.BuildAsync());
var path = Path.GetFullPath(args[2]);

if (args[0] == "export")
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await File.WriteAllTextAsync(path, manifest);
    Console.WriteLine($"Wrote {path}");
    return 0;
}

if (!File.Exists(path) || await File.ReadAllTextAsync(path) != manifest)
{
    Console.Error.WriteLine($"{path} is stale. Run the exporter and commit the result.");
    return 1;
}

Console.WriteLine($"{path} is current.");
return 0;
