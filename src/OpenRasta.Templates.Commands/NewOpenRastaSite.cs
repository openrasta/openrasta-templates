using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenFileSystem.IO;
using OpenRasta.IO;
using OpenWrap;
using OpenWrap.Commands;
using OpenWrap.IO;
using OpenWrap.PackageManagement;
using OpenWrap.Runtime;
using OpenWrap.Services;
using IFile = OpenFileSystem.IO.IFile;
using OpenWrap.PackageModel;

namespace OpenRasta.Templates.Commands
{
    [Command(Verb="new", Noun="OpenRastaSite",IsDefault=true)]
    public class NewOpenRastaSite : ICommand
    {
        private const string OR_ASPNET_TEMPLATE = "OpenRasta.AspNetTemplate";
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironment _environment;
        private readonly IPackageManager _packageManager;

        [CommandInput(IsRequired = true, Position=0)]
        public string Name { get; set; }

        [CommandInput(IsRequired = true, Position=1)]
        public string Namespace { get; set; }

        [CommandInput]
        public bool StartEditor { get; set; }

        public NewOpenRastaSite()
            : this(
            ServiceLocator.GetService<IFileSystem>(),
            ServiceLocator.GetService<IEnvironment>(),
            ServiceLocator.GetService<IPackageManager>())
        {
            
        }

        private NewOpenRastaSite(
            IFileSystem fileSystem, 
            IEnvironment environment,
            IPackageManager packageManager)
        {
            _fileSystem = fileSystem;
            _environment = environment;
            _packageManager = packageManager;
        }

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
            yield return new GenericMessage("Template created.");
            var sourceRepositories = new[] {_environment.CurrentDirectoryRepository, _environment.SystemRepository}
                .Concat(_environment.RemoteRepositories)
                .Concat(new[]{_environment.ProjectRepository})
                .Where(x=>x != null);
            _packageManager.AddProjectPackage(
                PackageRequest.Exact("openrasta-hosting-aspnet", "3.0".ToVersion()),
                sourceRepositories, 
                _environment.Descriptor,
                _environment.ProjectRepository).ToList();
            yield return new GenericMessage("OpenRasta added and initialized.");
            _environment.ScopedDescriptors[string.Empty].Save();
            if (StartEditor)
            {
                var process = new Process()
                                  {StartInfo = new ProcessStartInfo(solution.Path.FullPath) {UseShellExecute = true}};
                process.Start();
                yield return new GenericMessage("Starting editor...");
            }
        }

        private void ReplaceTokensAndSave(IFile solution)
        {
            var solutionContent = solution.ReadString();
            solutionContent = solutionContent.Replace(OR_ASPNET_TEMPLATE, Namespace).Replace("openrasta-templates", Name);
            using(var stream = solution.OpenWrite())
            using(var writer = new StreamWriter(stream, Encoding.UTF8))
                writer.Write(solutionContent);
        }
    }
}
