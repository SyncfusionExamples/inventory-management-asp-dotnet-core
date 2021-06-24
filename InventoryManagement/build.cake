///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var distDirectory = Directory("./distination");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
});

Teardown(ctx =>
{
	// Executed AFTER the last task.
	Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
.Does(() => {
	CleanDirectory(distDirectory);
});

Task("Build")
	.IsDependentOn("Clean")
	.Does(()=>
	{
		NuGetRestore(".");
		MSBuild(".", new MSBuildSettings().SetConfiguration(configuration).WithTarget("Build"));
		//DotNetCoreBuild("./InventoryManagement/InventoryManagement.csproj", new
			//DotNetBuildSettings(){
			//		Configuration = configuration,
			//		ArgumentCustomization = args => args.Append("--no-restore"),
			//	});
		//});
	});
Task("Default").IsDependentOn("Build");
RunTarget(target);