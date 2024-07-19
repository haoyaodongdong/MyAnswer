
public class Client
{
    private static readonly object InstanceMux = new object();
    private static Client _instance = null;
    public static Client Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (InstanceMux)
                {
                    if (_instance == null)
                    {
                        _instance = new Client();
                    }
                }
            }

            return _instance;
        }
    }


    private Connection _connection;

    public void Send(short packId, byte[] message)
    {
        _connection.Send(packId, message);
    }

    
    public string GetConnectionStatus()
    {
        return _connection != null ? _connection.CurStatus.ToString() : "";
    }

    public virtual void Connect(string host, int port,
        Connection.OnMessageCallback mcb,
        Connection.OnConnectCallback ccb,
        Connection.OnReadyCallback rcb,
        Connection.OnDisconnectCallback dcb)
    {
        if (_connection != null)
        {
            _connection.ClearAllCallback();
            _connection = null;
        }

        _connection = new TcpConnection();
        _connection.ConnectTo(host, port);
        _connection.RegisterOnMessageCallback(mcb);
        _connection.RegisterConnectCallback(ccb);
        _connection.RegisterReadyCallback(rcb);
        _connection.RegisterOnDisconnectCallback(dcb);
    }
    
    public void Disconnect()
    {
        if (_connection != null && _connection.IsConnected)
        {
            _connection.Disconnect(Connection.DisconnectReason.ManualDisconnect);
        }
    }

    public bool IsReady()
    {
        return _connection != null && _connection.IsReady;
    }

    public bool IsConnected()
    {
        return _connection != null && _connection.IsConnected;
    }

    public bool HaveConnection()
    {
        return _connection != null && _connection.HaveConnection;
    }
    
    // 由Unity驱动
    public void Update(double realtime)
    {
        if (_connection != null)
        {
            _connection.ProcessQueue();
            _connection.Update(realtime);
        }
    }
    
}