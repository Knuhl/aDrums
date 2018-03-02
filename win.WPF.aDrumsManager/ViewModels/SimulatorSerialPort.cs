using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using aDrumsLib;
using NamedPipeWrapper;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class SimulatorSerialPort : ISerialPort
    {
        public const string SimulatedSerialPortName = "COM_SIMULATED";

        private readonly NamedPipeClient<byte[]> _client;

        private readonly Queue<byte[]> _answers = new Queue<byte[]>();
        private byte[] _currentlyReadAnswer;
        private int _currentReadOffset;
        private bool _messageWasReceived;

        public int BaudRate { get; set; }

        public int BytesToRead { get; set; }

        public bool DtrEnable { get; set; }

        public bool IsOpen { get; set; }

        public string PortName { get; set; } = SimulatedSerialPortName;

        public int ReadTimeout { get; set; } = 30;
        
        public SimulatorSerialPort()
        {
            _client = new NamedPipeClient<byte[]>("aDrumsSimulatorPipe");
            _client.ServerMessage += OnServerMessage;
            _client.Disconnected += OnDisconnected;
            _client.Error += OnError;
            _client.AutoReconnect = false;
        }

        private void OnError(Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }

        private void OnDisconnected(NamedPipeConnection<byte[], byte[]> connection)
        {
        }

        private void OnServerMessage(NamedPipeConnection<byte[], byte[]> connection, byte[] message)
        {
            if (message.Length > 0)
                _answers.Enqueue(message);
            _messageWasReceived = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            _client.Stop();
            IsOpen = false;
        }

        public void Open()
        {
            if (IsOpen) return;
            _client.Start();
            IsOpen = true;
            _messageWasReceived = false;
        }
        
        public int ReadByte()
        {
            if (_currentlyReadAnswer == null)
            {
                DateTime startTime = DateTime.Now;
                while (_answers.Count < 1 && (DateTime.Now - startTime).Seconds < ReadTimeout)
                {
                }

                _currentlyReadAnswer = _answers.Dequeue()?.ToArray();
                _currentReadOffset = 0;
            }
            if (_currentlyReadAnswer == null)
                return -1;
            int result = _currentlyReadAnswer[_currentReadOffset++];

            if (_currentReadOffset >= _currentlyReadAnswer.Length)
                _currentlyReadAnswer = null;

            return result;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public string ReadExisting()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            DateTime max = DateTime.Now.AddSeconds(2);
            while (!_messageWasReceived)
            {
                if (DateTime.Now > max) throw new TimeoutException();
                ThreadPool.QueueUserWorkItem(wb => { _client.PushMessage(new byte[0]); });
                Thread.Sleep(50);
            }

            //Debug.WriteLine("SerialPort Received message");
            _client.PushMessage(buffer.Skip(offset + 1).Take(count - 2).ToArray());
        }
    }
}
