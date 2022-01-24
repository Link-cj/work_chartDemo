using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace work_chartDemo
{
    class Modbus_RTU : TCPUlites
    {
        public string infomation = " ";

        public event UpdateEventHandle InfoModbusRtu;

        #region 连接方式枚举
        public enum ConnectMethods
        { 
            // 基于TCP的rtu
            rtuOnTCPCliect,
            // 基于串口的rtu
            rtuOnSerial
        };
        #endregion

        #region 服务信息结构体
        public struct SerialPortStruct  // 串口信息结构体
        {
            public string sp_name;  // 串口号
            public Int32 sp_baud;  // 波特率
            public Int32 sp_dataBits;  // 数据位
            public StopBits sp_stopBits;  // 停止位
            public Parity sp_parity;  // 校验位
        };
        #endregion

        #region Field
        public ConnectMethods connectMethods = ConnectMethods.rtuOnSerial;  // 通信连接方式
        private SerialPort MyCom;  // 创建串口对象
        private TCPUlites My_ModbusTcp;  // 创建TCPCliect对象
        #endregion

        #region Property
        //超时时间
        public int RcvTimeOut { get; set; } = 1000;
        //连接状态
        public bool ConnectState { get; set; } = false;
        public TCPStruct tcpStruct { get; set; }
        public SerialPortStruct spStruct { get; set; }
        public bool isBusy { get; set; } = false;  // 通信正忙
        #endregion

        #region Methods

        /// <summary>
        /// TCPCliect 连接
        /// </summary>
        /// <param name="tCPStruct">TCPCliect信息结构体</param>
        public void tcpConnect(TCPStruct tCPStruct)
        {
            My_ModbusTcp = new TCPUlites();
            My_ModbusTcp.Connect_tcpCliect(tCPStruct);

            ConnectState = My_ModbusTcp.isTcpClientConnect;
            My_ModbusTcp.Update += new UpdateEventHandle(update);
        }

        private void update(object sender, TransmitEventArgs e)
        {
            infomation = e.data;
        }

        public void tcpDisconnect()
        {
            My_ModbusTcp.Disconnect_tcpCliect();
            ConnectState = My_ModbusTcp.isTcpClientConnect;
        }
        /// <summary>
        /// 串口连接
        /// </summary>
        /// <param name="serialPortStruct">串口信息结构体</param>
        public void spConnect(SerialPortStruct serialPortStruct)
        {
            //实例化串口对象
            MyCom = new SerialPort();
            MyCom.PortName = serialPortStruct.sp_name;
            MyCom.BaudRate = serialPortStruct.sp_baud;
            MyCom.Parity = serialPortStruct.sp_parity;
            MyCom.DataBits = serialPortStruct.sp_dataBits;
            MyCom.StopBits = serialPortStruct.sp_stopBits;
            if (MyCom.IsOpen)
            {
                MyCom.Close();
            }
            MyCom.Open();
            if (MyCom.IsOpen)
            {
                ConnectState = true;
            }
        }

        /// <summary>
        /// 串口断开
        /// </summary>
        public void spDisConnect()
        {
            if (MyCom.IsOpen)
            {
                MyCom.Close();
                ConnectState = false;
            }
        }

        public void CommunicateConnect()
        {
            switch (connectMethods)
            {
                case ConnectMethods.rtuOnTCPCliect:
                    tcpConnect(tcpStruct);
                    break;

                case ConnectMethods.rtuOnSerial:
                    spConnect(spStruct);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 读取输出线圈01H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">读取线圈数量</param>
        /// <returns></returns>
        public byte[] ReadOutputStatus(byte iDevAddr, ushort iStartAddr, ushort iLength)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x01);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //线圈数量
            sendCommand.Add((byte)(iLength / 256));
            sendCommand.Add((byte)(iLength % 256));

            //CRC校验：
            //1、查表的方式；
            sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            /*byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);*/

            byte[] response = null;

            int byteLength = iLength % 8 == 0 ? iLength / 8 : iLength / 8 + 1;
            int receiveLength = 5 + byteLength;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 5 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x01 && response[2] == byteLength && CheckCRC(response))
                    {
                        return GetByteArray(response, 3, response.Length - 5);
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// 读取输入线圈02H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">读取线圈数量</param>
        /// <returns></returns>
        public byte[] ReadInputStatus(byte iDevAddr, ushort iStartAddr, ushort iLength)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x02);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //线圈数量
            sendCommand.Add((byte)(iLength / 256));
            sendCommand.Add((byte)(iLength % 256));

            //CRC校验：
            //1、查表的方式；
            sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            /*byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);*/

            byte[] response = null;

            int byteLength = iLength % 8 == 0 ? iLength / 8 : iLength / 8 + 1;
            int receiveLength = 5 + byteLength;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 5 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x02 && response[2] == byteLength && CheckCRC(response))
                    {
                        return GetByteArray(response, 3, response.Length - 5);
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// 读取保持性寄存器03H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">读取寄存器数量</param>
        /// <returns></returns>
        public byte[] ReadHoldRegister(byte iDevAddr, ushort iStartAddr, ushort iLength)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x03);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //寄存器数量
            sendCommand.Add((byte)(iLength / 256));
            sendCommand.Add((byte)(iLength % 256));

            //CRC校验
            //1、查表法CRC计算
            //sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);

            byte[] response = null;

            int byteLength = iLength * 2;
            int receiveLength = 5 + byteLength;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 5 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x03 && response[2] == byteLength && CheckCRC(response))
                    {
                        return GetByteArray(response, 3, response.Length - 5);
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// 读取输入寄存器04H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">读取寄存器数量</param>
        /// <returns></returns>
        public byte[] ReadInputRegister(byte iDevAddr, ushort iStartAddr, ushort iLength)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x04);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //寄存器数量
            sendCommand.Add((byte)(iLength / 256));
            sendCommand.Add((byte)(iLength % 256));

            //CRC校验
            //1、查表法CRC计算
            //sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);

            byte[] response = null;

            int byteLength = iLength * 2;
            int receiveLength = 5 + byteLength;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 5 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x04 && response[2] == byteLength && CheckCRC(response))
                    {
                        return GetByteArray(response, 3, response.Length - 5);
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// 写入单个输出线圈05H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">写入线圈数量</param>
        /// <param name="iValue">写入线圈值</param>
        /// <returns></returns>
        public bool WriteSingleOutputStatus(byte iDevAddr, ushort iStartAddr, ushort iLength, bool iValue)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x05);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //线圈值
            byte[] outputStatus = new byte[2];
            if (iValue == true)
            {
                sendCommand.Add(0xFF);
                sendCommand.Add(0x00);
                outputStatus[0] = 0xFF;
                outputStatus[1] = 0x00;
            }
            else
            {
                sendCommand.Add(0x00);
                sendCommand.Add(0x00);
                outputStatus[0] = 0x00;
                outputStatus[1] = 0x00;
            }
            //CRC校验：
            //1、查表的方式；
            sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            /*byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);*/

            byte[] response = null;
            int receiveLength = 8;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 8)
                {
                    if (response[0] == iDevAddr && response[1] == 0x05 && response[2] == ((byte)(iStartAddr / 256)) && response[3] == ((byte)(iStartAddr % 256)) && response[4] == outputStatus[0] && response[5] == outputStatus[1] && CheckCRC(response))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// 写入单个字保持性寄存器06H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">写入单个字寄存器数量</param>
        /// <returns></returns>
        public bool WriteSingleHoldRegister(byte iDevAddr, ushort iStartAddr, byte[] iWriteByte)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x06);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));

            //需要写入的数据
            sendCommand.Add(iWriteByte[0]);
            sendCommand.Add(iWriteByte[1]);

            //CRC校验
            //1、查表法CRC计算
            //sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);

            byte[] response = null;

            int byteLength = 2;
            int receiveLength = 6 + byteLength;
            if (SendAndReceive( receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 6 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x06 && response[2] == (iStartAddr / 256) && response[3] == (iStartAddr % 256) && CheckCRC(response))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// 写入多个输出线圈0FH
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">写入线圈数量</param>
        /// <param name="iValue">写入线圈值</param>
        /// <returns></returns>
        public bool WriteMultiOutputStatus(byte iDevAddr, ushort iStartAddr, ushort iLength, List<byte> iValue)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x0F);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));
            //线圈数量
            sendCommand.Add((byte)((iValue.Count * 8) / 256));
            sendCommand.Add((byte)((iValue.Count * 8) % 256));
            //字节数
            sendCommand.Add((byte)(iValue.Count));
            //线圈值
            for (int i = 0; i < iValue.Count; i++)
            {
                sendCommand.Add(iValue[i]);
            }
            //CRC校验：
            //1、查表的方式；
            sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            /*byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);*/

            byte[] response = null;
            int receiveLength = 8;
            if (SendAndReceive( receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 8)
                {
                    if (response[0] == iDevAddr && response[1] == 0x0F && response[2] == ((byte)(iStartAddr / 256)) && response[3] == ((byte)(iStartAddr % 256)) && response[4] == ((byte)iValue.Count * 8 / 256) && response[5] == ((byte)iValue.Count * 8 % 256) && CheckCRC(response))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// 写入多个字保持性寄存器10H
        /// </summary>
        /// <param name="iDevAddr">从站号</param>
        /// <param name="iStartAddr">起始地址</param>
        /// <param name="iLength">写入多个字寄存器数量</param>
        /// <returns></returns>
        public bool WriteMultiHoldRegister(byte iDevAddr, ushort iStartAddr, List<byte> iWriteByte)
        {
            //报文拼接
            //站号
            List<byte> sendCommand = new List<byte>();
            sendCommand.Add(iDevAddr);
            //功能码
            sendCommand.Add(0x10);
            //起始地址
            sendCommand.Add((byte)(iStartAddr / 256));
            sendCommand.Add((byte)(iStartAddr % 256));

            //写入数据的数量
            int writeCount = iWriteByte.Count / 2;
            sendCommand.Add((byte)(writeCount / 256));
            sendCommand.Add((byte)(writeCount % 256));

            //写入字节计数
            sendCommand.Add((byte)(iWriteByte.Count));

            //需要写入的数据
            for (int i = 0; i < writeCount; i++)
            {
                sendCommand.Add(iWriteByte[2 * i]);
                sendCommand.Add(iWriteByte[2 * i + 1]);
            }
            //CRC校验
            //1、查表法CRC计算
            //sendCommand.AddRange(Crc16(sendCommand.ToArray(), sendCommand.Count));

            //2、根据CRC算法进行编写
            byte hi, lo;//CRC校验的高位和低位
            CalculateCRC(sendCommand.ToArray(), sendCommand.Count, out hi, out lo);
            //低位在前，高位在后
            sendCommand.Add(lo);
            sendCommand.Add(hi);

            byte[] response = null;

            int byteLength = 2;
            int receiveLength = 6 + byteLength;
            if (SendAndReceive(receiveLength, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 6 + byteLength)
                {
                    if (response[0] == iDevAddr && response[1] == 0x10 && response[2] == (iStartAddr / 256) && response[3] == (iStartAddr % 256) && response[4] == (writeCount / 256) && response[5] == (writeCount % 256) && CheckCRC(response))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// 截取数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private byte[] GetByteArray(byte[] source, int start, int count)
        {
            if (source == null || source?.Length <= 0) return null;
            if (start < 0 || count < 0) return null;
            if (source.Length < start + count) return null;
            byte[] result = new byte[count];
            Array.Copy(source, start, result, 0, count);
            return result;
        }


        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool CheckCRC(byte[] response)
        {
            byte[] crc = Crc16(response, response.Length - 2);
            if (crc[0] == response[response.Length - 2] && crc[1] == response[response.Length - 1])
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 发送并接收报文
        /// </summary>
        /// <param name="send"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool SendAndReceive(int dataReceiveNum, byte[] send, ref byte[] response)
        {
            bool temp = false;

            DateTime start = DateTime.Now;
            while (isBusy)
            {
                Thread.Sleep(50);

                if ((DateTime.Now - start).TotalMilliseconds > this.RcvTimeOut)
                {
                    Console.WriteLine("回复超时1");
                    isBusy = false;
                    return false;
                }
            }

            switch (connectMethods)
            {
                case ConnectMethods.rtuOnTCPCliect:
                    temp = TCPSendAndReceive(dataReceiveNum, send, ref response);
                    break;

                case ConnectMethods.rtuOnSerial:
                    temp = SPSendAndReceive(send, ref response);
                    break;

                default:
                    break;
            }
            return temp;
        }
        /// <summary>
        /// 串口发送并接收报文
        /// </summary>
        /// <param name="send"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool SPSendAndReceive(byte[] send, ref byte[] response)
        {
            try
            {
                MyCom.Write(send, 0, send.Length);
                byte[] buffer = new byte[1024];

                MemoryStream ms = new MemoryStream();

                DateTime start = DateTime.Now;

                //串口的接收事件：
                while (true)
                {
                    Thread.Sleep(20);
                    if (MyCom.BytesToRead > 0)
                    {
                        int count = MyCom.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, count);
                    }
                    else
                    {
                        if ((DateTime.Now - start).TotalMilliseconds > this.RcvTimeOut)
                        {
                            ms.Dispose();
                            return false;
                        }
                        else if (ms.Length > 0)
                        {
                            break;
                        }
                    }
                }
                response = ms.ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// TCP发送并接收报文
        /// </summary>
        /// <param name="send"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool TCPSendAndReceive(int dataNum ,byte[] send, ref byte[] response)
        {
            try
            {                
                My_ModbusTcp.TCPClientSend(Transform.ToHexString(send), true);
                isBusy = true;
                // 发送信息拼接
                string tempData = null;
                tempData = "充电机发送:";
                tempData += DateTime.Now.AddHours(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + ":" + Transform.ToHexString(send, " ");
                tempData += "\r\n";
                Console.WriteLine(tempData);
                InfoModbusRtu?.Invoke(this, new TransmitEventArgs { data = tempData });  // 信息通过事件发送
                // 等待100ms后处理接受信息
                Thread.Sleep(100);
                byte[] buffer = new byte[1024];  // 接受信息缓存
                DateTime start = DateTime.Now;  // 开始计时
                MemoryStream ms = new MemoryStream();
                
                //string[] tempStr;  // = My_ModbusTcp.data.Split(new Char[] { ' ' });
                List<string> trmpStrList = new List<string>();
                //接受信息处理：
                while (true)
                {
                    Thread.Sleep(5);
                    if (My_ModbusTcp.data != null)
                    {
                        if (My_ModbusTcp.data.Count() > 0)
                        {
                            trmpStrList.AddRange(My_ModbusTcp.data.Split(new Char[] { ' ' }));
                            //int count = MyTcp.ReceivedDataHandler();
                            if (trmpStrList.Count() < dataNum)
                            {
                                My_ModbusTcp.data = null;
                            }
                            else
                            {
                                tempData = "充电机接收:";
                                tempData += DateTime.Now.AddHours(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + ":" + My_ModbusTcp.data;
                                tempData += "\r\n";
                                Console.WriteLine(tempData);
                                InfoModbusRtu?.Invoke(this, new TransmitEventArgs { data = tempData });

                                if (trmpStrList.Count() == 0)
                                {
                                    isBusy = false;
                                    return false;
                                }
                                else
                                {
                                    for (int i = 0; i < trmpStrList.Count(); i++)
                                    {
                                        buffer[i] = Convert.ToByte(trmpStrList[i], 16);
                                    }
                                }

                                ms.Write(buffer, 0, trmpStrList.Count);
                                My_ModbusTcp.data = null;
                                isBusy = false;
                                break;
                            }
                        }
                        else
                        {
                            if ((DateTime.Now - start).TotalMilliseconds > this.RcvTimeOut)
                            {
                                ms.Dispose();
                                isBusy = false;
                                Console.WriteLine("回复超时");
                                return false;
                            }
                            else if (ms.Length > 0)
                            {
                                Console.WriteLine("回复超时1");
                                isBusy = false;
                                break;
                            }
                        }
                    }                    
                }
                response = ms.ToArray();
                isBusy = false;
                return true;            
            }
            catch (Exception ex)
            {
                Console.WriteLine("报错:" + ex);
                isBusy = false;
                return false;
            }           
        }

        #region  CRC校验
        #region 查表法
        /// <summary>
        /// 高位表
        /// </summary>
        private static readonly byte[] aucCRCCHi =
        {
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x01,0xc0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x01,0xc0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
            0x00,0xc1,0x81,0x40
        };
        /// <summary>
        /// 低位表
        /// </summary>
        private static readonly byte[] aucCRCCLo =
        {
            0x00,0xc0,0xc1,0x01,0xc3,0x03,0x02,0xc2,0xc6,0x06,0x07,0xc7,
            0x05,0xc5,0xc4,0x04,0xcc,0x0c,0x0d,0xcd,0x0f,0xcf,0xce,0x0e,
            0x0a,0xca,0xcb,0x0b,0xc9,0x09,0x08,0xc8,0xd8,0x18,0x19,0xd9,
            0x1b,0xdb,0xda,0x1a,0x1e,0xde,0xdf,0x1f,0xdd,0x1d,0x1c,0xdc,
            0x14,0xd4,0xd5,0x15,0xd7,0x17,0x16,0xd6,0xd2,0x12,0x13,0xd3,
            0x11,0xd1,0xd0,0x10,0xf0,0x30,0x31,0xf1,0x33,0xf3,0xf2,0x32,
            0x36,0xf6,0xf7,0x37,0xf5,0x35,0x34,0xf4,0x3c,0xfc,0xfd,0x3d,
            0xff,0x3f,0x3e,0xfe,0xfa,0x3a,0x3b,0xfb,0x39,0xf9,0xf8,0x38,
            0x28,0xe8,0xe9,0x29,0xeb,0x2b,0x2a,0xea,0xee,0x2e,0x2f,0xef,
            0x2d,0xed,0xec,0x2c,0xe4,0x24,0x25,0xe5,0x27,0xe7,0xe6,0x26,
            0x22,0xe2,0xe3,0x23,0xe1,0x21,0x20,0xe0,0xa0,0x60,0x61,0xa1,
            0x63,0xa3,0xa2,0x62,0x66,0xa6,0xa7,0x67,0xa5,0x65,0x64,0xa4,
            0x6c,0xac,0xad,0x6d,0xaf,0x6f,0x6e,0xae,0xaa,0x6a,0x6b,0xab,
            0x69,0xa9,0xa8,0x68,0x78,0xb8,0xb9,0x79,0xbb,0x7b,0x7a,0xba,
            0xbe,0x7e,0x7f,0xbf,0x7d,0xbd,0xbc,0x7c,0xb4,0x74,0x75,0xb5,
            0x77,0xb7,0xb6,0x76,0x72,0xb2,0xb3,0x73,0xb1,0x71,0x70,0xb0,
            0x50,0x90,0x91,0x51,0x93,0x53,0x52,0x92,0x96,0x56,0x57,0x97,
            0x55,0x95,0x94,0x54,0x9c,0x5c,0x5d,0x9d,0x5f,0x9f,0x9e,0x5e,
            0x5a,0x9a,0x9b,0x5b,0x99,0x59,0x58,0x98,0x88,0x48,0x49,0x89,
            0x4b,0x8b,0x8a,0x4a,0x4e,0x8e,0x8f,0x4f,0x8d,0x4d,0x4c,0x8c,
            0x44,0x84,0x85,0x45,0x87,0x47,0x46,0x86,0x82,0x42,0x43,0x83,
            0x41,0x81,0x80,0x40
        };

        /// <summary>
        /// 查表的方式CRC校验
        /// </summary>
        /// <param name="pucFrame"></param>
        /// <param name="usLen"></param>
        /// <returns></returns>
        private byte[] Crc16(byte[] pucFrame, int usLen)
        {
            int i = 0;
            byte[] res = new byte[2] { 0xff, 0xff };
            UInt16 iIndex = 0x0000;
            while (usLen-- > 0)
            {
                iIndex = (UInt16)(res[0] ^ pucFrame[i++]);
                res[0] = (byte)(res[1] ^ aucCRCCHi[iIndex]);
                res[1] = aucCRCCLo[iIndex];
            }
            return res;
        }
        #endregion

        #region 计算法
        /// <summary>
        /// 计算CheckSum
        /// </summary>
        /// <param name="pByte"></param>
        /// <param name="nNumberOfBytes"></param>
        /// <param name="pChecksum"></param>
        private void CalculatepCheckSum(byte[] pByte, int nNumberOfBytes, out ushort pCheckSum)
        {
            int nBit;
            ushort nShiftedBit;
            pCheckSum = 0xFFFF;
            for (int nByte = 0; nByte < nNumberOfBytes; nByte++)
            {
                pCheckSum ^= pByte[nByte];
                for (nBit = 0; nBit < 8; nBit++)
                {
                    if ((pCheckSum & 0x1) == 1)
                    {
                        nShiftedBit = 1;
                    }
                    else
                    {
                        nShiftedBit = 0;
                    }
                    pCheckSum >>= 1;
                    if (nShiftedBit != 0)
                    {
                        pCheckSum ^= 0xA001;
                    }
                }
            }
        }

        /// <summary>
        /// 算法的方式CRC校验
        /// </summary>
        /// <param name="pByte"></param>
        /// <param name="nNumberOfBytes"></param>
        /// <param name="hi"></param>
        /// <param name="lo"></param>
        private void CalculateCRC(byte[] pByte, int nNumberOfBytes, out byte hi, out byte lo)
        {
            ushort sum;
            CalculatepCheckSum(pByte, nNumberOfBytes, out sum);
            lo = (byte)(sum & 0xff);
            hi = (byte)((sum & 0xFF00) >> 8);

        }
        #endregion

        #endregion

        #endregion

    }
}
