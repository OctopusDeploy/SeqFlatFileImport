//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.4"
#tool "nuget:?package=ILRepack&version=2.0.11"
#addin "MagicChunks"

using Path = System.IO.Path;
using IO = System.IO;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var publishDir = "./publish";
var artifactsDir = "./artifacts";
var cleanups = new List<Action>();
var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

var nugetVersion = gitVersionInfo.NuGetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building SeqFlatFileImport v{0}", nugetVersion);
     if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);
    if(BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);
});

Teardown(context =>
{
    Information("Cleaning up");
    foreach(var item in cleanups)
        item();

    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectory(publishDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());

Task("__UpdateProjectJsonVersion")
    .Does(() =>
{
    var files = GetFiles("./source/**/project.json");
    foreach(var file in files)
    {   
        var projectJson = file.FullPath;
        RestoreFileOnCleanup(projectJson);
        Information("Updating {0} version -> {1}", projectJson, nugetVersion);

        TransformConfig(projectJson, projectJson, new TransformationCollection {
            { "version", nugetVersion }
        });
    }
});

Task("__Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .Does(() =>
{
    DotNetCoreBuild("**/project.json", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__Test")
    .IsDependentOn("__Build")
    .Does(() =>
{
    GetFiles("source/*Tests/project.json")
        .ToList()
        .ForEach(testProjectFile => 
        {
            DotNetCoreTest(testProjectFile.ToString(), new DotNetCoreTestSettings
            {
                Configuration = configuration,
                WorkingDirectory = Path.GetDirectoryName(testProjectFile.ToString())
            });
        });
});

Task("__DotnetPublish")
    .IsDependentOn("__Test")
    .Does(() =>
{
    DotNetCorePublish("source/Console", new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir
    });
});

Task("__Merge")
    .IsDependentOn("__DotnetPublish")
    .Does(() => {
        CreateDirectory(artifactsDir);

        var inputFolder = publishDir;
        var outputFolder = artifactsDir;
        CreateDirectory(outputFolder);
        ILRepack(
            $"{outputFolder}/SeqFlatFileImport.exe",
            $"{inputFolder}/SeqFlatFileImport.exe",
            IO.Directory.EnumerateFiles(inputFolder, "*.dll").Select(f => (FilePath) f),
            new ILRepackSettings { 
                Internalize = true, 
                Libs = new List<FilePath>() { inputFolder }
            }
        );
    });

Task("__Zip")
    .IsDependentOn("__Merge")
    .Does(() => {
        Zip($"{artifactsDir}/SeqFlatFileImport.exe", $"{artifactsDir}/SeqFlatFileImport.zip");
    });




//////////////////////////////////////////////////////////////////////
// HELPERS
//////////////////////////////////////////////////////////////////////

private void RestoreFileOnCleanup(string file)
{
    var contents = System.IO.File.ReadAllBytes(file);
    cleanups.Add(() => {
        Information("Restoring {0}", file);
        System.IO.File.WriteAllBytes(file, contents);
    });
}


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__Zip");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);

