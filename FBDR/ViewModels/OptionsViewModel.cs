using FBDR.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace FBDR.ViewModels
{
    class OptionsViewModel : BindableBase, INavigationAware
    {
        #region Fields
        private ICommand _SetFontCommand;
        private ICommand _CloseCommand;
        private ICommand _OptionsChangedCommand;
        private int _SelectedSectionIndex;
        #endregion

        #region Properties
        public ICommand SetFontCommand => _SetFontCommand ??=
            new DelegateCommand(SetFont);

        public ICommand CloseCommand => _CloseCommand ??=
            new DelegateCommand(Close);

        public ICommand OptionsChangedCommand => _OptionsChangedCommand ??=
            new DelegateCommand(OptionsChanged);
        public IRegionManager RegionManager { get; }
        public IEventAggregator EventAggregator { get; }
        public Options Options { get; }
        public int SelectedSectionIndex
        {
            get => _SelectedSectionIndex;
            set => SetProperty(ref _SelectedSectionIndex, value);
        }

        #endregion

        #region Constructors
        public OptionsViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, Options options)
        {
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            Options = options;
        }
        #endregion

        #region Methods
        private async void Close()
        {
            EventAggregator.GetEvent<Events.StatusUpdateEvent>().Publish(
                new Events.StatusUpdateEventArgs() { Status = "Speichere Einstellungen..." });
            await Task.Run(() => Serializer.WriteToBinaryFile(Options.FilePath, Options));
            RegionManager.RequestNavigate(RegionNames.ContentRegion, nameof(Views.MainView));
            EventAggregator.GetEvent<Events.StatusUpdateEvent>().Publish(
                new Events.StatusUpdateEventArgs() { Status = "Einstellungen gespeichert", DisplayTime = 3000 });
        }


        private void SetFont()
        {
            var fontDialog = new FontDialog();
            fontDialog.Font = Options.DefaultFont;
            fontDialog.ShowColor = false;
            var result = fontDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                Options.DefaultFont = fontDialog.Font;
                OptionsChanged();
            }
        }

        private void OptionsChanged()
        {
            RaisePropertyChanged(nameof(Options));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
            var section = navigationContext.Parameters["section"] as string;
            if (section == OptionSections.GeneralSection)
            {
                SelectedSectionIndex = 1;
            }
            else if (section == OptionSections.FontsSection)
            {
                SelectedSectionIndex = 2;
            }
            else if (section == OptionSections.MarginsSection)
            {
                SelectedSectionIndex = 3;
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
        }
        #endregion
    }
}
