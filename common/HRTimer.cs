using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public delegate void HRHandler();

public enum HRTimerCritical
{
    Low = 1,
    Normal = 2,
    High = 3,
    Highest = 4
}

public class HRTimer
{
    [DllImport(@"kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long frq);
    [DllImport(@"kernel32.dll")]
    private static extern bool QueryPerformanceCounter(out long count);

    private long lCPUFrq, lTimerTrigger, lNowTime;
    private long lInterval = 0;
    private Thread thread = null;
    public UInt32 uiLoops { get; set; }
    public event HRHandler HRTEvent;
    bool bThreadRunning;
    HRTimerCritical Critical;
    TimeSpan ts;

    public HRTimer(UInt32 interval, HRTimerCritical critical = HRTimerCritical.Normal)
    {
        QueryPerformanceFrequency(out lCPUFrq);
        lInterval = interval * lCPUFrq / 1000;
        QueryPerformanceCounter(out lTimerTrigger);
        lTimerTrigger += lInterval;
        ts = TimeSpan.FromMilliseconds(lInterval);
        Critical = critical;
    }

    public void Start()
    {
        thread = new Thread(() =>
        {
            uiLoops = 0;
            //QueryPerformanceCounter(out lTimerTrigger);
            try
            {
                while (bThreadRunning)
                {
                    QueryPerformanceCounter(out lNowTime);
                    if ((lNowTime - lTimerTrigger) > -10)
                    {
                        uiLoops++;
                        if (HRTEvent != null) HRTEvent();
                        else break;
                        lTimerTrigger += lInterval;
                    }
                    Thread.Sleep(Critical > HRTimerCritical.Normal ? 0 : 1);
                }
            }
            catch (System.Exception ex)
            {
                ex.Message.ToString();
                bThreadRunning = false;
                Thread.ResetAbort();
            }
        });
        thread.Priority = Critical == HRTimerCritical.Low ? ThreadPriority.BelowNormal : (Critical == HRTimerCritical.Normal ? ThreadPriority.Normal : (Critical == HRTimerCritical.High ? ThreadPriority.AboveNormal : ThreadPriority.Highest));
        bThreadRunning = true;
        thread.Start();
    }
    public void Stop()
    {
        bThreadRunning = false;
        HRTEvent = null;
    }

    public static Double GetHRMilliseconds()
    {
        long now, frq;
        QueryPerformanceFrequency(out frq);
        QueryPerformanceCounter(out now);
        return now / (double)frq * 1000;
    }

    public double GetTrigger()
    {
        return lTimerTrigger / (double)lCPUFrq * 1000.0;
    }
}
