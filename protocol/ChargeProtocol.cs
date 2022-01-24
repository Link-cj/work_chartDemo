using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace work_chartDemo.protocol
{    
    class ChargeProtocol
    {
        Modbus_RTU MyModbus_RTUByTCp;

        public event UpdateEventHandle InfoRtu;
        public event UpdateEventHandle InfoPro;

        public string ProtocolInfomation;

        public bool connectState;

        public void Connect_Conmunicate(TCPUlites.TCPStruct MyTCPStruct)
        {
            try
            {
                MyModbus_RTUByTCp = new Modbus_RTU
                {
                    connectMethods = Modbus_RTU.ConnectMethods.rtuOnTCPCliect,
                    tcpStruct = MyTCPStruct
                };
                MyModbus_RTUByTCp.InfoModbusRtu += new UpdateEventHandle(SendReceiveData);
                MyModbus_RTUByTCp.CommunicateConnect();

                connectState = MyModbus_RTUByTCp.ConnectState;
            }
            catch (Exception ex)
            {
                ProtocolInfomation = "连接失败:" + ex.ToString();

                InfoRtu?.Invoke(this, new TransmitEventArgs { data = ProtocolInfomation });
                connectState = MyModbus_RTUByTCp.ConnectState;
                return;
            }
        }

        private void SendReceiveData(object sender, TransmitEventArgs e)
        {
            InfoPro?.Invoke(this,new TransmitEventArgs { data = e.data});
        }

        public string UpdataConnectInfo()
        {
            string infomation_01 = MyModbus_RTUByTCp.infomation;

            connectState = infomation_01.Contains("成功");

            return infomation_01;
        }

        /// <summary>
        /// 设置电压
        /// </summary>
        /// <param name="chargeVolt"></param>
        /// <returns></returns>
        public bool setChargeVolt(int chargeVolt)
        {
            List<byte> dataList = new List<byte>();

            int tempData = chargeVolt * 5 / 11;

            dataList.Add((byte)(tempData / 256));//高位
            dataList.Add((byte)(tempData % 256));//低位

            bool result = Pro_WriteMultiHoldRegister(1, 6, dataList);

            return result;
        }

        /// <summary>
        /// 设置充电周期
        /// </summary>
        /// <param name="cycTime"></param>
        /// <returns></returns>
        public bool setCycTime(int cycTime)
        {
            List<byte> dataList = new List<byte>();

            int tempData = cycTime;

            dataList.Add((byte)(tempData / 256));//高位
            dataList.Add((byte)(tempData % 256));//低位

            bool result = Pro_WriteMultiHoldRegister(1, 7, dataList);

            return result;
        }

        /// <summary>
        /// 设置充电次数
        /// </summary>
        /// <param name="chargeNum"></param>
        /// <returns></returns>
        public bool setChargeNum(int chargeNum)
        {
            List<byte> dataList = new List<byte>();

            int tempData = chargeNum;

            dataList.Add((byte)(tempData / 256));//高位
            dataList.Add((byte)(tempData % 256));//低位

            bool result = Pro_WriteMultiHoldRegister(1, 8, dataList);

            return result;
        }

        /// <summary>
        /// 使能充电电源
        /// </summary>
        /// <param name="enCharge"></param>
        /// <returns></returns>
        public bool enableCharge(bool[] enCharge)
        {
            List<byte> dataList = new List<byte>();

            byte[] tempData = Transform.ToBytes(enCharge);

            if (tempData != null)
            {
                if (tempData.Length == 1)
                {
                    dataList.Add(0x00);
                    dataList.Add(tempData[0]);
                }
                else
                {
                    foreach (byte temp in tempData)
                    {
                        dataList.Add(temp);
                    }
                }
            }

            bool result = Pro_WriteMultiHoldRegister(1, 9, dataList);

            return result;
        }

        /// <summary>
        /// 启动、停止充电
        /// </summary>
        /// <param name="funcation">1:启动,0:停止</param>
        /// <returns></returns>
        public bool OnCharge(byte funcation)
        {
            List<byte> dataList = new List<byte>
            {
                0,
                funcation
            };
            bool result = Pro_WriteMultiHoldRegister(1, 10, dataList);
            return result;
        }

        /// <summary>
        /// 更新充电机状态
        /// </summary>
        /// <returns></returns>
        public byte[] updataStata()
        {
            byte[] tempData = Pro_ReadHoldRegister(1, 0, 3);
            return tempData;
        }

        /// <summary>
        /// 读取保持寄存器03H
        /// </summary>
        private byte[] Pro_ReadHoldRegister(byte iDevAddr, ushort iStartAddr, ushort iLength)
        {
            byte[] result = null;
            try
            {
                result = MyModbus_RTUByTCp.ReadHoldRegister(iDevAddr, iStartAddr, iLength);
                string tempInfo;
                if (result == null)
                {
                    tempInfo = "充电机：" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + "：读取失败 \r\n";
                    InfoRtu?.Invoke(this, new TransmitEventArgs { data = tempInfo });
                    Console.WriteLine("单个寄存器读取失败！");
                }
                else
                {
                    tempInfo = "充电机：" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + "：读取成功 \r\n";
                    InfoRtu?.Invoke(this, new TransmitEventArgs { data = tempInfo });
                }

                return result;
            }
            catch (Exception ex)
            {
                ProtocolInfomation = "读写失败:" + ex.ToString();

                InfoRtu?.Invoke(this, new TransmitEventArgs { data = ProtocolInfomation });
                Console.WriteLine(ProtocolInfomation);
                
                return result;
            }
        }

        /// <summary>
        /// 写入多个保持寄存器10H
        /// </summary>
        private bool Pro_WriteMultiHoldRegister(byte iDevAddr, ushort iStartAddr, List<byte> writeList)
        {
            try
            {
                bool result = MyModbus_RTUByTCp.WriteMultiHoldRegister(iDevAddr, iStartAddr, writeList);
                string tempInfo;
                if (result == false)
                {
                    tempInfo = "充电机：" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + "：写入失败 \r\n";
                    InfoRtu?.Invoke(this, new TransmitEventArgs { data = tempInfo });
                    Console.WriteLine("寄存器写入失败！");
                    result = false;                 
                }
                else
                {
                    tempInfo = "充电机：" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd h:mm:ss.fff") + "：写入成功 \r\n";
                    InfoRtu?.Invoke(this, new TransmitEventArgs { data = tempInfo });
                }

                return result;
            }
            catch (Exception ex)
            {
                InfoRtu?.Invoke(this, new TransmitEventArgs { data = "读写失败:" + ex.ToString() });
                return false;
            }
        }
    }
}
