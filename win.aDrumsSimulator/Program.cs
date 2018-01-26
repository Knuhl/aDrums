using System;
using aDrumsLib;
using NamedPipeWrapper;

namespace win.aDrumsSimulator
{
    internal class Program
    {
        private readonly NamedPipeServer<byte[]> _server;
        private BoardSimulation _connection;

        public static void Log(string s)
        {
            Console.WriteLine(s);
        }

        static void Main(string[] args)
        {
            var pg = new Program();
            pg.Run();
        }

        public Program()
        {
            _server = new NamedPipeServer<byte[]>("aDrumsSimulatorPipe");
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.ClientMessage += OnClientMessage;
            _server.Error += OnError;
            _server.Start();
            Log("Pipe-Server started");
        }

        public void Run()
        {
            while (true)
            {
                try
                {
                    if (_connection != null && _connection.Answers.Count > 0)
                        _server.PushMessage(_connection.Answers.Dequeue());
                }
                catch (Exception e)
                {
                    OnError(e);
                }
            }
        }

        private void OnError(Exception exception)
        {
            Log(exception.ToString());
        }

        private void OnClientMessage(NamedPipeConnection<byte[], byte[]> connection, byte[] message)
        {
            //Log($"Received Message from Client {connection.Id}");
            if (message.Length > 0)
                _connection?.ExecuteCommand(new SysExMessage(message));
            else
                _server.PushMessage(message);
        }

        private void OnClientDisconnected(NamedPipeConnection<byte[], byte[]> connection)
        {
            Log($"Client {connection.Id} disconnected");
            _connection = null;
        }

        private void OnClientConnected(NamedPipeConnection<byte[], byte[]> connection)
        {
            Log($"Client {connection.Id} connected");
            if (_connection != null) Log("Previous Connection is now ignored!");
            _connection = new BoardSimulation();
        }
    }
}
