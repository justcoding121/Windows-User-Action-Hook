﻿using EventHook.Hooks;
using EventHook.Helpers;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHook
{
    public class MouseEventArgs : EventArgs
    {
        public MouseMessages Message { get; set; }
        public POINT Point { get; set; }
    }

    public class MouseWatcher
    {
        /*Keyboard*/
        private static bool _IsRunning { get; set; }
        private static object _Accesslock = new object();

        private static AsyncCollection<object> _kQueue;
        private static MouseHook _mh;

        public static event EventHandler<MouseEventArgs> OnMouseInput;

        public static void Start()
        {
            if (!_IsRunning)
                lock (_Accesslock)
                {
                    _kQueue = new AsyncCollection<object>();

                    _mh = new MouseHook();
                    _mh.MouseAction += MListener;


                    Task.Factory.StartNew(() => { }).ContinueWith(x =>
                    {
                        _mh.Start();

                    }, SharedMessagePump.GetTaskScheduler());

                    Task.Factory.StartNew(() => ConsumeKeyAsync());

                    _IsRunning = true;
                }

        }

        public static void Stop()
        {
            if (_IsRunning)
                lock (_Accesslock)
                {
                    if (_mh != null)
                    {
                        _mh.MouseAction -= MListener;
                        _mh.Stop();
                        _mh = null;
                    }
                    _kQueue.Add(false);
                    _IsRunning = false;
                }
        }


        private static void MListener(object sender, RawMouseEventArgs e)
        {
            _kQueue.Add(e);
        }

        // This is the method to run when the timer is raised. 
        private static async Task ConsumeKeyAsync()
        {
            while (_IsRunning)
            {

                //blocking here until a key is added to the queue
                var item = await _kQueue.TakeAsync();
                if (item is bool) break;

                KListener_KeyDown(item as RawMouseEventArgs);

            }
        }

        private static void KListener_KeyDown(RawMouseEventArgs kd)
        {
            EventHandler<MouseEventArgs> handler = OnMouseInput;
            if (handler != null)
            {
                handler(null, new MouseEventArgs() { Message = kd.Message, Point = kd.Point });
            }
        }
    }
}
