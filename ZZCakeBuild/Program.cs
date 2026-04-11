using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.IO; // Added for DirectoryPath/FilePath types
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    public class BuildContext : FrostingContext
    {
        public const string ProjectName = "ParticlesPlus";
        public DirectoryPath RootPath { get; }
        public DirectoryPath ProjectPath { get; }
        public string BuildConfiguration { get; }
        public string Version { get; }
        public string Name { get; }
        public bool SkipJsonValidation { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);

            // .MakeAbsolute() and .Collapse() ensure Linux doesn't complain about relative path roots
            var currentDir = context.Environment.WorkingDirectory.MakeAbsolute(context.Environment);
            RootPath = currentDir.Combine("..").Collapse(); 
            ProjectPath = RootPath.Combine(ProjectName).Collapse();

            var modInfoPath = ProjectPath.CombineWithFilePath("modinfo.json");
            var modInfo = context.DeserializeJsonFromFile<ModInfo>(modInfoPath.FullPath);
            Version = modInfo.Version;
            Name = modInfo.ModID;
        }
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation) return;

            // Use context.ProjectPath to find assets
            var jsonFiles = context.GetFiles($"{context.ProjectPath}/assets/**/*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file.FullPath);
                    JToken.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception($"JSON Error in {file.GetFilename()}: {ex.Message}");
                }
            }
        }
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var csproj = context.ProjectPath.CombineWithFilePath($"{BuildContext.ProjectName}.csproj");

            context.DotNetClean(csproj.FullPath, new DotNetCleanSettings { Configuration = context.BuildConfiguration });

            context.DotNetPublish(csproj.FullPath, new DotNetPublishSettings
            {
                Configuration = context.BuildConfiguration,
                // Force output to a known temp folder to make Packaging easier
                OutputDirectory = context.ProjectPath.Combine("temp-publish")
            });
        }
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            // Use the existing Releases folder at the root
            var releasesDir = context.RootPath.Combine("Releases").Collapse();
            var packageDir = releasesDir.Combine(context.Name).Collapse();

            context.EnsureDirectoryExists(releasesDir);
            context.CleanDirectory(packageDir); // Cleans the mod sub-folder specifically

            // 1. Copy DLLs from temp-publish
            // Use FullPath and ensure the glob pattern is handled safely
            var tempPublishDir = context.ProjectPath.Combine("temp-publish").FullPath;
            context.CopyFiles($"{tempPublishDir}/*", packageDir);

            // 2. Copy Assets - Using FullPath strings avoids the "Relative to Root" error
            var assetsSrc = context.ProjectPath.Combine("assets");
            if (context.DirectoryExists(assetsSrc))
            {
                context.CopyDirectory(assetsSrc.FullPath, packageDir.Combine("assets").FullPath);
            }

            // 3. Metadata
            context.CopyFile(context.ProjectPath.CombineWithFilePath("modinfo.json").FullPath, 
                            packageDir.CombineWithFilePath("modinfo.json").FullPath);
            
            var icon = context.ProjectPath.CombineWithFilePath("modicon.png");
            if (context.FileExists(icon)) {
                context.CopyFile(icon.FullPath, packageDir.CombineWithFilePath("modicon.png").FullPath);
            }

            // 4. Zip
            var zipFile = releasesDir.CombineWithFilePath($"{context.Name}_{context.Version}.zip");
            context.Zip(packageDir, zipFile);

            // Clean up
            context.DeleteDirectory(context.ProjectPath.Combine("temp-publish"), new DeleteDirectorySettings { Recursive = true });
        }
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask { }
}