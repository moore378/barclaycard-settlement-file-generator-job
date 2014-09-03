using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MjhGeneral.EasyTcp
{
    public class EasyTcpServer : IDisposable
    {
        Task listeningTask;
        CancellationTokenSource cancelListeningTask;
        bool listening = false;

        public EasyTcpServer(int port)
        {
            this.port = port;
        }

        public Task Listen(Action<TcpClient> connectionReceived)
        {
            var newCancelListeningTask = new CancellationTokenSource();
            var oldCancel = Interlocked.Exchange(ref this.cancelListeningTask, newCancelListeningTask);
            if (oldCancel != null)
                oldCancel.Cancel();
            listeningTask = ListenInternal(port, cancelListeningTask.Token, connectionReceived);
            return listeningTask;
        }

        protected async Task ListenInternal(int port, CancellationToken cancel, Action<TcpClient> connectionReceived)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listening = true;
            try
            {
                cancel.Register(() => listener.Stop());
                while (!cancel.IsCancellationRequested)
                {
                    var newConnection = await listener.AcceptTcpClientAsync();
                    if (newConnection != null)
                    {
                        connectionReceived(newConnection);
                    }
                }
            }
            catch (ObjectDisposedException) // This is a natural exception that seems to happen when the server stops while its listening
            {
            }
            finally
            {
                listener.Stop();
            }
        }

        private int port;

        public void Dispose()
        {
            Stop().Wait();
        }

        public Task Stop()
        {
            if (listening)
            {
                cancelListeningTask.Cancel();
                listening = false;
                return listeningTask;
            }
            else
                return TaskEx.FromResult(0);
        }
    }
}
