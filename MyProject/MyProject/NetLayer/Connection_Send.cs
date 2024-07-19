using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

public abstract partial class Connection
{
    private EventWaitHandle _messageWaiting = new EventWaitHandle(false, EventResetMode.AutoReset);//用于线程阻塞
    private EventWaitHandle _exitEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
    
    private List<ArraySegment<byte>> _pendingSendBuffers = new List<ArraySegment<byte>>();
    private List<ArraySegment<byte>> _activeSendBuffers = new List<ArraySegment<byte>>();
    private readonly object _sendLock = new object(); //线程锁
    private Thread _sendThread;
    /// <summary>
    /// 心跳
    /// </summary>
    /// <param name="realtime"></param>
    private void SendUpdate(double realtime)
    {
        //可以做心跳相关逻辑。计算时间
    }

    public void Send(short packId, byte[] message)
    {
        if (IsReady)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                int totalSize = 0;
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int msgLen = 0;
                    if (message == null)
                    {
                        msgLen = 0;
                    }
                    else
                    {
                        msgLen = message.Length;
                    }
                    totalSize = SEND_HEAD_BUFFER_SIZE + msgLen;

                    writer.Write(IPAddress.HostToNetworkOrder(msgLen)); //排除低 位字节 高位字节不同主机排序问题
                    writer.Write(IPAddress.HostToNetworkOrder(packId));
                    //writer.Write(IPAddress.HostToNetworkOrder(++_sendSn));对应 _msgSn 预留下
                    if (message != null)
                    {
                        writer.Write(message);
                    }
                    //proto 内容 这里可以加上计数
                }
                
                lock (_sendLock)        //注意异步和加lock
                {
                    _pendingSendBuffers.Add(new ArraySegment<byte>(stream.GetBuffer(), 0, totalSize));//加入缓存
                    _messageWaiting.Set(); //解除线程阻塞
                }
                
            }
        }

    }

    private void SendThreadProc()
    {
        WaitHandle[] handles = { _exitEvent, _messageWaiting };

        try
        {
            while (true)
            {
                int idx = WaitHandle.WaitAny(handles, -1);
                lock (_sendLock)
                {
                    if (_pendingSendBuffers.Count > 0)
                    {
                        List<ArraySegment<byte>> temp = _activeSendBuffers;
                        _activeSendBuffers = _pendingSendBuffers;
                        _pendingSendBuffers = temp;
                    }

                    if (_activeSendBuffers.Count > 0)
                    {
                        SocketSend(_activeSendBuffers);
                        _activeSendBuffers.Clear();
                    }
                }

                if (idx == 0)
                {
                    break;
                }
            }
        }
        catch (NetException ex)
        {
            PacketExceptionAndDisconnect(ex, DisconnectReason.SendError);
        }
        catch (Exception ex)
        {
            PacketExceptionAndDisconnect(ex, DisconnectReason.NetworkError);
        }
        finally
        {
            Close();
        }
    }

    private void StartSend()
    {
        _sendThread = new Thread(SendThreadProc)
        {
            Name = "SendThread"
        }; //单独开个线程，网络层和业务层异步处理
        _sendThread.Start();
    }
}