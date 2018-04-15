var target = Argument("target", "build");
var configuration = Argument("configuration", "Release");

var buildDir = Directory("./src/bin") + Directory(configuration);

Task("clean")
    .Does(() => {
    CleanDirectory(buildDir);
});

var pagesDir =string.Concat(buildDir, "/Pages/"); 

Task("clean-pages")
  .Does(() => {
    CleanDirectories(pagesDir);
  });


Task("copy-pages")
    .IsDependentOn("clean-pages")
    .Does(() =>{
       CopyDirectory("./src/Pages", pagesDir);
    });

void PublishTransmission(string output,  string runtime = ""){
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

void Build(string buildPath, bool noRestore, bool noIncremental, bool noDependencies, params Action[] addonsPublish){
  var settings = new DotNetCoreBuildSettings{
        Configuration = configuration,
        NoRestore = noRestore,
        NoIncremental = noIncremental,
        NoDependencies = noDependencies
      };
      var acts = new Action[addonsPublish.Length + 1];
      Array.Copy(addonsPublish, 0, acts, 1, addonsPublish.Length);
      acts[0] = () => DotNetCoreBuild(buildPath, settings);
      foreach (var act in acts){
        act();
      }
}

Task("build")
    .IsDependentOn("clean")
    .IsDependentOn("copy-pages")
    .Does(() => {
      Build("./RPiBotFs.sln", false, true, false, new Action[]{
          () => PublishTransmission(string.Concat(buildDir,"/extensions", "/transmission"))
        });
});

Task("build-core")
  .Does(() => {
    Build("./src/RPiBotFs.fsproj", true, false, true);
  });

Task("rebuild-core")
  .Does(() => {
    Build("./src/RPiBotFs.fsproj", true, false, false);
  });



void RunTests(string testProj, bool noBuild = false, bool noRestore = false){
  var settings = new DotNetCoreTestSettings{
    NoBuild = noBuild,
    NoRestore = noRestore
  };
  DotNetCoreTest(testProj, settings);
}

Task("clean-test-output")
  .Does(() => {
    CleanDirectory("./tests/RPiBotFs.Tests/bin/" + configuration);
  });

Task("build-run-tests")
  .IsDependentOn("clean-test-output")
  .Does(() => {
    RunTests("./tests/RPiBotFs.Tests/RPiBotFs.Tests.fsproj");
  });

Task("run-tests")
  .Does(() => {
    RunTests("./tests/RPiBotFs.Tests/RPiBotFs.Tests.fsproj", true, true);
  });



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
    DotNetCorePublish("./RPiBotFs.sln", settings);
    PublishTransmission(string.Concat(publishDir, "/extensions", "/transmission"), settings.Runtime);
  });

RunTarget(target);