var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var buildDir = Directory("./src/bin") + Directory(configuration);

Task("Clean")
    .Does(() => {
    CleanDirectory(buildDir);
});

Task("Copy-Pages")
    .IsDependentOn("Clean")
    .Does(() =>{
       CopyDirectory("./src/Pages", string.Concat(buildDir, "/Pages/"));
    });
Task("Restore-NuGet-Packages")
    .IsDependentOn("Copy-Pages")
    .Does(() => {
    NuGetRestore("./RPiBotFs.sln");
});
void publishTransmission(string output,  string runtime = ""){
  var proj = "./src/Transmission.RPIExtension/Transmission.RPIExtension.fsproj";
    
    var settings = new DotNetCorePublishSettings() {
      OutputDirectory =  output,
      Configuration = configuration
    };
    if(!string.IsNullOrWhiteSpace(runtime))
      settings.Runtime = runtime;
    DotNetCorePublish(proj,
      settings);
}

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => {
      // Use MSBuild
      Parallel.Invoke(() =>
        MSBuild("./RPiBotFs.sln", settings =>
          settings.SetConfiguration(configuration)), 
        () => publishTransmission(string.Concat(buildDir,"/extensions", "/transmission")));
});

Task("Default")
  .IsDependentOn("Build");

var publishDir = "publish";
Task("clean-publish")
  .Does(() => {
    CleanDirectory(publishDir);
  });
Task("linux-publish")
  .IsDependentOn("clean-publish")
  .Does(() => {
    var settings = new DotNetCorePublishSettings(){
      OutputDirectory = publishDir,
      Runtime = "linux-arm"
    };
    Parallel.Invoke(() => DotNetCorePublish("./RPiBotFs.sln", settings),
      () => publishTransmission(string.Concat(publishDir, "/extensions", "/transmission"), 
          settings.Runtime));
  });

RunTarget(target);