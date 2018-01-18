using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using Prism.Regions;
using win.WPF.aDrumsManager.Events;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class ManagerShellViewModel : DialogViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        private string _title = "aDrums Manager";
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }

        public ManagerShellViewModel(IDialogCoordinator dialogCoordinator, IRegionManager regionManager,
            IEventAggregator eventAggregator) : base(dialogCoordinator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationBusyEvent>().Subscribe(OnApplicationBusy);
        }

        private void OnApplicationBusy(bool busy)
        {
            Application.Current.MainWindow.Cursor = busy ? Cursors.Wait : Cursors.Arrow;
        }
    }
}
