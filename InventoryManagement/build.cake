#addin "nuget:?package=Cake.ArgumentHelpers"
#addin "Cake.Npm"
#addin "Cake.Karma"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=NUnit.Extension.TeamCityEventListener"
#tool "nuget:?package=OctopusTools"
#tool "nuget:?package=TeamCity.VSTest.TestAdapter"
#addin "nuget:?package=Cake.Sonar"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"
#module "nuget:?package=Cake.BuildSystems.Module"

var OctopusDeployUrl = "https://deploy.hhaexchange.com";

var Target = Argument("target", "BuildWithAnalysis");
var BuildNumber = ArgumentOrEnvironmentVariable("build.number", "", "0.0.1-local.0");
var OctopusDeployApiKey = ArgumentOrEnvironmentVariable("OctopusDeployApiKey", "");
var MsBuildLogger = ArgumentOrEnvironmentVariable("MsBuildLogger", "", "");
var ProjectName = ArgumentOrEnvironmentVariable("ProjectName","","InventoryManagement.App");
var DeploymentBranches = ArgumentOrEnvironmentVariable("DeploymentBranches", "", " ");
var TeamCityBuildAgentDirectory = ArgumentOrEnvironmentVariable("teamcity.agent.home.dir", "", "c:\\BuildAgent");
var configuration = Argument("configuration", "Release");

    Action<FilePath, ProcessArgumentBuilder> Cmd => (path, args) => {
        var result = StartProcess(
            path,
            new ProcessSettings {
                Arguments = args
            });

        if (0 != result)
        {
            throw new Exception($"Failed to execute tool {path.GetFilename()} ({result})");
        }
    };

string BranchName = null;
string tenant = null;

var ngPath = Context.Tools.Resolve("ng.cmd");
var npmPath = Context.Tools.Resolve("npm.cmd");

var SonarLogin = ArgumentOrEnvironmentVariable("SonarLocalLogin", "", " ");
var SonarFileExclusions = "**/*.Test/*.cs,**/*.Jobs/**/*,**/assets/Scripts/**/*,**/*.config,**/*.sitemap,**/*.pdf*,**/*.exclude*,**/*.vbproj*,**/*.nuspec*,**/*.txt*,**/css*/**/*,**/obj/**/*,**/bin/**/*,**/App_Themes/**/*,**/App_GlobalResources/**/*,**/App_Data/**/*,**/*.sln,**/*.ps1,**/*.vs/**/*,**/*.nuget/**/*,**/*.md,**/*.yml,**/.gitignore,**/.git/**/*,**/*.config,**/fonts/**/*,**/Images/**/*" +
                          ",**/styles/**/*,**/*.spec.ts,**/Context/*.cs";
var CoverageFileExclusions = "**/*.Test/*.cs, **/*.Jobs/**/*, **/*.Domain/**/*,**/Startup.cs,**/Program.cs";
bool SonarAnalysis = false;

bool.TryParse(ArgumentOrEnvironmentVariable("SonarAnalysis", "", "false"), out SonarAnalysis);

Task("Version")
    .Does(() =>
{
    GitVersionSettings buildServerSettings = new GitVersionSettings {
        OutputType = GitVersionOutput.BuildServer,
        UpdateAssemblyInfo = true
    };

    SetGitVersionPath(buildServerSettings);

    // Ran twice because the result is empty when using buildserver mode but we need to output to TeamCity
    // and use the result
    GitVersion(buildServerSettings);

    GitVersionSettings localSettings = new GitVersionSettings();

    SetGitVersionPath(localSettings);

    var versionResult = GitVersion(localSettings);

    Information("AssemblySemVer: " + versionResult.AssemblySemVer);

    // Convert 12.1.3.4 to 1201030004 etc.
    string paddedVersionNumber = string.Join("", versionResult.AssemblySemVer.Split('.').Select(s => s.PadLeft(2, '0')).ToArray()) + "00";

    Information("PaddedVersionNumber: " + paddedVersionNumber);

    BuildNumber = versionResult.SemVer;
    BranchName = versionResult.BranchName;

    Information("BuildNumber updated: " + BuildNumber);
});

n

Task("RestorePackages")
    .Does(() =>
{
    NuGetRestore("InventoryManagement.sln");

    DotNetCoreRestore("InventoryManagement.sln");
});

Task("Build-API")
    .IsDependentOn("Version")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var msBuildSettings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("DeployOnBuild", "true");

    if(!string.IsNullOrEmpty(MsBuildLogger))
    {
        msBuildSettings.ArgumentCustomization = arguments =>
                arguments.Append(string.Format("/logger:{0}", MsBuildLogger));
    }
        
    MSBuild("InventoryManagement.sln", msBuildSettings);
});

Task("Test-API")
    .IsDependentOn("Build-API")
    .Does(() =>
{
    var settings = new DotNetCoreTestSettings()
    {
        NoBuild = true,
        Configuration = configuration,
        ArgumentCustomization = args => {
            return args.Append("/p:CollectCoverage=true")
                       .Append("/p:CoverletOutputFormat=opencover");
        }
    };

    var testAdapterPath = GetFiles("./**/vstest15/TeamCity.VSTest.TestAdapter.dll").First();

    Information("Test Adapter Path " + testAdapterPath);

    if (TeamCity.IsRunningOnTeamCity) 
    {
        settings.Logger = "teamcity";
        settings.TestAdapterPath = testAdapterPath.GetDirectory();
    }

	var projectFiles = GetFiles("**/*.Test.csproj");

    foreach(var file in projectFiles)
    {
        Information("Test File FullPath {0}", file.FullPath);
        DotNetCoreTest(file.FullPath, settings);
    }
});

