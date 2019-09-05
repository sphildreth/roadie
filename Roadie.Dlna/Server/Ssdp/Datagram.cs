using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Roadie.Dlna.Server.Ssdp
{
    internal sealed class Datagram
    {
        public readonly IPEndPoint EndPoint;

        public readonly IPAddress LocalAddress;

        public readonly string Message;

        public readonly bool Sticky;

        public uint SendCount { get; private set; }

        public Datagram(IPEndPoint endPoint, IPAddress localAddress,
              string message, bool sticky)
        {
            EndPoint = endPoint;
            LocalAddress = localAddress;
            Message = message;
            Sticky = sticky;
            SendCount = 0;
        }

        public void Send()
        {
            var msg = Encoding.ASCII.GetBytes(Message);
            try
            {
                var client = new UdpClient();
                client.Client.Bind(new IPEndPoint(LocalAddress, 0));
                client.Ttl = 10;
                client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);
                client.BeginSend(msg, msg.Length, EndPoint, result =>
                {
                    try
                    {
                        client.EndSend(result);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);
                    }
                    finally
                    {
                        try
                        {
                            client.Close();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }, null);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            ++SendCount;
        }
    }
}