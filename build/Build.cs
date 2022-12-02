using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Publish) },
    PublishArtifacts = true,
    EnableGitHubToken = true
)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly GitHubActions GithubActions = GitHubActions.Instance;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [Solution] readonly Solution Solution;

    Target GetVersion => _ => _
        .Executes(() =>
        {
            Log.Information("GitVersion = {Value}", GitVersion.MajorMinorPatch);
        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s.SetAssemblyVersion(GitVersion.AssemblySemVer));
        });


    public static readonly string publishFolder = RootDirectory / "publish";

    Target Publish => _ => _
        .OnlyWhenStatic(() => GitRepository.Branch == "master")
        .DependsOn(Compile)
        .Produces(publishFolder)
        .Executes(() =>
        {
            DotNetPublish(s =>
                s.SetOutput(publishFolder)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
            );
        });
}