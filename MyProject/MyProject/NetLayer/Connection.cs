using System;
public abstract partial class Connection
{
    public enum Status
    {
        None,
        Connecting,
        Connected, //socket建立链接，紧接着可以登录了
        Handshaking, //登录中
        Handshaked, //登录过了
        Disconnected,
        Failed
    }
    
    public enum DisconnectReason
    {
        ManualDisconnect,   //  代码主动的、强行断开的情况
        SendError,
        NetworkError,//网络错误
        ServerDisconnected, //  下行Msg收到解不出来 或者收不到
        ConnectErrorWhenConnecting,       //  TCP Socket 创建失败
        ConnectError,       //  TCP Socket 连接失败
    }
    
    public delegate void OnConnectCallback(bool success);
    public delegate void OnReadyCallback(bool success);
    public delegate void OnMessageCallback(int code, byte[] bytes);
    public delegate void OnDisconnectCallback(DisconnectReason r);

    private OnConnectCallback _onConnectCallback;
    private OnReadyCallback _onReadyCallback;//牵扯到登录就不继续了
    private OnMessageCallback _onMessageCallback;   
    private OnDisconnectCallback _onDisconnectCallback;


    private Status _curStatus;
    

    

    public Status CurStatus => _curStatus;


    public bool IsConnected => _curStatus == Status.Connected || _curStatus == Status.Handshaking || IsReady;

    public bool IsReady => _curStatus == Status.Handshaked;

    public bool HaveConnection => _curStatus >= Status.Connecting && _curStatus <= Status.Handshaked;

    public void ConnectTo(string host, int port)
    {
        Create(host, port);
    }

    public void Disconnect(DisconnectReason reason)
    {
        try
        {
            _exitEvent.Set();
            if (_curStatus > Status.Connected)
            {
                _curStatus = Status.Disconnected;
                EnqueueMsg(new DisconnectMsg(reason)); //加入消息队列
            }
        }
        catch (ObjectDisposedException ex)
        {
            //error 
        }
    }
    
   
    public void ClearAllCallback()
    {
        _onConnectCallback = null;
        _onReadyCallback = null;
        _onMessageCallback = null;
    }

    public void RegisterConnectCallback(OnConnectCallback cb)
    {
        if (_onConnectCallback == null)
        {
            _onConnectCallback = cb;
        }
        else
        {
            _onConnectCallback += cb;
        }
    }


    public void RegisterReadyCallback(OnReadyCallback cb)
    {
        if (_onConnectCallback == null)
        {
            _onReadyCallback = cb;
        }
        else
        {
            _onReadyCallback += cb;
        }
    }




    public void RegisterOnMessageCallback(OnMessageCallback cb)
    {
        if (_onMessageCallback == null)
        {
            _onMessageCallback = cb;
        }
        else
        {
            _onMessageCallback += cb;
        }
    }
    
    public void RegisterOnDisconnectCallback(OnDisconnectCallback cb)
    {
        if (_onDisconnectCallback == null)
        {
            _onDisconnectCallback = cb;
        }
        else
        {
            _onDisconnectCallback += cb;
        }
    }


    /// <summary>
    /// 解包出现问题走重连逻辑
    /// </summary>
    /// <param name="ex"></param>
    protected void PacketExceptionAndDisconnect(Exception ex,DisconnectReason errorResason = DisconnectReason.ManualDisconnect)
    {
        //
    }


    private void ThrowExceptionExtraInfo(Exception ex, string info)
    {
        _curStatus = Status.Failed;
    }

    protected void OnConnected(bool success)
    {
        if (success)
        {
            //登录 并开始解析协议
            StartRead(); //解析 下行
            StartSend();//轮巡 发送上行
    
            _curStatus = Status.Handshaked;//登录代码这里处理，这里直接算登录了
            EnqueueMsg(new ConnectMsg(true));
        }
        else
        {
            _curStatus = Status.Failed;
            EnqueueMsg(new ConnectMsg(false));
        }
    }

    public void Update(double realtime)
    {
        SendUpdate(realtime);
    }
}