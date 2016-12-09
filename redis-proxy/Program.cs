// ***********************************************************
// File     : Program.cs
// Author   : beyondpn
// Created  : 2016年12月9日
// Porpuse  : FileDescription
// ***********************************************************

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisProxy
{
    public class Program
    {
        private const int BufferSize = 4096;
        private static byte[] _selectDbCmd;
        private static Config _config;

        public static void Main(string[] args)
        {
            _config = Config.Load();

            int dbStrLength = _config.RedisDB.ToString().Length;
            _selectDbCmd = Encoding.ASCII.GetBytes($"*2\r\n$6\r\nselect\r\n${dbStrLength}\r\n{_config.RedisDB}\r\n");

            new Program().Start().Wait();
        }

        private async Task Start()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TcpListener listener = new TcpListener(IPAddress.Parse(_config.ProxyBindHost), _config.ProxyBindPort);
            try
            {
                listener.Start();
                Console.WriteLine($"redis-proxy listening at {_config.ProxyBindHost}:{_config.ProxyBindPort}");
                await AcceptClientAsync(listener, cts.Token);
            }
            finally
            {
                cts.Cancel();
                listener.Stop();
            }
        }

        private async Task AcceptClientAsync(TcpListener listener,CancellationToken ct)
        {
            uint clientId = 0;
            while(!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                clientId++;
                Task t = ProxyAsync(client, clientId, ct);
            }
        }

        private async Task ProxyAsync(TcpClient client,uint clientIndex,CancellationToken ct)
        {
            using (TcpClient proxy = new TcpClient())
            {
                bool dbSelected = await SelectRedisDBAsync(proxy, client,ct);
                if (dbSelected)
                {
                    using (client)
                    {
                        var clientStream = client.GetStream();
                        var proxyStream = proxy.GetStream();

                        var upStream = StreamAsync(clientStream, proxyStream, ct);
                        var downStream = StreamAsync(proxyStream, clientStream, ct);

                        await Task.WhenAny(upStream, downStream).ConfigureAwait(false);                      
                    }
                }
                else
                {
                    client.Dispose();
                }
            }
        }

        private async Task<bool> SelectRedisDBAsync(TcpClient proxy, TcpClient client,CancellationToken ct)
        {
            byte[] buf = new byte[BufferSize];
            await proxy.ConnectAsync(_config.RedisHost, _config.RedisPort);
            var stream = proxy.GetStream();
            stream.Write(_selectDbCmd, 0, _selectDbCmd.Length);
            var read = await stream.ReadAsync(buf, 0, buf.Length,ct);
            if (read != 5) return false;
            string resp = Encoding.ASCII.GetString(buf, 0, read);
            return "+OK\r\n" == resp;
        }
        
        private async Task StreamAsync(NetworkStream inStream,NetworkStream outStream,CancellationToken ct)
        {
            byte[] buf = new byte[BufferSize];
            while(!ct.IsCancellationRequested)
            {
                var read = await inStream.ReadAsync(buf, 0, buf.Length, ct);
                if (read == 0) break;
                await outStream.WriteAsync(buf, 0, read, ct).ConfigureAwait(false);
            }
        }
    }
}