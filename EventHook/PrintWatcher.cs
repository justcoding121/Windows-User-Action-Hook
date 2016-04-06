﻿using System;
using System.Collections;
using System.Printing;
using EventHook.Hooks;
using EventHook.Helpers;
using EventHook.Hooks.Library;
using System.Threading.Tasks;
using EventHook.Hooks.PrintQueue;

namespace EventHook
{
    public class PrintEventData
    {
        public DateTime EventDateTime { get; set; }
        public string PrinterName { get; set; }
        public string JobName { get; set; }
        public int? Pages { get; set; }
        public int? JobSize { get; set; }
    }

    public class PrintEventArgs : EventArgs
    {
        public PrintEventData EventData { get; set; }
    }

    public class PrintWatcher
    {
        /*Print history*/
        private static bool _IsRunning;
        private static object _Accesslock = new object();

        private static ArrayList _printers = null;
        private static PrintServer ps = null;

        public static event EventHandler<PrintEventArgs> OnPrintEvent;

        public static void Start()
        {
             if (!_IsRunning)
                 lock (_Accesslock)
                 {
                     _printers = new ArrayList();
                     ps = new PrintServer();
                     foreach (var pq in ps.GetPrintQueues())
                     {

                         var pqm = new PrintQueueHook(pq.Name);
                         pqm.OnJobStatusChange += pqm_OnJobStatusChange;
                         pqm.Start();
                         _printers.Add(pqm);
                     }
                     _IsRunning = true;
                 }

        }
        public static void Stop()
        {
             if (_IsRunning)
                 lock (_Accesslock)
                 {
                     if (_printers != null)
                     {
                         foreach (PrintQueueHook pqm in _printers)
                         {
                             pqm.OnJobStatusChange -= pqm_OnJobStatusChange;
                             pqm.Stop();
                         }
                         _printers.Clear();
                     }
                     _printers = null;
                     _IsRunning = false;
                 }
        }
        private static void pqm_OnJobStatusChange(object sender, PrintJobChangeEventArgs e)
        {

            if ((e.JobStatus & JOBSTATUS.JOB_STATUS_SPOOLING) == JOBSTATUS.JOB_STATUS_SPOOLING && e.JobInfo !=null)
            {

                var hWnd = WindowHelper.GetActiveWindowHandle();
                var appTitle = WindowHelper.GetWindowText(hWnd);
                var appName = WindowHelper.GetAppDescription(WindowHelper.GetAppPath(hWnd));

                var printEvent = new PrintEventData()
                 {

                     JobName = e.JobInfo.JobName,
                     JobSize = e.JobInfo.JobSize,
                     EventDateTime = DateTime.Now,
                     Pages = e.JobInfo.NumberOfPages,
                     PrinterName = ((PrintQueueHook)sender).SpoolerName

                 };

                EventHandler<PrintEventArgs> handler = OnPrintEvent;

                if (handler != null)
                {
                    handler(null, new PrintEventArgs() { EventData = printEvent });
                }
            }
        }
    }
}
