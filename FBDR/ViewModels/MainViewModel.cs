using FBDR.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace FBDR.ViewModels
{
    class MainViewModel : BindableBase
    {
        #region Constants
        private const string FOLDER_NAME = "Facebook Dokumente (PDF)";
        private const string RENDERING_STATUS_TEXT = "Rendere {0}/{1} (\"{2}\")";
        #endregion

        #region Fields
        private ICommand _SelectFilesCommand;
        private ObservableCollection<SelectedPath> _SelectedPaths;
        private ICommand _RenderCommand;
        private FacebookDocumentRenderer _Renderer;
        private ICommand _ConfigureOptionsCommand;
        private ICommand _SelectDirectoriesCommand;
        private ICommand _DeletePathCommand;
        #endregion
        #region Properies
        public ICommand SelectFilesCommand => _SelectFilesCommand ??=
            new DelegateCommand(SelectFiles);
        public ICommand SelectDirectoriesCommand => _SelectDirectoriesCommand ??=
            new DelegateCommand(SelectDirectories);

        public ICommand RenderCommand => _RenderCommand ??=
            new DelegateCommand(Render, CanRender);

        public ICommand ConfigureOptionsCommand => _ConfigureOptionsCommand ??=
            new DelegateCommand<string>(ConfigureOptions);

        public ICommand DeletePathCommand => _DeletePathCommand ??=
            new DelegateCommand<SelectedPath>(DeletePath);

        public ObservableCollection<SelectedPath> SelectedPaths => _SelectedPaths ??=
            new ObservableCollection<SelectedPath>();

        public FacebookDocumentRenderer Renderer => _Renderer ??= new FacebookDocumentRenderer(Options);

        public IRegionManager RegionManager { get; }
        public IEventAggregator EventAggregator { get; }
        public Options Options { get; }
        public bool RenderingInProgress { get; private set; }
        #endregion

        #region Constructors
        public MainViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, Options options)
        {
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            Options = options;
        }
        #endregion

        #region Methods
        private void ConfigureOptions(string optionSection)
            => RegionManager.RequestNavigate(RegionNames.ContentRegion,
                $"{nameof(Views.OptionsView)}?section={optionSection}");

        private void DeletePath(SelectedPath path)
            => SelectedPaths.Remove(path);

        private void SelectFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Title = "Datei auswählen";
            dialog.Filter = "Alle Dateien|*.htm;*.html;*.zip|HTML Dateien|*.htm;*.html|Zip Dateien|*.zip";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() is true)
            {
                foreach (var path in dialog.FileNames)
                {
                    SelectedPaths.Add(new SelectedPath(path));
                }
            }
            ((DelegateCommand)RenderCommand).RaiseCanExecuteChanged();
        }

        private void SelectDirectories()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedPaths.Add(new SelectedPath(dialog.SelectedPath));
            }
            ((DelegateCommand)RenderCommand).RaiseCanExecuteChanged();
        }

        private void SendStatusUpdate(Events.StatusUpdateEventArgs update)
            => EventAggregator.GetEvent<Events.StatusUpdateEvent>().Publish(update);


        private async void Render()
        {
            RenderingInProgress = true;
            ((DelegateCommand)RenderCommand).RaiseCanExecuteChanged();

            var dirName = Path.GetDirectoryName(SelectedPaths[0].Path);
            var dir = Directory.CreateDirectory(Path.Combine(dirName, FOLDER_NAME)).FullName;

            SendStatusUpdate(new Events.StatusUpdateEventArgs()
            {
                Status = "Zähle Dateien",
            });

            var count = await GetTotalFileCount();

            SendStatusUpdate(new Events.StatusUpdateEventArgs()
            {
                //Status = string.Empty,
                ProgressMax = count
            });

            int i = 1;
            foreach (var selectedPath in SelectedPaths)
            {
                var path = selectedPath.Path;
                if (selectedPath.Type == PathType.File)
                {
                    UpdateRenderingState(path);
                    await RenderFile(path, CoerceFileName(path, dir));
                }
                else if (selectedPath.Type == PathType.Directory)
                {
                    foreach (var file in Directory.EnumerateFiles(path))
                    {
                        if (file.EndsWith(".html") || file.EndsWith(".htm"))
                        {
                            UpdateRenderingState(file);
                            System.Diagnostics.Debug.WriteLine(file);
                            await RenderFile(file, CoerceFileName(file, dir));
                        }
                    }
                }
                else if (selectedPath.Type == PathType.Archive)
                {
                    using var file = File.OpenRead(path);
                    using var zip = new ZipArchive(file, ZipArchiveMode.Read);
                    foreach (var entry in zip.Entries)
                    {
                        if (Path.GetExtension(entry.Name).ToLower().StartsWith(".htm"))
                        {
                            UpdateRenderingState(entry.Name);
                            using var streamReader = new StreamReader(entry.Open());
                            var html = await streamReader.ReadToEndAsync();
                            Console.WriteLine(entry.FullName);
                            var pdf = await Renderer.RenderAsPdf(html);
                            await File.WriteAllBytesAsync(CoerceFileName(entry.Name, dir), pdf);
                        }
                    }
                }
            }
            SendStatusUpdate(new Events.StatusUpdateEventArgs()
            {
                Status = "fertig",
                DisplayTime = 3000,
                Progress = 0,
                ProgressMax = 0
            });
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { FileName = dir, UseShellExecute = true });
           

            void UpdateRenderingState(string docPath)
            => SendStatusUpdate(new Events.StatusUpdateEventArgs()
            {
                Status = string.Format(RENDERING_STATUS_TEXT, i, count, Path.GetFileNameWithoutExtension(docPath)),
                Progress = i++
            });
            RenderingInProgress = false;
            ((DelegateCommand)RenderCommand).RaiseCanExecuteChanged();
        }

        private string CoerceFileName(string fileName, string dirName)
        {
            var fn = Path.GetFileNameWithoutExtension(fileName);
            var pdfName = fn + ".pdf";
            if (!Options.OverwriteFiles)
            {
                int i = 1;
                while (File.Exists(Path.Combine(dirName, pdfName)))
                {
                    pdfName = fn + $" ({i}).pdf";
                    i++;
                }
            }
            return Path.Combine(dirName, pdfName);
        }

        private async Task<int> GetTotalFileCount(string initialPath = null)
        {
            int count = 0;
            await Task.Run(() =>
            {
                foreach (var selectedPath in SelectedPaths)
                {
                    var path = selectedPath.Path.ToLower();
                    switch (selectedPath.Type)
                    {
                        case PathType.File:
                            count++;
                            break;
                        case PathType.Directory:
                            foreach (var file in Directory.EnumerateFiles(path))
                            {
                                if (file.EndsWith(".html") || file.EndsWith(".htm"))
                                {
                                    count++;
                                }
                                //else if (file.EndsWith(".zip"))
                                //{
                                //    count += GetTotalFileCount(file);
                                //}
                            }
                            break;
                        case PathType.Archive:
                            using (var file = File.OpenRead(path))
                            {
                                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                                {
                                    foreach (var entry in zip.Entries)
                                    {
                                        var fileName = entry.FullName;
                                        if (fileName.EndsWith(".html") || fileName.EndsWith(".htm"))
                                        {
                                            count++;
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            });
            return count;
        }

        private async Task RenderFile(string from, string to)
        {
            var html = await File.ReadAllTextAsync(from);
            var bytes = await Renderer.RenderAsPdf(html);
            await File.WriteAllBytesAsync(to, bytes);
        }

        private bool CanRender() => SelectedPaths.Count > 0 && !RenderingInProgress;
        #endregion
    }
}
