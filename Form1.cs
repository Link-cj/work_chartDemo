using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using work_chartDemo.protocol;
using work_chartDemo.ulites;

namespace work_chartDemo
{
    public partial class Form1 : Form
    {       
        // 充电机状态
        private bool chargeError = false;  // 充电机故障
        private bool chargeInterlock = false;  // 外部联锁
        private bool chargeValue = false;  // 达值
        private bool chargeCharging = false;  // 充电中
        private Int16 chargeVolte = 0;  // 充电电压
        //private Int16 chargeNum = 0;  // 充电次数
        // 信息框内容
        private List<string> info_system = new List<string>();  // 系统信息缓冲
        // 初始化
        private readonly ChargeProtocol charge = new ChargeProtocol();
        private TCPUlites.TCPStruct chargeTCPStruct;
        // 界面参数
        private int setMxVolt;
        public Form1()
        {
            InitializeComponent();
        }

        private void Thread_Charge()
        {
            charge.InfoPro += new UpdateEventHandle(DataSendReceive);  // 显示发送、接受信息
            charge.InfoRtu += new UpdateEventHandle(ChargeInfo);  // 显示故障信息
            // 加载IP及端口
            XmlNode tempNode = XMLUlites.GetNode("充电机");
            chargeTCPStruct.IPAddress = tempNode.Attributes["ip"].Value;
            chargeTCPStruct.serverPort = Convert.ToInt32(tempNode.Attributes["port"].Value);
            setMxVolt = Convert.ToInt32(tempNode.Attributes["maxVol"].Value);
            // 建立通信连接
            if (!charge.connectState)
            {
                charge.Connect_Conmunicate(chargeTCPStruct);
                //Console.WriteLine("charge.Connect_Conmunicate(tCPStruct)");
            }
            // 定时更新状态 300ms
            System.Timers.Timer updataStateTimer = new System.Timers.Timer
            {
                Interval = 300  // 定时时间
            };
            updataStateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updataStateTimer_TimedEvent);
            //updataStateTimer.Tick += new EventHandler(updataStateTimer_TimedEvent);
            updataStateTimer.AutoReset = true;
            updataStateTimer.Enabled = true;
            //updataStateTimer.Start();
        }
        /// <summary>
        /// 定时器，定时更新状态
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void updataStateTimer_TimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            charge.UpdataConnectInfo();  // 更新连接信息
            if (!charge.connectState)  // 如果未连接，则重新连接通信
            {
                charge.Connect_Conmunicate(chargeTCPStruct);
                //Console.WriteLine(tCPStruct.IPAddress + tCPStruct.serverPort);
            }
            else
            {
                byte[] tempState = charge.updataStata();
                if (tempState != null)
                {                   
                    bool[] tempBools = Transform.ToBools(tempState[1]);  // 充电机系统状态                                       
                    chargeError = tempBools[0] || tempBools[1] || tempBools[2] || tempBools[3] || tempBools[4];  // 充电机故障信息 
                    chargeInterlock = tempBools[5];  // 外部联锁
                    chargeValue = tempBools[6];  // 达值
                    chargeCharging = tempBools[7];  // 充电中                                                   
                    DataCheck.ReverseBytes(tempState, 2, 2);  // 数据翻转
                    DataCheck.ReverseBytes(tempState, 4, 2);
                    chargeVolte = Transform.ToInt16(tempState, 2);  // 输出电压                  
                    //chargeNum = Transform.ToInt16(tempState, 4);  // 充电次数
                }
            }
        }
        /// <summary>
        /// 事件处理，数据发送接受信息追加至系统信息缓冲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataSendReceive(object sender, TransmitEventArgs e)
        {
            info_system.Add(e.data);
        }
        /// <summary>
        /// 事件处理，故障信息追加到系统信息缓冲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChargeInfo(object sender, TransmitEventArgs e)
        {
            string[] tempStr = e.data.Split(new char[1] { '：'});

            if (tempStr[tempStr.Length - 1].Contains("失败"))
            {
                info_system.Add(e.data);
                tsl_systemState1.Text = "充电机:" + tempStr[tempStr.Length - 1];
            }
            else
            {
                tsl_systemState1.Text = "运行正常";
            }
        }
        /// <summary>
        /// 设置充电电压
        /// </summary>
        /// <param name="chargeVolte">充点电压，kV</param>
        private void SetChargeVolte(double chargeVolte)
        {
            try
            {
                if (!charge.connectState)
                {
                    MessageBox.Show("通信未连接", "woring");
                    return;
                }
                else
                {
                    bool[] tempEnable = { true, false, false, false, false };
                    bool isEnable = charge.enableCharge(tempEnable);  // 充电机使能 充电机1
                    bool isSet = charge.setChargeVolt(Convert.ToInt32(chargeVolte * 1000));  // 充电电压设置
                    string tempInfo = (isEnable && isSet ? "设置成功" : "设置失败");
                    info_system.Add(tempInfo);
                    aGauge1.MaxValue = Convert.ToInt32((chargeVolte * 1000) + (chargeVolte * 1000 / 10) );  // 指针表最大值
                    aGauge1.ScaleLinesMajorStepValue = Convert.ToInt32(chargeVolte * 1000 / 10);  // 指针表每大格示数
                }
            }
            catch (Exception)
            {
                MessageBox.Show("通信未连接", "woring");
                return;
            }
        }
        /// <summary>
        /// 窗口加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 加载功能区选择
            //groupBox2.Enabled = false;
            //groupBox3.Enabled = true;            
            // 启动通信连接，并查询状态
            timer_info.Start();
            // 启动充电机线程新线程更新信息
            Thread thread = new Thread(new ThreadStart(Thread_Charge));
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true; //关闭窗体继续执行
            thread.Start();
            // 更新界面属性
            Thread.Sleep(1000);
            nud_setVolt1.Maximum = setMxVolt < 11 ? setMxVolt : 11;
            nud_setVolt2.Maximum = setMxVolt < 11 ? setMxVolt : 11;
        }
        /// <summary>
        /// 设备调试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsm_debug_Click(object sender, EventArgs e)
        {
            groupBox2.Enabled = true;
            groupBox3.Enabled = false;
        }
        /// <summary>
        /// 设备功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsm_function_Click(object sender, EventArgs e)
        {
            groupBox2.Enabled = false;
            groupBox3.Enabled = true;
        }
        /// <summary>
        /// 定时器，定时更新界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_info_Tick(object sender, EventArgs e)
        {
            charge.UpdataConnectInfo();
            tsl_charge.Text = tsl_systemState1.Text.Contains("充电机") ? "通信故障" : "通信正常";  // 更新通信状态
            ttb_errorInfo.Text = chargeError ? "充电机故障" : "无";  // 故障信息文本框显示
            ttb_chargeVolt.Text = (chargeVolte * 11000 / 4096).ToString();  // 充电电压文本框显示
            aGauge1.Value = chargeVolte * 11000 / 4096;  // 指针表显示
            // 通信数据显示
            if (info_system.Count > 0)
            {
                for (int i = 0; i < info_system.Count; i++)
                {
                    rtb_info.AppendText(info_system[i]);
                }
                info_system.Clear();
            }
            // 运行状态文本框状态显示
            if (chargeError || chargeInterlock || !tsl_systemState1.Text.Contains("正常"))
            {
                ptb_system.Image = Properties.Resources.红色;
                ptb_charge.Image = Properties.Resources.灰色;
                if (chargeError)
                {
                    ttb_system.Text = "充电机故障";
                    ttb_runState.Text = "充电机故障";                    
                    return;
                }
                else if (chargeInterlock)
                {
                    ttb_system.Text = "充电机外部联锁";
                    ttb_runState.Text = "充电机外部联锁";
                    return;
                }
                else if (!tsl_systemState1.Text.Contains("正常"))
                {
                    ttb_system.Text = "系统异常";
                    ttb_runState.Text = "系统异常";
                    return;
                }
            }
            else
            {
                ttb_system.Text = "正常";                
                ptb_system.Image = Properties.Resources.绿色;

                if (chargeValue)
                {
                    ttb_charge.Text = "充电机达值";
                    ttb_runState.Text = "充电机达值";
                    ptb_charge.Image = chargeCharging ? Properties.Resources.蓝色 : Properties.Resources.绿色;
                    return;
                }
                else if (chargeCharging)
                {
                    ttb_charge.Text = "充电机充电中";
                    ttb_runState.Text = "充电机充电中";
                    ptb_charge.Image = Properties.Resources.红闪;
                    return;
                }
                else
                {
                    ttb_charge.Text = "充电机待机";
                    ttb_runState.Text = "充电机待机";
                    ptb_charge.Image = Properties.Resources.灰色;
                }
            }
        }
        /// <summary>
        /// 界面关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer_info.Stop();
        }

        private void btn_setVolt2_Click(object sender, EventArgs e)
        {
            SetChargeVolte(Convert.ToDouble(nud_setVolt2.Value));
        }
        private void btn_setVolt1_Click(object sender, EventArgs e)
        {
            SetChargeVolte(Convert.ToDouble(nud_setVolt1.Value));
        }

        private void rtb_info_TextChanged(object sender, EventArgs e)
        {
            rtb_info.SelectionStart = rtb_info.TextLength;
            rtb_info.ScrollToCaret();
        }

        private void btn_charge1_Click(object sender, EventArgs e)
        {
            charge.OnCharge(1);
        }

        private void btn_stopCharge1_Click(object sender, EventArgs e)
        {
            charge.OnCharge(0);
        }

        private void tsm_about_Click(object sender, EventArgs e)
        {
            XmlNode tempNode = XMLUlites.GetNode("充电机");
            Console.WriteLine(tempNode.Attributes["ip"].Value);
            Console.WriteLine(tempNode.Attributes["port"].Value);
        }
    }

    public delegate void UpdateEventHandle(object sender, TransmitEventArgs e);  // 信息传递委托

    public class TransmitEventArgs : EventArgs  // 信息传递事件
    {
        public string data { get; set; }
    }
}
