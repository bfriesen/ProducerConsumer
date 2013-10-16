<Query Kind="Program">
  <NuGetReference>ManyConsole</NuGetReference>
  <Namespace>ManyConsole</Namespace>
  <Namespace>NDesk.Options</Namespace>
</Query>

#define NONEST
#if CMD
int
#else
void
#endif
Main(string[] args)
{
#if !CMD
    string rawArgs;
    while (!string.IsNullOrEmpty(rawArgs = Util.ReadLine("Enter command-line args, or enter nothing to quit.", "-n")))
    {
        args =
            Regex.Matches(rawArgs, @"""((?:[^""]|"""")*)""|(\S+)")
            .Cast<Match>()
            .Select(
                m =>
                m.Groups
                    .Cast<Group>()
                    .Skip(1)
                    .Where(g => g.Success)
                    .Select(g => g.Value.Replace(@"""""", @""""))
                    .First())
            .ToArray();    
#endif

        var commands = ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(this.GetType());
#if CMD
        return
#endif
            ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
#if !CMD
    }
#endif
}

public class BuildCommand : ConsoleCommand
{
    public BuildCommand()
    {
        this.IsCommand("Build");

        this.HasOption("n|nuget", "Whether to build nuget package.", b => BuildNuget = true);
    }

    public bool BuildNuget { get; private set; }

    private static readonly string projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Util.CurrentQueryPath));
    
    private static readonly string assemblyInfoPath = Path.Combine(projectRoot, @"src\Properties\AssemblyInfo.cs");
    private static readonly string dotNet35NuspecPath = Path.Combine(projectRoot, @"nuget\RandomSkunk.ProducerConsumer.nuspec");
    private static readonly string portableNuspecPath = Path.Combine(projectRoot, @"nuget\RandomSkunk.ProducerConsumer.Portable.nuspec");
    
    private static readonly string solutionPath = Path.Combine(projectRoot, @"src\ProducerConsumer.sln");

    public override int Run(string[] remainingArguments)
    {
        Version dotNet35NuspecVersion = null;
        Version portableNuspecVersion = null;
        
        if (BuildNuget)
        {
            var assemblyInfo = File.ReadAllText(assemblyInfoPath);
            var dotNet35Nuspec = File.ReadAllText(dotNet35NuspecPath);
            var portableNuspec = File.ReadAllText(portableNuspecPath);
            
            const string assemblyAttributePattern = @"\[\s*assembly\s*:\s*AssemblyFileVersion\s*\(\s*""(\d+\.\d+\.\d+\.\d+)""\s*\)\s*]";
            
            var dotNet35NuspecVersionElement =
                XDocument.Parse(dotNet35Nuspec)
                    .Root
                    .Element(XName.Get("metadata", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"))
                    .Element(XName.Get("version", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"));            
            
            var portableNuspecVersionElement =
                XDocument.Parse(portableNuspec)
                    .Root
                    .Element(XName.Get("metadata", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"))
                    .Element(XName.Get("version", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"));
            
            var assemblyInfoVersion = new Version(Regex.Match(assemblyInfo, assemblyAttributePattern).Groups[1].Value);
            dotNet35NuspecVersion = new Version(dotNet35NuspecVersionElement.Value);
            portableNuspecVersion = new Version(portableNuspecVersionElement.Value);            
            
            assemblyInfoVersion.Build++;
            dotNet35NuspecVersion.Build++;
            portableNuspecVersion.Build++;
            
            dotNet35NuspecVersionElement.Value = dotNet35NuspecVersion.ToString();
            portableNuspecVersionElement.Value = portableNuspecVersion.ToString();
            
            assemblyInfo = Regex.Replace(assemblyInfo, assemblyAttributePattern, match => string.Format(@"[assembly: AssemblyFileVersion(""{0}"")]", assemblyInfoVersion));
            dotNet35Nuspec = dotNet35NuspecVersionElement.Document.ToString();
            portableNuspec = portableNuspecVersionElement.Document.ToString();
            
            File.WriteAllText(assemblyInfoPath, assemblyInfo);
            File.WriteAllText(dotNet35NuspecPath, dotNet35Nuspec);
            File.WriteAllText(portableNuspecPath, portableNuspec);
        }
        
        Util.Cmd(string.Format(@"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe {0} /t:rebuild /p:Configuration=Release /p:Platform=""Any CPU""", solutionPath));

        if (BuildNuget)
        {
            if (!Directory.Exists(string.Format(@"{0}\nuget\packages\{1}", projectRoot, dotNet35NuspecVersion)))
            {
                Directory.CreateDirectory(string.Format(@"{0}\nuget\packages\{1}", projectRoot, dotNet35NuspecVersion));
            }
            
            if (!Directory.Exists(string.Format(@"{0}\nuget\packages\{1}", projectRoot, portableNuspecVersion)))
            {
                Directory.CreateDirectory(string.Format(@"{0}\nuget\packages\{1}", projectRoot, portableNuspecVersion));
            }
        
            Util.Cmd(string.Format(@"{0}\src\packages\NuGet.CommandLine.2.7.1\tools\NuGet.exe", projectRoot).Dump(), string.Format(@"pack {0}\nuget\RandomSkunk.ProducerConsumer.nuspec -OutputDirectory {0}\nuget\packages\{1}", projectRoot, dotNet35NuspecVersion));
            Util.Cmd(string.Format(@"{0}\src\packages\NuGet.CommandLine.2.7.1\tools\NuGet.exe", projectRoot), string.Format(@"pack {0}\nuget\RandomSkunk.ProducerConsumer.Portable.nuspec -OutputDirectory {0}\nuget\packages\{1}", projectRoot, portableNuspecVersion));
        }

        return 0;
    }
    
    private class Version
    {
        public Version(string version)
        {
            var match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?$");
            if (!match.Success)
            {
                throw new InvalidOperationException();
            }
            
            Major = int.Parse(match.Groups[1].Value);
            Minor = int.Parse(match.Groups[2].Value);
            Build = int.Parse(match.Groups[3].Value);
            
            if (match.Groups[4].Success)
            {
                Revision = int.Parse(match.Groups[4].Value);
            }
        }
    
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Build { get; set; }
        public int? Revision { get; private set; }
        
        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}{3}", Major, Minor, Build, Revision.HasValue ? "." + Revision.Value : "");
        }
    }
}