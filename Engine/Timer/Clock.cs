using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

internal class Clock
{

    private Stopwatch _watch;

    private Thread _thread;

    public Clock() {
        _watch = new Stopwatch();

        _thread = new Thread(() =>
        {
            _watch.Start();
        });

        _thread.Start();
    }

    public long Time => _watch.ElapsedMilliseconds;
}