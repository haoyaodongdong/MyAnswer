using System;
using System.Collections.Generic;
using System.IO;
public abstract partial class Connection
{
    protected Stream netStream = null;

    protected abstract void Create(string ip, int port);

    protected Stream GetStream()
    {
        return netStream;
    }

    private void Close()
    {
        try
        {
            if (netStream != null)
            {
                netStream.Dispose();
            }
            netStream = null;
        }
        catch (Exception ex)
        {
        }
    }

    private IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, System.Object state)
    {
        return netStream.BeginRead(buffer, offset, size, callback, state);
    }

    private int EndRead(IAsyncResult asr)
    {
        int length = 0;
        try
        {
            length = netStream.EndRead(asr);
        }
        catch (InvalidOperationException ie)
        {
               
        }
        catch (IOException ioe)
        {
             
        }
        return length;
    }

    private void SocketSend(List<ArraySegment<byte>> buffer)
    {
        if (buffer != null)
        {
            try
            {
                for (var i = 0; i < buffer.Count; i++)
                {
                    byte[] array = buffer[i].Array;
                    if (array != null) netStream.Write(array, buffer[i].Offset, buffer[i].Count);
                }
            }
            catch (Exception ex)
            {
                PacketExceptionAndDisconnect(ex);
            }
        }
    }

    protected void ConnectSuccess(Stream ns)
    {
        netStream = ns;
        OnConnected(true);
    }
}