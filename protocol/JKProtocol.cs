using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace work_chartDemo.protocol
{
    class JKProtocol
    {
        TCPUlites My_JKTCP = null;

        public event UpdateEventHandle InfoJK;  // 信息反馈
        public bool ConnectState { get; set; } = false;  // 连接状态
        public int RcvTimeOut { get; set; } = 2000;  // 超时时间
        public bool isBusy { get; set; } = false;  // 总线忙碌

        public string infomation = null;  // 连接信息反馈

        public void Connect_Communicate(TCPUlites.TCPStruct tcpStruct)
        {
            My_JKTCP = new TCPUlites();
            My_JKTCP.Connect_tcpCliect(tcpStruct);

            ConnectState = My_JKTCP.isTcpClientConnect;
            My_JKTCP.Update += new UpdateEventHandle(update);
        }
        private void update(object sender, TransmitEventArgs e)
        {
            infomation = e.data;
        }
        public bool UpdataConnectInfo()
        {
            ConnectState = My_JKTCP.isTcpClientConnect;
            return ConnectState;
        }
        /// <summary>
        /// 集控板复位
        /// </summary>
        /// <returns></returns>
        public bool SystemReset()
        {
            // 报文拼接            
            List<byte> sendCommand = new List<byte>
            {
                0xFE,  // HEAD               
                0x80,  // CMD                
                0x01,  // LEN                
                0x00   // Data
            };          
            sendCommand.Add(DataCheck.DataSum_8(sendCommand));  // CS校验：           
            sendCommand.Add(0xA6);  // END
            // 发送并接受
            byte[] response = null;
            if (SendAndReceive(6, sendCommand.ToArray(), ref response)) 
            {
                //验证报文
                if (response.Length == 6)
                {
                    if ((response[0] == 0xFE) && (response[1] == 0xFF) && (response[2] == 0x01) 
                        && (response[3] == 0x00) && (response[4] == 0xFE) && (response[5] == 0xA6))
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
        /// 获取集控板状态
        /// </summary>
        /// <returns>00:泄能断开、01:泄能吸合；00:触发停止、01:触发</returns>
        public byte[] GetState()
        {
            byte[] tempByte = null;
            // 报文拼接            
            List<byte> sendCommand = new List<byte>
            {
                0xFE,  // HEAD               
                0x01,  // CMD                
                0x01,  // LEN                
                0x1F   // Data
            };
            sendCommand.Add(DataCheck.DataSum_8(sendCommand));  // CS校验：           
            sendCommand.Add(0xA6);  // END
            // 发送并接受
            byte[] response = null;
            if (SendAndReceive(7, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 7)
                {
                    List<byte> temp = new List<byte>()
                    {
                        response[0],response[1],response[2],response[3],response[4]
                    };
                    byte check = DataCheck.DataSum_8(temp);
                    if ((response[0] == 0xFE) && (response[1] == 0x01) && (response[2] == 0x02) &&
                         (response[5] == check) && (response[6] == 0xA6))
                    {
                        tempByte.Append(response[3]);
                        tempByte.Append(response[4]);
                        return tempByte;
                    }
                }
            }
            else
            {
                return tempByte;
            }
            return tempByte;
        }
        /// <summary>
        /// 集控板触发
        /// </summary>
        /// <param name="data">01:触发、00:关闭</param>
        /// <returns>01:触发失败、03:泄能失败、00:操作成功</returns>
        public byte Trigger(byte data)
        {
            byte tempByte = 0xFF;
            // 报文拼接            
            List<byte> sendCommand = new List<byte>
            {
                0xFE,  // HEAD               
                0x84,  // CMD                
                0x01,  // LEN                
            };
            sendCommand.Add(data);  // DATA校验
            sendCommand.Add(DataCheck.DataSum_8(sendCommand));  // CS校验           
            sendCommand.Add(0xA6);  // END
            // 发送并接受
            byte[] response = null;
            if (SendAndReceive(6, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 6)
                {
                    List<byte> temp = new List<byte>()
                    {
                        response[0],response[1],response[2],response[3]
                    };
                    byte check = DataCheck.DataSum_8(temp);
                    if ((response[0] == 0xFE) && (response[1] == 0xFF) && (response[2] == 0x01) &&
                         (response[4] == check) && (response[5] == 0xA6))
                    {
                        tempByte = response[3];
                        return tempByte;
                    }
                }
            }
            else
            {
                return tempByte;
            }
            return tempByte;
        }
        /// <summary>
        /// 集控板泄能
        /// </summary>
        /// <param name="data">01:吸合、00:断开</param>
        /// <returns>01:触发失败、03:泄能失败、00:操作成功</returns>
        public byte Energesis(byte data)
        {
            byte tempByte = 0xFF;
            // 报文拼接            
            List<byte> sendCommand = new List<byte>
            {
                0xFE,  // HEAD               
                0x86,  // CMD                
                0x01,  // LEN                
            };
            sendCommand.Add(data);  // DATA
            sendCommand.Add(DataCheck.DataSum_8(sendCommand));  // CS校验           
            sendCommand.Add(0xA6);  // END
            // 发送并接受
            byte[] response = null;
            if (SendAndReceive(6, sendCommand.ToArray(), ref response))
            {
                //验证报文
                if (response.Length == 6)
                {
                    List<byte> temp = new List<byte>()
                    {
                        response[0],response[1],response[2],response[3]
                    };
                    byte check = DataCheck.DataSum_8(temp);
                    if ((response[0] == 0xFE) && (response[1] == 0xFF) && (response[2] == 0x01) &&
                         (response[4] == check) && (response[5] == 0xA6))
                    {
                        tempByte = response[3];
                        return tempByte;
                    }
                }
            }
            else
            {
                return tempByte;
            }
            return tempByte;
        }
        /// <summary>
        /// 发送并接受数据
        /// </summary>
        /// <param name="dataReceiveNum">数据长度</param>
        /// <param name="send">数据数组</param>
        /// <param name="response">结果数组</param>
        /// <returns></returns>
        private bool SendAndReceive(int dataReceiveNum, byte[] send, ref byte[] response)
        {
            bool temp;
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
            temp = TCPSendAndReceive(dataReceiveNum, send, ref response);
            return temp;
        }
        /// <summary>
        /// TCP发送并接收报文
        /// </summary>
        /// <param name="send"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool TCPSendAndReceive(int dataNum, byte[] send, ref byte[] response)
        {
            try
            {
                My_JKTCP.TCPClientSend(Transform.ToHexString(send), true);
                isBusy = true;
                // 发送信息拼接
                string tempData = null;
                tempData = "集控发送:";
                tempData += DateTime.Now.AddHours(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + ":" + Transform.ToHexString(send, " ");
                tempData += "\r\n";
                Console.WriteLine(tempData);
                InfoJK?.Invoke(this, new TransmitEventArgs { data = tempData });  // 信息通过事件发送
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
                    if (My_JKTCP.data != null)
                    {
                        if (My_JKTCP.data.Count() > 0)
                        {
                            trmpStrList.AddRange(My_JKTCP.data.Split(new Char[] { ' ' }));
                            if (trmpStrList.Count() < dataNum)
                            {
                                My_JKTCP.data = null;
                            }
                            else
                            {
                                tempData = "集控接收:";
                                tempData += DateTime.Now.AddHours(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + ":" + My_JKTCP.data;
                                tempData += "\r\n";
                                Console.WriteLine(tempData);
                                InfoJK?.Invoke(this, new TransmitEventArgs { data = tempData });

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
                                My_JKTCP.data = null;
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
    }
}
