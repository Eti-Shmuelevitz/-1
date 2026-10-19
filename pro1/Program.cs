
using System.CommandLine;
using System.Diagnostics.SymbolStore;
var rootCommand = new RootCommand();
var bundlecommand = new Command("bundle", "Bundle code files to a single file");
rootCommand.Subcommands.Add(bundlecommand);

var languageOption = new Option<string[]>("--language", "-l")
{
    Description = "Programming languages to include in the bundle",
    Required = true
}; 
bundlecommand.Options.Add(languageOption);
var output = new Option<string>("--output", "-o")
{
    Description ="Name or path for the output bundle file"
};
bundlecommand.Options.Add(output);
string[] allLanguages = { "c#", "python", "java", "html" };
Dictionary<string, string> languageExtensions = new()
{
    { "c#", ".cs" },
    { "python", ".py" },
    { "java", ".iml" },
    { "html", ".html" }
};
var note = new Option<bool>("--note", "-n")
{
    Description = "Add source file name and relative path as a comment"
};
bundlecommand.Options.Add(note);
var sort = new Option<string>("--sort", "-s")
{
    Description = "Sort files by name or type"

};
sort.DefaultValueFactory = _ => "name";
bundlecommand.Add(sort);
var removeLines = new Option<bool>("--remove-empty-lines", "-r")
{
    Description="remove empty lines"
};
bundlecommand.Add(removeLines);
var author = new Option<string>("--author", "-a")
{
    Description = "Write the author name in the bundle"
};
bundlecommand.Add(author);  
bundlecommand.SetAction((parseResult) => {
    string[] l = parseResult.GetValue(languageOption)  ?? Array.Empty<string>();
    if (l.Length == 0 || l.All(string.IsNullOrWhiteSpace))
    {
        Console.WriteLine("Please provide at least one language.");
        return;
    }

    string? outputPath = parseResult.GetValue(output);

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Please provide an output path.");
        return;
    }
    

    string fullOutputPath;

    if (Path.IsPathFullyQualified(outputPath))
    {
        fullOutputPath = outputPath;
    }
    else
    {
        fullOutputPath = Path.Combine(
            Environment.CurrentDirectory,
            outputPath);
    }
    string current = Environment.CurrentDirectory;
    string[] files = Directory.GetFiles(current);
    List<string> selectedFiles = new List<string>();
    string[] ends =
    parseResult.GetValue(languageOption) ?? Array.Empty<string>();
    List<string> language = ends
    .SelectMany(s => s.Split(','))
    .Select(s => s.Trim())
    .ToList();
    foreach (string lang in language)
    {
        if (lang != "all" && !languageExtensions.ContainsKey(lang))
        {
            Console.WriteLine($"Unknown language: {lang}");
            return;
        }
    }
    bool isnote = parseResult.GetValue(note);
    string? wSort = parseResult.GetValue(sort);

    if (language.Contains("all"))
    {
        foreach (string file in files)
        {
            if (Path.GetFullPath(file) == Path.GetFullPath(fullOutputPath))
            {
                continue;
            }
            string x = Path.GetExtension(file);
            if (languageExtensions.ContainsValue(x))
            {
                selectedFiles.Add(file);
            }
        }
    }

    else
    {
        foreach (string file in files)
        {
            if (Path.GetFullPath(file) == Path.GetFullPath(fullOutputPath))
            {
                continue;
            }
            string x = Path.GetExtension(file);

            foreach (string lang in language)
            {
                if (languageExtensions.ContainsKey(lang) && languageExtensions[lang] == x)
                {
                    selectedFiles.Add(file);
                    break;
                }
            }
        }
    }
    if (language.Contains("all") && language.Count > 1)
    {
        Console.WriteLine("'all' cannot be combined with other languages.");
        return;
    }
  
    if (wSort=="name")
    {
        selectedFiles=selectedFiles.OrderBy(s => Path.GetFileName(s)).ToList();
    }
    else if (wSort=="type")
    {
        selectedFiles=selectedFiles.OrderBy(s => Path.GetExtension(s)).ThenBy(s => Path.GetFileName(s)).ToList();
    }
    else 
    {
        Console.WriteLine("the sort kategory is invalid");
        return;
    }

    bool isRemove = parseResult.GetValue(removeLines);
    string? authorName = parseResult.GetValue(author);

    string bundleContent = "";
    if (!string.IsNullOrWhiteSpace(authorName))
    {
        bundleContent += $"// Author: {authorName}";
        bundleContent += Environment.NewLine;
    }
    foreach (string selectedFile in selectedFiles)
    {
        if (isnote)
        {
            string fileName = Path.GetFileName(selectedFile);
            string relativePath = Path.GetRelativePath(current, selectedFile);

            bundleContent += $"// Source: {fileName} | Path: {relativePath}";
            bundleContent += Environment.NewLine;
        }
        string content = File.ReadAllText(selectedFile);
        if (isRemove)
        {
            content = string.Join(
                Environment.NewLine,
                content.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.None
                ).Where(line => !string.IsNullOrWhiteSpace(line))
            );
        }

        bundleContent += content;
        bundleContent += Environment.NewLine;
    }


    try
        {
           File.WriteAllText(fullOutputPath, bundleContent);
           Console.WriteLine($"Bundle saved to: {fullOutputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not save the bundle: {ex.Message}");
        }
});

