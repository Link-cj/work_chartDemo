using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static work_chartDemo.Modbus_RTU;

namespace work_chartDemo
{
    class TCPUlites
    {
        public event UpdateEventHandle Update;

        public enum ExitType  // 退出类型枚举
        {
            ClientExit,  // 用户退出

            NormalExit,  // 正常退出

            ExceptionExit  // 异常关闭
        };

        public struct TCPStruct  // TCPCliect信息结构体
        {
            public string IPAddress;  // IP地址
            public int serverPort;  // 端口号
        };

        private const int ClientDefaultBufferSize = 64 * 1024;  // 接收数据缓冲区大小64K

        private readonly byte[] ClientReciveDataBuffer = new byte[ClientDefaultBufferSize];  // 接收数据缓冲区

        public string data = null;  // 接收到的数据

        public bool isTcpClientConnect = false;  // 通信是否连接

        public string info = "1";  // 反馈信息

        private TcpClient _tcpClient = null;
        /// <summary>
        /// TCP 通信连接
        /// </summary>
        /// <param name="serverIpstr">IP地址</param>
        /// <param name="serverPort">端口号</param>
        public void Connect_tcpCliect(TCPStruct tCPStruct)
        {
            try
            {
                if (isTcpClientConnect == false)
                {
                    _tcpClient = new TcpClient();

                    _tcpClient.BeginConnect(tCPStruct.IPAddress, tCPStruct.serverPort, new AsyncCallback(Connected), _tcpClient.Client);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        /// <summary>
        /// TCP 连接回调函数
        /// </summary>
        /// <param name="ar"></param>
        public void Connected(IAsyncResult ar)
        {         
            // 获取参数
            Socket msocket = (Socket)ar.AsyncState;
            try
            {
                // 结束链接请求，必须调用EndConnect。重点。
                msocket.EndConnect(ar);
            }
            catch (SocketException ex)
            {
                isTcpClientConnect = false;
                if (ex.ErrorCode == 10061)
                    info = "远程服务器拒绝接入";
                return;
            }
            finally
            {
                // nothing to do...
            }
            // 链接完成更新界面
            info = ClientConnected(msocket);
            // 事件传参
            Update?.Invoke(this, new TransmitEventArgs { data = info});
            
            // 启动数据读取，重点 socket下的BeginReceive，异步
            _tcpClient.Client.BeginReceive(ClientReciveDataBuffer, 0, ClientDefaultBufferSize, SocketFlags.None,
              new AsyncCallback(RecivedData), msocket);
        }
        /// <summary>
        /// TCP 连接成功
        /// </summary>
        /// <param name="msocket"></param>
        /// <returns></returns>
        public string ClientConnected(Socket msocket)
        {
            // 更新链接状态
            isTcpClientConnect = true;

            // 格式化输出的信息字符串
            string info = string.Format("成功连接远程服务器：{0}",
                msocket.RemoteEndPoint.ToString());
            return info;
        }
        /// <summary>
        /// TCP连接接收回调函数
        /// </summary>
        /// <param name="ar"></param>
        public void RecivedData(IAsyncResult ar)
        {
            // 断开状态就返回
            if (isTcpClientConnect == false) return;
            // 获取Socket对象
            Socket remote = (Socket)ar.AsyncState;
            try
            {
                // 获取接收数据的长度，EndReceive
                int reciveLength = remote.EndReceive(ar);
                //服务端关闭，正常退出
                if (reciveLength == 0)
                {
                    // UI更新
                    ClientDisConnected(ExitType.NormalExit);
                    return;
                }
                //接收数据更新
                ReceivedDataHandler(ClientReciveDataBuffer, reciveLength, true);
                //继续接收，继续启动接收
                _tcpClient.Client.BeginReceive(ClientReciveDataBuffer, 0, ClientDefaultBufferSize, SocketFlags.None,
                new AsyncCallback(RecivedData), _tcpClient.Client);
            }
            catch (SocketException ex)
            {
                if (10054 == ex.ErrorCode)
                {
                    //服务器强制的关闭连接，强制退出
                    info = $"远程服务器关闭：{ex.ErrorCode}";
                }
                else
                {
                    info = $"{ex.Message}";
                }
            }
            catch (ObjectDisposedException ex)
            {
                // 不处理异常
                if (ex != null)
                {
                    ex = null;
                }
            }
            // 事件传参
            Update?.Invoke(this, new TransmitEventArgs { data = info });
        }

        /// <summary>
        /// TCP 数据接收
        /// </summary>
        /// <param name="recbuffer">接收的数据</param>
        /// <param name="len">接收数据长度</param>
        /// <param name="isHexReceive">是否以十六进制接收</param>
        /// <returns></returns>
        private void ReceivedDataHandler(byte[] recbuffer, int len, bool isHexReceive)
        {
            if (!isHexReceive)
            {
                data = Encoding.Default.GetString(recbuffer, 0, len);
            }
            else
            {
                byte[] tempData = new byte[len];
                Array.Copy(recbuffer, tempData, tempData.Length);
                data = Transform.ToHexString(tempData, " ");
            }
        }
        /// <summary>
        /// 更新连接信息
        /// </summary>
        /// <param name="TypeOfExit"></param>
        private void ClientDisConnected(ExitType TypeOfExit)
        {
            isTcpClientConnect = false;
            // 判断退出是用户主动退出
            if (TypeOfExit == ExitType.ClientExit)
            {
                info = "连接已断开！！";
            }
            // 判断退出是异常退出
            else if (TypeOfExit == ExitType.ExceptionExit)
            {
                info = "服务器异常关闭！！";
                _tcpClient.Close();
            }
            // 其他作为异常服务器退出
            else
            {
                info = "服务器已经关闭！！";
                _tcpClient.Close();

            }

            // 事件传参
            Update?.Invoke(this, new TransmitEventArgs { data = info });
        }
        /// <summary>
        /// TCP 数据发送
        /// </summary>
        /// <param name="data">需发送的数据</param>
        /// <param name="isHexSend">选择是否以十六进制发送</param>
        public void TCPClientSend(string data, bool isHexSend)
        {
            if (isTcpClientConnect == false) return;
            /// 
            if (!isHexSend)  // 是否为十六进制发送
            {
                if (data != "")
                {
                    _tcpClient.Client.Send(Encoding.Default.GetBytes(data));
                }
            }
            else
            {
                string hexstr = data.Replace(" ", "");              
                if (DataEncoding.IsHexString(data.Replace(" ", "")))
                {
                    _tcpClient.Client.Send(Transform.ToBytes(hexstr));
                }
                else
                {
                    Console.WriteLine("请输入正确的十六进制数据！！");
                }
           }
        }
        /// <summary>
        /// 断开TCP连接
        /// </summary>
        public void Disconnect_tcpCliect()
        {
            if (isTcpClientConnect == true && _tcpClient.Connected)
            {
                // 断开链接
                _tcpClient.Close();
                // 更新连接信息
                ClientDisConnected(ExitType.ClientExit);
            }
        }
    }
}
