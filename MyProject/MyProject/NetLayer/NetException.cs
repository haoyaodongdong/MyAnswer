using System;

public class NetException : Exception
{
    private Exception _ex;
    public Exception exception => _ex;

    public NetException(string info)
    {
        _ex = new Exception(info);
    }

    public NetException(string msg, Exception ex)
    {
        _ex = new Exception(msg, ex);
    }
}