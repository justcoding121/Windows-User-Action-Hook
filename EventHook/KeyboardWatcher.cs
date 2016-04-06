﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EventHook.Hooks;
using EventHook.Helpers;
using System.Threading.Tasks;
using Nito.AsyncEx;


namespace EventHook
{
    public class KeyInputEventArgs : EventArgs
    {
        public KeyData KeyData { get; set; }
    }
    public class KeyData
    {
        public string UnicodeCharacter;
        public string Keyname;
        public KeyEvent EventType;
    }
    public enum KeyEvent
    {
        down = 0,
        up = 1
    }

    public class KeyboardWatcher
    {

        /*Keyboard*/
        private static bool _IsRunning { get; set; }
        private static KeyboardHook _kh;
        private static object _Accesslock = new object();
        private static AsyncCollection<object> _kQueue;

        public static event EventHandler<KeyInputEventArgs> OnKeyInput;

        public static void Start()
        {
            if (!_IsRunning)
                lock (_Accesslock)
                {
                    _kQueue = new AsyncCollection<object>();

                    _kh = new KeyboardHook();
                    _kh.KeyDown += new RawKeyEventHandler(KListener);
                    _kh.KeyUp += new RawKeyEventHandler(KListener);


                    Task.Factory.StartNew(() => { }).ContinueWith(x =>
                    {
                        _kh.Start();
               
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
                    if (_kh != null)
                    {
                        _kh.KeyDown -= new RawKeyEventHandler(KListener);
                        _kh.Stop();
                        _kh = null;
                    }

                    _kQueue.Add(false);
                    _IsRunning = false;
                }
        }

        private static void KListener(object sender, RawKeyEventArgs e)
        {
            _kQueue.Add(new KeyData() { UnicodeCharacter = e.Character, Keyname = e.Key.ToString(), EventType = (KeyEvent)e.EventType });
        }

        // This is the method to run when the timer is raised. 
        private static async Task ConsumeKeyAsync()
        {
            while (_IsRunning)
            {

                //blocking here until a key is added to the queue
                var item = await _kQueue.TakeAsync();
                if (item is bool) break;

                KListener_KeyDown((KeyData)item);
            }
        }

        private static void KListener_KeyDown(KeyData kd)
        {
            EventHandler<KeyInputEventArgs> handler = OnKeyInput;
            if (handler != null)
            {
                handler(null, new KeyInputEventArgs() { KeyData = kd });
            }

        }

    }
}