var createRspCommand = new Command("create-rsp", "Create a response file");
rootCommand.Subcommands.Add(createRspCommand);
createRspCommand.SetAction((parseResult) =>
{
    Console.Write("Enter the language(s): ");
    string? language = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(language))
    {
        Console.WriteLine("Language is required.");
        return;
    }
    string[] requestedLanguages = language
    .Split(',')
    .Select(s => s.Trim())
    .ToArray();
    if (requestedLanguages.Contains("all") && requestedLanguages.Length > 1)
    {
        Console.WriteLine("'all' cannot be combined with other languages.");
        return;
    }
    foreach (string lang in requestedLanguages)
    {
        if (lang != "all" && !languageExtensions.ContainsKey(lang))
        {
            Console.WriteLine($"Unknown language: {lang}");
            return;
        }
    }
    Console.Write("Enter the output file name: ");
    string? output = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("Output file name is required.");
        return;
    }
    Console.Write("Add source notes? (yes/no): ");
    string? note = Console.ReadLine();

    Console.Write("Sort by name or type: ");
    string? sort = Console.ReadLine();
    if (sort != "name" && sort != "type")
    {
        Console.WriteLine("Please enter 'name' or 'type'.");
        return;
    }
    Console.Write("Remove empty lines? (yes/no): ");
    string? removeEmptyLines = Console.ReadLine();

    Console.Write("Enter the author name (or leave empty): ");
    string? author = Console.ReadLine();

    List<string> arguments = new List<string>();
    arguments.Add("bundle");
    arguments.Add("--language");
    arguments.Add(language);
    if (!string.IsNullOrWhiteSpace(output))
    {
        arguments.Add("--output");
        arguments.Add(output);
    }
    if (note?.Trim().ToLower() == "yes")
    {
        arguments.Add("--note");
    }
    else if (note?.Trim().ToLower() != "no")
    {
        Console.WriteLine("Please enter yes or no.");
        return;
    }
    if (!string.IsNullOrWhiteSpace(sort))
    {
        arguments.Add("--sort");
        arguments.Add(sort);
    }
    if (removeEmptyLines?.Trim().ToLower() == "yes")
    {
        arguments.Add("--remove-empty-lines");
    }
    else if (removeEmptyLines?.Trim().ToLower() != "no")
    {
        Console.WriteLine("Please enter yes or no.");
        return;
    }
    if (!string.IsNullOrWhiteSpace(author))
    {
        arguments.Add("--author");
        arguments.Add(author);
    }
    Console.Write("Enter response file name: ");
    string? responseFileName = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(responseFileName))
    {
        Console.WriteLine("Response file name is required.");
        return;
    }
    try
    {
        string[] responseLines = arguments.Select(arg =>
           arg.Contains(' ') ? $"\"{arg}\"" : arg
           ).ToArray();

        File.WriteAllLines(responseFileName, responseLines);
        Console.WriteLine($"Response file saved to: {responseFileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not save response file: {ex.Message}");
    }
});





rootCommand.Parse(args).Invoke();