using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Octokit;
using Octokit.Internal;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;


[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Publish) },
    PublishArtifacts = true,
    EnableGitHubToken = true
)]
[GitHubActions(
    "PullRequest",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    On = new[] { GitHubActionsTrigger.PullRequest },
    OnWorkflowDispatchOptionalInputs = new string[] { },
    InvokedTargets = new[] { nameof(NotifyRelease) }, PublishArtifacts = true,
    EnableGitHubToken = true
)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Publish);

    [Nuke.Common.Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly GitHubActions GitHubActions = GitHubActions.Instance;

    [GitRepository] readonly GitRepository GitRepository;
    [Solution] readonly Solution Solution;
    [GitVersion] readonly GitVersion GitVersion;

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
            DotNetBuild();
        });


    public static readonly string publishFolder = RootDirectory / "publish";

    Target Publish => _ => _
        .DependsOn(Compile)
        .DependsOn(GetSemVer)
        .Produces(publishFolder)
        .Triggers(Release)
        .Executes(() =>
        {
            DotNetPublish(s =>
                s.SetOutput(publishFolder)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
            );
        });

    Target Release => _ => _
        .OnlyWhenStatic(() => GitRepository.Branch == "master" && IsServerBuild)
        .After(Publish)
        .Executes(async () =>
        {
            var credentials = new Credentials(GitHubActions.Token);
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));
            var (owner, name) = (GitRepository.GetGitHubOwner(), GitRepository.GetGitHubName());

            var releaseTag = GitVersion.AssemblySemVer;

            var newRelease = new NewRelease(releaseTag)
            {
                TargetCommitish = GitRepository.Commit,
                Draft = true,
                Name = $"v{releaseTag}",
                Prerelease = !string.IsNullOrEmpty(GitVersion.PreReleaseTag),
                Body = ""
            };

            var createdRelease = await GitHubTasks
                .GitHubClient
                .Repository
                .Release.Create(owner, name, newRelease);

            var zipPath = RootDirectory / $"{GitVersion.AssemblySemVer}.zip";
            ZipFile.CreateFromDirectory(publishFolder, zipPath);

            await UploadReleaseAssetToGithub(createdRelease, zipPath);

            await GitHubTasks
                .GitHubClient
                .Repository
                .Release
                .Edit(owner, name, createdRelease.Id, new ReleaseUpdate { Draft = false });
        });

    Target GetSemVer => _ => _
        .Executes(() =>
        {
            Log.Information("GitVersion = {Value}", GitVersion.AssemblySemVer);
        });

    Target GetGitCommit => _ => _
        .Executes(() =>
        {
            Log.Information("GitCommit = {Value}", GitRepository.Commit);
        });

    Target NotifyRelease => _ => _
        .OnlyWhenStatic(() => GitHubActions.IsPullRequest && IsServerBuild)
        .Executes(
            () =>
            {
                    Log.Information("GithubEvent = {Value}", JsonConvert.SerializeObject(GitHubActions));
);
            }
        );

    static async Task UploadReleaseAssetToGithub(Release release, string asset)
    {
        await using var artifactStream = File.OpenRead(asset);
        var fileName = Path.GetFileName(asset);
        var assetUpload = new ReleaseAssetUpload
        {
            FileName = fileName,
            ContentType = "application/zip",
            RawData = artifactStream,
        };
        await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, assetUpload);
    }
}