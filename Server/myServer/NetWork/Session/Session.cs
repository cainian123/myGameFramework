using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Session
{
    private int _defaultPacketLength = 4;//协议格式:4位长度+4位id+内容;此处指长度,4位;
    private int _defaultMaxPacketLength = 1024 * 64;

    private readonly string _name;
    private SessionType _sessionType;
    private Socket _socket;
    private SessionReceiver _receiver;
    private bool _active;
    private bool _disposed;

    private IPAddress _ipAddress;
    private int _port;

    private object thisLock = new object();

    public SessionConnectedEventHandler ConnectedHandler;
    public SessionClosedEventHandler ClosedHandler;
    public SessionErrorEventHandler ErrorHandler;
    public SessionCustomErrorEventHandler CustomErrorHandler;
    public SessionReceiveEventHandler ReceiveHandler;
    public SessionSendEventHandler SendHandler;

    public string Name { get { return _name; } }
    public SessionType Type { get { return _sessionType; } }
    public bool Active { get { return _active; } }
    public Socket socket { get { return _socket; } set { _socket = value; } }
    public IPAddress ipAddress { get { return _ipAddress; } set { _ipAddress = value; } }
    public int Port { get { return _port; } set { _port = value; } }

    public Session(string name)
    {
        _name = name ?? string.Empty;
        _sessionType = SessionType.Unknown;
        _socket = null;
        _receiver = null;
        _active = false;
        _disposed = false;
        ConnectedHandler = null;
        ClosedHandler = null;
        ErrorHandler = null;
        CustomErrorHandler = null;
        ReceiveHandler = null;
        SendHandler = null;
    }

    public void Update()
    {

    }

    #region Session Info

    public bool Connected
    {
        get
        {
            if (_socket != null)
            {
                return _socket.Connected;
            }
            return false;
        }
    }

    public IPAddress LocalIPAddress
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            IPEndPoint ipEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            if (ipEndPoint == null)
            {
                throw new Exception("localIPAddress is invalid.");
            }
            return ipEndPoint.Address;
        }
    }

    public int LocalPort
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            IPEndPoint ipEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            if (ipEndPoint == null)
            {
                throw new Exception("LocalPort is invalid.");
            }
            return ipEndPoint.Port;
        }
    }

    public IPAddress RemoteIPAddress
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            IPEndPoint ipEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            if (ipEndPoint == null)
            {
                throw new Exception("RemoteIPAddress is invalid.");
            }
            return ipEndPoint.Address;
        }
    }

    public int RemotePort
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            IPEndPoint ipEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            if (ipEndPoint == null)
            {
                throw new Exception("RemotePort is invalid.");
            }
            return ipEndPoint.Port;
        }
    }

    public int ReceiveBufferSize
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            return _socket.ReceiveBufferSize;
        }
        set
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            _socket.ReceiveBufferSize = value;
        }
    }

    public int SendBufferSize
    {
        get
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            return _socket.SendBufferSize;
        }
        set
        {
            if (_socket == null)
            {
                throw new Exception("not initialize network.");
            }
            _socket.SendBufferSize = value;
        }
    }

    #endregion

    #region Session Connect

    public void Connect(IPAddress ipAddress, int port)
    {
        Initialize(ipAddress.AddressFamily);
        _ipAddress = ipAddress;
        _port = port;
        if (_socket == null)
        {
            string errorMessage = "initialize failure.";
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, errorMessage);
                return;
            }
            throw new Exception(errorMessage);
        }
        try
        {
            SessionParam param = new SessionParam(_socket, _ipAddress, _port);
            _socket.BeginConnect(_ipAddress, _port, ConnectCallback, param);
        }
        catch (Exception exception)
        {
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, exception.Message);
                return;
            }
            throw;
        }
    }

    private void Initialize(AddressFamily addressFamily)
    {
        if (_socket != null)
        {
            Close();
            _socket = null;
        }
        switch (addressFamily)
        {
            case AddressFamily.InterNetwork:
                _sessionType = SessionType.IPv4;
                break;
            case AddressFamily.InterNetworkV6:
                _sessionType = SessionType.IPv6;
                break;
            default:
                throw new Exception(string.Format("Not supported address family '{0}'.", addressFamily.ToString()));
        }
        _socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        _receiver = new SessionReceiver(_defaultMaxPacketLength);
        _receiver.Reset(_defaultPacketLength);
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        SessionParam param = (SessionParam)ar.AsyncState;
        try
        {
            param.socket.EndConnect(ar);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.ConnectError, exception.Message);
                return;
            }
            throw;
        }
        _active = true;
        if (ConnectedHandler != null)
        {
            ConnectedHandler(this, param);
        }
        Receive();
    }

    #endregion

    #region Session Send

    public void Send(byte[] buffer, int offset, int size)
    {
        if (_socket == null)
        {
            string errorMessage = "session not initialize.";
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, errorMessage);
                return;
            }
            throw new Exception(errorMessage);
        }
        try
        {
            SessionParam param = new SessionParam(_socket, _ipAddress, _port);
            _socket.BeginSend(buffer, offset, size, SocketFlags.None, SendCallback, param);
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, exception.Message);
                return;
            }
            throw;
        }
    }

    public void Send<T>(T packet) where T : Packet
    {
        if (_socket == null)
        {
            string errorMessage = "session not initialize.";
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, errorMessage);
                return;
            }
            throw new Exception(errorMessage);
        }
        if (packet == null)
        {
            string errorMessage = "Packet is invalid.";
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StateError, errorMessage);
                return;
            }
            throw new Exception(errorMessage);
        }
        try
        {
            int length = 0;
            int packetLength = 0;
            byte[] packetBuffer = new byte[_defaultMaxPacketLength];//TODO:内存池;
            using (MemoryStream memoryStream = new MemoryStream(packetBuffer, true))
            {
                memoryStream.Seek(_defaultPacketLength, SeekOrigin.Begin);
                SessionUtil.Serialize(this, memoryStream, packet);
                length = (int)memoryStream.Position;
            }
            packetLength = length - _defaultPacketLength;
            ConverterUtility.GetBytes(packetLength).CopyTo(packetBuffer, 0);
            Send(packetBuffer, 0, length);
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.SerializeError, exception.ToString());
                return;
            }
            throw;
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        SessionParam param = (SessionParam)ar.AsyncState;

        int bytesSent = 0;
        try
        {
            bytesSent = param.socket.EndSend(ar);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.SendError, exception.Message);
                return;
            }

            throw;
        }
        if (SendHandler != null)
        {
            SendHandler(this, bytesSent, param);
        }
    }

    #endregion

    #region Session Receive

    private void Receive()
    {
        try
        {
            _socket.BeginReceive(_receiver.GetBuffer(), _receiver.ReceivedLength, _receiver.Length - _receiver.ReceivedLength, SocketFlags.None, ReceiveCallback, _socket);
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.ReceiveError, exception.Message);
                return;
            }
            throw;
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState;
        int bytesReceived = 0;
        try
        {
            bytesReceived = socket.EndReceive(ar);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.ReceiveError, exception.Message);
                return;
            }

            throw;
        }
        if (bytesReceived <= 0)
        {
            Close();
            return;
        }
        _receiver.ReceivedLength += bytesReceived;
        if (_receiver.ReceivedLength < _receiver.Length)
        {
            Receive();
            return;
        }
        bool processSuccess = false;
        try
        {
            processSuccess = Process();//返回false,继续接收;
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.StreamError, exception.Message);
                return;
            }

            throw;
        }
        if (processSuccess)
        {
            Receive();
            return;
        }
    }

    private bool Process()
    {
        if (_receiver.ReceivedLength != _receiver.Length)
        {
            throw new Exception(string.Format("Receive length '{0}' is not equal to length '{1}'.", _receiver.ReceivedLength.ToString(), _receiver.Length.ToString()));
        }
        if (_receiver.Length < _defaultPacketLength)
        {
            throw new Exception(string.Format("Length '{0}' is smaller than length header.", _receiver.Length.ToString()));
        }
        if (_receiver.Length == _defaultPacketLength)
        {
            int packetLength = ConverterUtility.GetInt32(_receiver.GetBuffer());
            if (packetLength <= 0)
            {
                string errorMessage = string.Format("Packet length '{0}' is invalid.", packetLength.ToString());
                if (ErrorHandler != null)
                {
                    ErrorHandler(this, SessionErrorCode.HeaderError, errorMessage);
                    return false;
                }
                throw new Exception(errorMessage);
            }
            _receiver.Length += packetLength;
            if (_receiver.Length > _receiver.BufferSize)
            {
                string errorMessage = string.Format("Length '{0}' is larger than buffer size '{1}'.", _receiver.Length.ToString(), _receiver.BufferSize.ToString());
                if (ErrorHandler != null)
                {
                    ErrorHandler(this, SessionErrorCode.OutOfRangeError, errorMessage);//未接收完成,继续;
                    return false;
                }
                throw new Exception(errorMessage);
            }
            return true;
        }
        Packet packet = null;
        try
        {
            int packetLength = _receiver.Length - _defaultPacketLength;
            object customErrorData = null;
            using (MemoryStream memoryStream = new MemoryStream(_receiver.GetBuffer(), _defaultPacketLength, packetLength, false))
            {
                lock (thisLock)
                {
                    packet = SessionUtil.Deserialize(this, memoryStream, out customErrorData);
                }
            }
            _receiver.Reset(_defaultPacketLength);
            if (packet == null)
            {
                if (CustomErrorHandler != null)
                {
                    CustomErrorHandler(this, customErrorData);
                }
            }
            else
            {
                if (ReceiveHandler != null)
                {
                    ReceiveHandler(this, packet);
                }
            }
        }
        catch (Exception exception)
        {
            _active = false;
            if (ErrorHandler != null)
            {
                ErrorHandler(this, SessionErrorCode.DeserializeError, exception.ToString());
                return false;
            }
            throw;
        }
        return true;
    }

    #endregion

    #region Session Close

    public void Close()
    {
        if (_socket == null)
        {
            return;
        }
        _active = false;
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
        }
        finally
        {
            _socket.Close();
            _socket = null;
            _receiver = null;
            if (ClosedHandler != null)
            {
                ClosedHandler(this);
            }
        }
    }

    #endregion

    #region Session Dispose

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            Close();
        }
        _disposed = true;
    }

    #endregion

}
