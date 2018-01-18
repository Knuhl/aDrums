using System;
using System.Collections.ObjectModel;
using aDrumsLib;
using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using win.WPF.aDrumsManager.Events;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class ComPortsViewModel : DialogViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<string> _availablePorts;
        public ObservableCollection<string> AvailablePorts
        {
            get
            {
                if (_availablePorts == null)
                    BuildAvailablePorts();
                return _availablePorts;
            }
            set
            {
                _availablePorts = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedComPort;
        public string SelectedComPort
        {
            get { return _selectedComPort; }
            set
            {
                _selectedComPort = value;
                RaisePropertyChanged();
                if (value != null)
                    ConnectToPort(value);
            }
        }

        public ComPortsViewModel(IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) : base(dialogCoordinator)
        {
            _eventAggregator = eventAggregator;
        }
        
        private void BuildAvailablePorts()
        {
            _availablePorts = new ObservableCollection<string>(Factory.GetPortNames());
            _availablePorts.Insert(0, SimulatorSerialPort.SimulatedSerialPortName);
            SelectedComPort = _availablePorts.Count == 1 ? _availablePorts[0] : null;
        }
        
        private void ConnectToPort(string comPort)
        {
            DrumManager manager;
            try
            {
                _eventAggregator.GetEvent<ApplicationBusyEvent>().Publish(true);
                manager = SimulatorSerialPort.SimulatedSerialPortName.Equals(comPort)
                    ? new DrumManager(new SimulatorSerialPort())
                    : new DrumManager(comPort);
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString(), "Could not Connect to aDrums at port " + comPort);
                manager = null;
            }
            
            _eventAggregator.GetEvent<PubSubEvent<DrumManager>>().Publish(manager);
            _eventAggregator.GetEvent<ApplicationBusyEvent>().Publish(false);
        }
    }
}
