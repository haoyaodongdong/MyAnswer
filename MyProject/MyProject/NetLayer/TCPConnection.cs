using System;
using System.Net;
using System.Net.Sockets;

public class TcpConnection : Connection
{
    protected override void Create(string ip, int port)
        {
            ip = ip.TrimEnd('\r');
            if (IPAddress.TryParse(ip, out var ipAddress))
            {
                ip = ipAddress.ToString();
            }
            else
            {
                var ips = Dns.GetHostAddresses(ip);
                if (ips.Length > 0)
                {
                    ipAddress = ips[0];
                    ip = ips[0].ToString();
                }
            }
            try
            {
                TcpClient tcpClient = new TcpClient(ipAddress.AddressFamily);
                tcpClient.BeginConnect(IPAddress.Parse(ip), port, ConnectedCallback, tcpClient);
            }
            catch (SocketException ex)
            {
                OnConnected(false);
                PacketExceptionAndDisconnect(new NetException("SocketException:", ex),
                    DisconnectReason.ConnectErrorWhenConnecting);
            }
            catch (Exception e)
            {
                OnConnected(false);
                PacketExceptionAndDisconnect(new NetException("Exception", e),
                    DisconnectReason.ConnectErrorWhenConnecting);
            }
        }

        private void ConnectedCallback(IAsyncResult asr)
        {
            try
            {
                if (asr.AsyncState is TcpClient t && t.Connected)
                {
                    t.EndConnect(asr);      
                    t.NoDelay = true;
                    t.ReceiveBufferSize = 32 * 1024;//自己设置下缓冲值大小

                    //把连接这样保存起来
                    RegisterOnDisconnectCallback(
                        (r) => {
                            try
                            {                                
                                if (netStream != null)// 因为生命周期不同的关系 尝试关闭一下                                        
                                {
                                    netStream.Dispose();
                                    netStream.Close();
                                    netStream = null;
                                }
                                if (t.Connected){
                                    t.Client.Shutdown(SocketShutdown.Both);// connected判定必须 否则有异常                     
                                }
                            }
                            catch (Exception ex)
                            {
                                // 出错日志
                            }
                            finally
                            {
                               
                                t.Client.Close();
                                t.Close();
                            }
                        }
                    );
                    NetworkStream s = t.GetStream();
                    ConnectSuccess(s);
                }
                else
                {
                    OnConnected(false);
                }
            }
            catch (Exception ex)
            {
                OnConnected(false);
                PacketExceptionAndDisconnect(new NetException("*Exception try to connect address*2", ex), DisconnectReason.ConnectError);
            }
        } 
}