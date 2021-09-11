using FBDR.Events;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FBDR.ViewModels
{
    public class StatusBarViewModel : BindableBase
    {
        #region Fields
        private string _StatusText;
        private int _ProgressMax;
        private int _Progress;
        #endregion

        #region Properties
        public string StatusText
        {
            get => _StatusText;
            set => SetProperty(ref _StatusText, value);
        }


        public Visibility Visibility
            => ProgressVisibility == Visibility.Visible || !string.IsNullOrWhiteSpace(StatusText) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ProgressVisibility
            => ProgressMax > 0 ? Visibility.Visible : Visibility.Collapsed;
        
        public int ProgressMax
        {
            get => _ProgressMax;
            set => SetProperty(ref _ProgressMax, value);
        }
        public int Progress
        {
            get => _Progress;
            set => SetProperty(ref _Progress, value);
        }
        #endregion

        #region Constructors
        public StatusBarViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<Events.StatusUpdateEvent>().Subscribe(OnStatusUpdate);
        }
        #endregion

        #region Methods
        private async void OnStatusUpdate(StatusUpdateEventArgs e)
        {
            StatusText = e.Status;
            if (e.ProgressMax.HasValue)
            {
                ProgressMax = e.ProgressMax.Value;
            }
            if (e.Progress.HasValue)
            {
                Progress = e.Progress.Value;
            }
            ProgressVisibilityChanged();
            VisibilityChanged();
            if (e.DisplayTime.HasValue)
            {
                await Task.Delay(e.DisplayTime.Value);
                if (StatusText == e.Status)
                {
                    StatusText = string.Empty;
                    VisibilityChanged();
                }
            }
        }

        private void VisibilityChanged() =>
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Visibility)));

        private void ProgressVisibilityChanged() =>
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ProgressVisibility)));
        #endregion
    }
}
