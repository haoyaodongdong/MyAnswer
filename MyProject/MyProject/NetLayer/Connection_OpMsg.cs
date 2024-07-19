using System;
using System.Collections.Generic;

public abstract partial class Connection
{
    private Queue<OpMessage> _opMsgQueue = new Queue<OpMessage>();
    protected abstract class OpMessage
    {
        public abstract void Process(Connection owner);
    }

    protected class ConnectMsg : OpMessage
    {
        private readonly bool _success;

        public ConnectMsg(bool success) : base()
        {
            _success = success;
        }

        public override void Process(Connection owner)
        {
            owner._onConnectCallback?.Invoke(_success);
        }
    }
    protected class DisconnectMsg : OpMessage
    {
        private readonly DisconnectReason _reason;
        public DisconnectMsg(DisconnectReason reason) : base()
        {
            _reason = reason;
        }

        public override void Process(Connection owner)
        {
           //类似的回掉给connect 先不写了
        }
    }


    protected class RespMsg : OpMessage
    {
        private readonly int _success;
        private readonly byte[] _message;

        public RespMsg(int success, byte[] message) : base()
        {
            _success = success;
            _message = message;
        }

        public override void Process(Connection owner)
        {
            owner._onMessageCallback?.Invoke(_success, _message);
        }
    }

    public void ProcessQueue()
    {
        try
        {
            while (true)
            {
                var msg = DequeueOpMsg();
                if (msg == null)
                {
                    break;
                }

                msg.Process(this);
            }
        }
        catch (Exception ex)
        {
            ThrowExceptionExtraInfo(ex, null);
        }
    }

    protected void EnqueueMsg(OpMessage evt)
    {
        lock (_opMsgQueue)
        {
            _opMsgQueue.Enqueue(evt);
        }
    }

    protected OpMessage DequeueOpMsg()
    {
        lock (_opMsgQueue)
        {
            return _opMsgQueue.Count > 0 ? _opMsgQueue.Dequeue() : null;
        }
    }
}