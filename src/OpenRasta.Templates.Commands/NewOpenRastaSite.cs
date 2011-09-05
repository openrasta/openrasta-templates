using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using OpenFileSystem.IO;
using OpenWrap;
using OpenWrap.Commands;
using OpenWrap.IO;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Runtime;
using OpenWrap.Services;

namespace OpenRasta.Templates.Commands
{
    [Command(Verb = "new", Noun = "OpenRastaSite", IsDefault = true)]
    public class NewOpenRastaSite : ICommand
    {
        private const string OR_ASPNET_TEMPLATE = "OpenRasta.AspNetTemplate";
        private readonly IEnvironment _environment;
        private readonly IFileSystem _fileSystem;
        private readonly IPackageManager _packageManager;
        private readonly IRemoteManager _remotes;

        public NewOpenRastaSite()
            : this(
                ServiceLocator.GetService<IFileSystem>(),
                ServiceLocator.GetService<IEnvironment>(),
                ServiceLocator.GetService<IPackageManager>(),
                ServiceLocator.GetService<IRemoteManager>())
        {
        }

        private NewOpenRastaSite(
            IFileSystem fileSystem,
            IEnvironment environment,
            IPackageManager packageManager,
            IRemoteManager remotes)
        {
            _fileSystem = fileSystem;
            _environment = environment;
            _packageManager = packageManager;
            _remotes = remotes;
        }

        [CommandInput(IsRequired = true, Position = 0)]
        public string Name { get; set; }

        [CommandInput(IsRequired = true, Position = 1)]
        public string Namespace { get; set; }

        [CommandInput]
        public bool StartEditor { get; set; }

        #region ICommand Members

        public IEnumerable<ICommandOutput> Execute()
        {
            var sourceFolder = _environment.CurrentDirectory.GetDirectory("src");
            var zip = new FastZip();
            zip.ExtractZip(new MemoryStream(Templates.AspNetTemplate), sourceFolder.Path.FullPath,
                           FastZip.Overwrite.Never, x => true, null, null, false, true);
            var projectFolder = sourceFolder.GetDirectory(Namespace);
            sourceFolder.GetDirectory(OR_ASPNET_TEMPLATE).MoveTo(projectFolder);
            // rename project folders
            var solution = sourceFolder.GetFile(Name + ".sln");
            sourceFolder.GetFile("openrasta-aspnettemplate.sln").MoveTo(solution);
            ReplaceTokensAndSave(solution);
            var projectFile = projectFolder.GetFile(Namespace + ".csproj");
            projectFolder.GetFile(OR_ASPNET_TEMPLATE + ".csproj").MoveTo(projectFile);
            ReplaceTokensAndSave(projectFile);
            foreach (var file in sourceFolder.Files("*.cs", SearchScope.SubFolders))
                ReplaceTokensAndSave(file);
            yield return new Info("Template created.");
            var sourceRepositories = new[] {_environment.CurrentDirectoryRepository, _environment.SystemRepository}
                .Concat(_remotes.FetchRepositories())
                .Concat(new[] {_environment.ProjectRepository})
                .Where(x => x != null);
            _packageManager.AddProjectPackage(
                PackageRequest.Exact("openrasta-hosting-aspnet", "2.1".ToVersion()),
                sourceRepositories,
                _environment.Descriptor,
                _environment.ProjectRepository).ToList();
            yield return new Info("OpenRasta added and initialized.");
            _environment.ScopedDescriptors[string.Empty].Save();
            if (StartEditor)
            {
                var process = new Process
                                  {StartInfo = new ProcessStartInfo(solution.Path.FullPath) {UseShellExecute = true}};
                process.Start();
                yield return new Info("Starting editor...");
            }
        }

        #endregion

        private void ReplaceTokensAndSave(IFile solution)
        {
            var solutionContent = solution.ReadString();
            solutionContent = solutionContent.Replace(OR_ASPNET_TEMPLATE, Namespace).Replace("openrasta-templates", Name);
            using (var stream = solution.OpenWrite())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                writer.Write(solutionContent);
        }
    }
}