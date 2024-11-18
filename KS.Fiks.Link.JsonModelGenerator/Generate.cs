using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

Generator.Generate(args[0], args[1]);

static class Generator
{
    const string commonNamespace = "KS.Fiks.Link.V1";
    const string feilmeldingSubNamespace = "feilmelding";
    const string innsynSubNamespace = "innsyn";
    const string fellesSubNamespace = "felles";
    const string namespaceSuffix = "Typer";
    const string fellesNamespace = $"{commonNamespace}.{fellesSubNamespace}";

    private static IEnumerable<string>? allFellesSchemas;

    private static readonly string[] fellesFilenamesSorted = new []
    {
        "no.ks.fiks.Link.v1.felles.adresse.schema.json",
        "no.ks.fiks.Link.v1.felles.personid.schema.json",
        "no.ks.fiks.Link.v1.felles.organisasjonid.schema.json",
        "no.ks.fiks.Link.v1.felles.matrikkelnummer.schema.json",
        "no.ks.fiks.Link.v1.felles.kontakt.schema.json",
        "no.ks.fiks.Link.v1.felles.eier.schema.json",
        "no.ks.fiks.Link.v1.felles.eiendom.schema.json",
        "no.ks.fiks.Link.v1.felles.flate.schema.json",
        "no.ks.fiks.Link.v1.felles.posisjon.schema.json",
    };

    public static void Generate(string sourcePath, string outputFolder)
    {
        var schemaFolder = new DirectoryInfo(sourcePath);
        
        allFellesSchemas = schemaFolder
             .GetFiles()
             .Where(file => file.Extension.Equals(".json") && file.Name.Contains(".felles.")) 
             .Select(file => Path.Combine(sourcePath, file.Name));

        var fellesSchemasToGenerateSorted = fellesFilenamesSorted.Select(filename => Path.Combine(sourcePath, filename)).ToList();

        var schemasToGenerate = schemaFolder
            .GetFiles()
            .Where(file => file.Extension.Equals(".json") && ! file.Name.Contains(".felles."))
            .Select(file => Path.Combine(sourcePath, file.Name));

        var fellesTypes = GetTypes(fellesSchemasToGenerateSorted);

        GenerateClasses(outputFolder, fellesSchemasToGenerateSorted, fellesTypes);
        GenerateClasses(outputFolder, schemasToGenerate, fellesTypes);
    }

    private static HashSet<string> GetNamespacesForType(IEnumerable<string>? schemas, string typeName)
    {
        var foundNamespaces = new HashSet<string>();

        foreach (var schemaFilename in schemas)
        {
            var namespacePrefix = GetNamespacePrefix(schemaFilename);
            var schemaFile =
                JsonSchema.FromFileAsync(schemaFilename).Result;
            var classFilename = GetClassName(schemaFilename, fellesSubNamespace);
            var fullNamespace = GetFullNamespace(namespacePrefix, classFilename);

            var generator = new CSharpGenerator(schemaFile)
            {
                Settings =
                {
                    Namespace = $"{commonNamespace}.{namespacePrefix}.{classFilename}",
                    ClassStyle = CSharpClassStyle.Poco,
                    TypeNameGenerator = new MyTypeNameGenerator(),
                    GenerateOptionalPropertiesAsNullable = true
                }
            };

            // Need to do this in order to get the types. Dont remove
            var classAsString = generator.GenerateFile();

            var types = generator.GenerateTypes();

            foreach (var codeArtifact in types)
            {
                if (typeName.Equals(codeArtifact.TypeName))
                {
                    Console.Out.WriteLine($"Found namespace {fullNamespace} for typename: {codeArtifact.TypeName}");
                    foundNamespaces.Add(fullNamespace);
                }
            }
        }
        return foundNamespaces;
    }

    private static string GetFullNamespace(string namespacePrefix, string classFilename)
    {
        if (string.IsNullOrEmpty(namespacePrefix))
        {
            return $"{commonNamespace}.{classFilename}{namespaceSuffix}";    
        }
        return $"{commonNamespace}.{namespacePrefix}.{classFilename}{namespaceSuffix}";
    }

    private static HashSet<string> GetTypes(IEnumerable<string> schemasToGenerate)
    {
        var typenames = new HashSet<string>();
        foreach (var schemaFilename in schemasToGenerate)
        {
            Console.WriteLine($"Finding types for {schemaFilename}");

            var namespacePrefix = fellesSubNamespace;
            if (!File.Exists(schemaFilename))
            {
                Console.Error.WriteLine($"Error: Schemafile {schemaFilename} missing. Cant resolve types");
                continue;
            }

            var schemaFile = JsonSchema.FromFileAsync(schemaFilename).Result;
                
            var classFilename = GetClassName(schemaFilename, fellesSubNamespace);
            
            var generator = new CSharpGenerator(schemaFile)
            {
                Settings =
                {
                    Namespace = $"{commonNamespace}.{namespacePrefix}.{classFilename}",
                    ClassStyle = CSharpClassStyle.Poco,
                    TypeNameGenerator = new MyTypeNameGenerator(),
                }
            };

            // Need to do this in order to get the types. Dont remove
            var classAsString = generator.GenerateFile();

            var types = generator.GenerateTypes();

            foreach (var codeArtifact in types)
            {
                //Console.Out.WriteLine($"Found typename: {codeArtifact.TypeName}");
                typenames.Add(codeArtifact.TypeName);
            }
        }

        return typenames;
    }

