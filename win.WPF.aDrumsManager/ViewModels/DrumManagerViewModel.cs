using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using aDrumsLib;
using MahApps.Metro.Controls.Dialogs;
using Prism.Events;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class DrumManagerViewModel : ViewModelBase
    {
        private DrumManager _drumManager;
        public DrumManager DrumManager
        {
            get { return _drumManager; }
            set
            {
                _drumManager = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<MidiTrigger> _triggerCollection;
        public ObservableCollection<MidiTrigger> TriggerCollection
        {
            get { return _triggerCollection; }
            set
            {
                _triggerCollection = value;
                RaisePropertyChanged();
            }
        }

        private MidiTrigger _selectedTrigger;
        public MidiTrigger SelectedTrigger
        {
            get { return _selectedTrigger; }
            set
            {
                _selectedTrigger = value;
                RaisePropertyChanged();
            }
        }

        public DrumManagerViewModel(IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) : base(dialogCoordinator)
        {
            eventAggregator.GetEvent<PubSubEvent<DrumManager>>().Subscribe(OnDrumManagerChanged);
        }

        private void OnDrumManagerChanged(DrumManager drumManager)
        {
            DrumManager?.Dispose();
            DrumManager = drumManager;
            TriggerCollection = new ObservableCollection<MidiTrigger>(DrumManager?.Triggers ?? new List<MidiTrigger>());
            SelectedTrigger = TriggerCollection.FirstOrDefault();
        }
    }
}