Task("SonarBegin")
    .WithCriteria(SonarAnalysis)
    .Does(() => 
{
    SonarBegin(new SonarBeginSettings {
        Key = "Opsworklist",
        Name = "Opsworklist",
        Version = BuildNumber,
        Branch = BranchName,
        Url = "https://sonar.hhaexchange.com/",
		Exclusions = SonarFileExclusions,
        CoverageExclusions=CoverageFileExclusions,
        OpenCoverReportsPath = "**/coverage.opencover.xml",
        Login = SonarLogin   
     });
});

Task("SonarEnd")
    .WithCriteria(SonarAnalysis)
    .Does(() => {
     SonarEnd(new SonarEndSettings{
        Login = SonarLogin
     });
});

Task("BuildWithAnalysis")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Test-API")
    .IsDependentOn("SonarEnd")
    .Does(() => {});



Task("Pack-Poller")
      .Does(() => 
{
    DeleteFiles("./publishpackage/InventoryManagement.*");

    string publishDirectory = "./InventoryManagement/bin/publish";

    var publishSettings = new DotNetCorePublishSettings
    {
        Configuration = "Release",
        OutputDirectory = publishDirectory
    };

    DotNetCorePublish("InventoryManagement/InventoryManagement.csproj", publishSettings);

    var nuGetPackSettings = new NuGetPackSettings 
    {
        OutputDirectory = "./publishpackage/",
        BasePath = "./InventoryManagement/bin/publish/",
        Version = BuildNumber
    };

    NuGetPack(".InventoryManagement/InventoryManagement.nuspec", nuGetPackSettings); 
});

Task("Pack")
   // .IsDependentOn("Pack-API")
  //  .IsDependentOn("Pack-Angular-App")
    .IsDependentOn("Pack-Poller");

Task("OctoPush")
  .IsDependentOn("Pack")
  .Does(() => 
{
    if (BuildNumber.Contains("-develop") || BuildNumber.Contains("-release") || BuildNumber.Contains("-hotfix") || IsFeatureBranchWithTenant())
    {
        Information("Push packages to Octopus");

        OctoPush(OctopusDeployUrl, 
                    OctopusDeployApiKey, 
                    GetFiles("./publishpackage/*.*"),
                    new OctopusPushSettings 
                    {
                        ReplaceExisting = true
                    });
    }
});

Task("OctoRelease")
  .IsDependentOn("OctoPush") 
  .Does(() => 
{
	var releaseSettings = new CreateReleaseSettings 
	{
		ApiKey = OctopusDeployApiKey,
		ArgumentCustomization = args => args.Append("--packageVersion " + BuildNumber),
        ReleaseNumber = BuildNumber,
		Server = OctopusDeployUrl
    };

	if (BuildNumber.Contains("-develop")) 
	{
		releaseSettings.Channel = "Trunk";
		releaseSettings.DeployTo = "DEV";
	} 
	else if (BuildNumber.Contains("-release"))
	{
		releaseSettings.Channel = "Release";
	}
    else if (BuildNumber.Contains("-hotfix"))
	{
		releaseSettings.Channel = "Hotfix";
	}
	else if(!string.IsNullOrEmpty(tenant))
	{
		releaseSettings.Channel = "Feature";
		releaseSettings.Tenant = new string[]{tenant};
		releaseSettings.DeployTo = "DEV";
	}
	else
	{
		releaseSettings.Channel = "Trunk";
		releaseSettings.DeployTo = "DEV";
	} 

    if(!string.IsNullOrEmpty(releaseSettings.Channel))
	{
		Information("Deploying to target project: "+ ProjectName);

		OctoCreateRelease(ProjectName, releaseSettings);
	}
	else
	{
		Information("Deployment is not enabled for this branch");
	}
});

public void SetGitVersionPath(GitVersionSettings settings)
{
    if (TeamCity.IsRunningOnTeamCity)
    {
        Information("Using shared GitVersion");

        settings.ToolPath = "c:\\tools\\gitversion\\gitversion.exe";
    }
}

private bool IsFeatureBranchWithTenant	()
{
    Information("Deployment Branches are: "+DeploymentBranches);
    Information("Current Branch is: "+BranchName);

    if(!string.IsNullOrEmpty(DeploymentBranches))
    {
        var deploymentBranches = DeploymentBranches.Split(new[]{ ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach(string deploymentBranch in deploymentBranches)
        {
            Information("Checking if branch is:" + deploymentBranch);

            if(BranchName.ToLower() == deploymentBranch.Trim().ToLower())
            {
                var pattern = "([^/]*)(.*[/])([^-_]+(?:-|_)[^-_]+)([-|_]*)(.*)";
        
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                var match = r.Match(BranchName);

                if (match.Success)
                {
                    var currentBranchFolder = match.Groups[1].Value; 
                    var currentTicketNumber = match.Groups[3].Value;
                    var currentBranch = match.Groups[5].Value;

                    Information("Folder: " + currentBranchFolder);
                    Information("Ticket: " + currentTicketNumber);
                    Information("Branch: " + currentBranch);

                    tenant = currentTicketNumber;

                    Information("Using tenant:" + tenant);

                    return true;
                }
            }
        }

        return false;
    }

    return false;
}

RunTarget(Target);
