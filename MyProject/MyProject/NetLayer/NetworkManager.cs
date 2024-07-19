
public class NetworkManager
{
    private Client _client = null;

    public Client client => _client;
        
    public void Init()
    {
        _client = new Client();
    }
        
    /// <summary>
    /// 重连的情况
    /// </summary>
    public void Reconnect()
    {
        Disconnect();
        ConnectClient();
    }

    /// <summary>
    /// 断开连接的释放
    /// </summary>
    private void Disconnect()
    {
        if (_client != null)
        {
            _client.Disconnect();
        }
    }

    private void ConnectClient()
    {
        _client.Connect("host", 1000, OnMessageCallback, OnConnectCallback, OnReadyCallback, OnDisconnectCallback);
    }

    /// <summary>
    /// 业务层发送协议
    /// </summary>
    /// <param name="packId"></param>
    /// <param name="data"></param>
    public void SendMessage(short packId, byte[] data)
    {
        _client.Send(packId, data);
    }
        
    /// <summary>
    /// 业务层 接收
    /// </summary>
    /// <param name="code"></param>
    /// <param name="bytes"></param>
    private void OnMessageCallback(int code, byte[] bytes)
    {
        //业务层
    }
        
    /// <summary>
    /// HandShake之前 网络连接的回调
    /// </summary>
    /// <param name="success"></param>
    private void OnConnectCallback(bool success)
    {
        if (success)
        {
            
        }
        else
        {
            CheckFail();
        }
    }

    /// <summary>
    /// 重连 
    /// </summary>
    private void CheckFail()
    {
        //这里可以搞个计数，决定后续游戏进程
    }
    
    /// <summary>
    /// HandShake成功与否的回调
    /// </summary>
    /// <param name="success"></param>
    private void OnReadyCallback(bool success)
    {
        CheckFail();
    }

    private void OnDisconnectCallback(Connection.DisconnectReason reason)
    {

    }
}