    private static List<string> GenerateClasses(string outputFolder, IEnumerable<string> schemasToGenerate, IReadOnlySet<string> usingTypes = null)
    {
        bool isFelles;
        var generatedTypes = new HashSet<string>();
        var generatedClassNamespaces = new List<string>();
        foreach (var schemaFilename in schemasToGenerate)
        {
            Console.WriteLine($"Generating code based on json schema {schemaFilename}");

            if (!File.Exists(schemaFilename))
            {
                Console.Error.WriteLine($"Error: Schemafile {schemaFilename} missing. Cant generate classes");
                continue;
            }


            var schemaFile =
                JsonSchema.FromFileAsync(schemaFilename).Result;

            var namespacePrefix = GetNamespacePrefix(schemaFilename);
            var classFilename = GetClassName(schemaFilename, namespacePrefix);
            isFelles = namespacePrefix == fellesSubNamespace;

            var fullNamespace = GetFullNamespace(namespacePrefix, classFilename);
            
            generatedClassNamespaces.Add(fullNamespace);

            var generator = new CSharpGenerator(schemaFile)
            {
                Settings =
                {
                    Namespace = fullNamespace,
                    ClassStyle = CSharpClassStyle.Poco,
                    TypeNameGenerator = new MyTypeNameGenerator(),
                    GenerateOptionalPropertiesAsNullable = true
                }
            };

            Directory.CreateDirectory($"./{outputFolder}/Models/{namespacePrefix}/{classFilename}{namespaceSuffix}/");

            // Need to do this in order to get the types. Dont remove
            var classAsString = generator.GenerateFile();
           
            var types = generator.GenerateTypes();

            var usingCode = "";
            var usingsSet = new HashSet<string>(); 
            foreach (var codeArtifact in types)
            {
                // Add using to all code artifacts
                if (usingTypes != null && usingTypes.Contains(codeArtifact.TypeName))
                {
                    Console.Out.WriteLine($"Add using to class because {codeArtifact.TypeName} found in {fellesNamespace} namespace and is already generated");
                    
                    usingsSet.UnionWith(GetNamespacesForType(allFellesSchemas, codeArtifact.TypeName));
                }
            }

            foreach (var distinctUsing in usingsSet)
            {
                usingCode += $"using {distinctUsing};\n";
            }

            usingCode += "\n\n";

            foreach (var codeArtifact in types)
            {
                var code = "";
     
                if (isFelles && generatedTypes.Contains(codeArtifact.TypeName))
                {
                    Console.Out.WriteLine($"Skipping already generated type {codeArtifact.TypeName} in this generate run through the {fellesNamespace} namespace.");
                    continue;
                } // Skip generate this type since it is already created in the "felles" namespace. Include it with using instead.
                else if (!isFelles && usingTypes != null && usingTypes.Contains(codeArtifact.TypeName))
                {
                    Console.Out.WriteLine($"Skipping already generated type {codeArtifact.TypeName} in the {fellesNamespace}. Include instead through {fellesNamespace} namespace");
                    continue;
                }

                code = $"{usingCode}" +
                       $"namespace {GetFullNamespace(namespacePrefix, classFilename)} {{\n" +
                       "#pragma warning disable // Disable all warnings\n\n" +
                       $"{codeArtifact.Code.Replace(" partial ", " ")}\n" +
                       "}";

                File.WriteAllText(
                    $"./{outputFolder}/Models/{namespacePrefix}/{classFilename}{namespaceSuffix}/{ToUpper(codeArtifact.TypeName)}.cs", code);

                generatedTypes.Add(codeArtifact.TypeName);
            }
        }

        return generatedClassNamespaces;
    }

    private static string GetNamespacePrefix(string schemaFilename)
    {
        var namespacePrefix = "";
        if (schemaFilename.Contains($".{feilmeldingSubNamespace}."))
        {
            namespacePrefix = feilmeldingSubNamespace;
        }
        else if (schemaFilename.Contains($".{innsynSubNamespace}."))
        {
            namespacePrefix = innsynSubNamespace;
        }
        else if (schemaFilename.Contains($".{fellesSubNamespace}."))
        {
            namespacePrefix = fellesSubNamespace;
        }

        return namespacePrefix;
    }

    public class MyTypeNameGenerator : ITypeNameGenerator
    {
        public string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            return !string.IsNullOrEmpty(schema.Title) ? schema.Title : typeNameHint;
        }
    }

    static string GetClassName(string schemaFilename, string namespaceName)
    {
        return string.Join("",
            schemaFilename.Split($".{namespaceName}.")[1].Replace(".schema.json", "").Split('.')
                .Select(ToUpper));
    }

    static string ToUpper(string input)
    {
        return char.ToUpper(input[0]) + input[1..];
    }
}