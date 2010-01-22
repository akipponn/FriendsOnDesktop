using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Xna_WindowForm
{
    class ThreadTest
    {
        public void Run()
        {
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine( "the other thread: " + i);
            }
        }
    }
}
