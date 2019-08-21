using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Utils
{
    public class DebugTimer : IDisposable
    {
        private string name;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public DebugTimer(string name)
        {
            this.name = name;
            DateTime start = DateTime.Now;
            System.Console.WriteLine("timer start " + name + ":" + start.ToString());
            sw.Start();
        }

        public void Dispose()
        {
            sw.Stop();
            System.Console.WriteLine("timer end " + name + ":" + sw.Elapsed);
        }
    }

}
