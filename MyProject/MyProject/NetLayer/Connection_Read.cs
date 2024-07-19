using System;
using System.IO;
using System.Net;

public abstract partial class Connection
{
    public const int HEAD_BUFFER_SIZE = 12;
    public const int SEND_HEAD_BUFFER_SIZE = 8;
    //head buffers
    private byte[] _headBuffer = new byte[HEAD_BUFFER_SIZE];
    private byte[] _bodyBuffer = null;
    
    private int _headBufferOffset = 0;
    private int _bodyBufferOffset = 0;
    
    private int _remainSize = 0;
    
    private int _messageLength = 0;
    private int _bodyBufferExpectedSize = 0;
    private int _packId = 0;

    private byte[] GetBufferFromPool(int expetsize)
    {
        return new byte[expetsize];
    }

    private void StartRead()
    {
        ResetOffset();
        StartHeaderRead(true);
    }

    private void ResetOffset()
    {
        _headBufferOffset = _remainSize;
        _bodyBuffer = null;
        _bodyBufferOffset = 0; 
        _bodyBufferExpectedSize = 0;
        _packId = 0;
        _messageLength = 0;
        Array.Clear(_headBuffer, 0, HEAD_BUFFER_SIZE);
    }
    private void StartHeaderRead(bool resetOffset)
    {
        BeginRead(_headBuffer, _headBufferOffset, _headBuffer.Length - _headBufferOffset, ReadHeaderCallback, null);
    }
    private void ReadHeaderCallback(IAsyncResult asr)
    {
          try
          {
              if (!IsReady)
              {
                  return;
              }
              int bytesRead = EndRead(asr);
              if (bytesRead == 0)
              {
                  Disconnect(DisconnectReason.ServerDisconnected);
                  return;
              }
              _headBufferOffset += bytesRead;
              if (_headBufferOffset <  HEAD_BUFFER_SIZE)
              {
                  StartHeaderRead(false);
                  return;
              }
              using (MemoryStream stream = new MemoryStream(_headBuffer, false))
              {
                  using (BinaryReader reader = new BinaryReader(stream))
                  {
                      _messageLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                      _packId = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                      // var number = IPAddress.NetworkToHostOrder(reader.ReadInt32()); 预留一下
                      _bodyBufferExpectedSize = _messageLength; 

                      //这种是不带返回数据的
                      if (_bodyBufferExpectedSize <= 0)
                      {
                          EnqueueMsg(new RespMsg(_packId, null));
                          StartHeaderRead(true);
                          return;
                      }
                      if (_headBufferOffset > HEAD_BUFFER_SIZE)
                      {
                          throw new NetException("Head length overflow!!");
                      }
                      
                      _bodyBuffer = GetBufferFromPool(_bodyBufferExpectedSize);
                  }
              }
              _bodyBufferOffset = 0;
              StartMessageRead(_bodyBuffer);
          }
          catch (Exception ex)
          {
              PacketExceptionAndDisconnect(ex, DisconnectReason.NetworkError);
          }
    }

    private void StartMessageRead(byte[] buffer)
    {
        BeginRead(buffer, _bodyBufferOffset, _bodyBufferExpectedSize - _bodyBufferOffset, ReadBodyCallback, null);
    }

    private void ReadBodyCallback(IAsyncResult asr)
    {
        try
        {
            if (!IsReady)
            {
                return;
            }
            int bytesRead = EndRead(asr);
            if (bytesRead == 0)
            {
                Disconnect(DisconnectReason.ServerDisconnected);
                return;
            }
            _bodyBufferOffset += bytesRead;
            if (_bodyBufferOffset < _bodyBufferExpectedSize)
            {
                StartMessageRead(_bodyBuffer);
                return;
            }

            if (_bodyBufferOffset > _bodyBufferExpectedSize)
            {
                _remainSize = _bodyBufferOffset - _bodyBufferExpectedSize;
            }

            using (MemoryStream stream = new MemoryStream(_bodyBuffer, 0, _bodyBufferExpectedSize, false))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var bytes = reader.ReadBytes(_bodyBufferExpectedSize);
                    EnqueueMsg(new RespMsg(_packId, bytes));
                }
            }

            StartHeaderRead(true);
        }
        catch (Exception ex)
        {
            PacketExceptionAndDisconnect(ex, DisconnectReason.NetworkError);
        }
    }
}