using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cctm.Common
{
    public class DelayedDoer
    {
        private int nowTime = 0;

        private struct ToDoRecord
        {
            public Action toDo;
            public int doTime;
        }
        Queue<ToDoRecord> listToDo = new Queue<ToDoRecord>();

        // A different doer for each possible delay in seconds
        private static Dictionary<int, DelayedDoer> doers = new Dictionary<int, DelayedDoer>();

        public DelayedDoer(int delayTime)
        {
            Thread doerThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    lock (listToDo)
                    {
                        while ((listToDo.Count > 0) && (listToDo.Peek().doTime <= nowTime))
                        {
                            listToDo.Dequeue().toDo();
                        }
                    }
                    nowTime++;
                }
            }));
            doerThread.IsBackground = true;
            doerThread.Start();
        }

        /// <summary>
        /// Perform the specified action after approximately the given number of seconds (+- 1 second)
        /// </summary>
        /// <param name="whatToDo"></param>
        /// <param name="delaySeconds"></param>
        public static void DoLater(int delaySeconds, Action whatToDo)
        {
            DelayedDoer doer;

            lock (doers)
            {
                if (doers.ContainsKey(delaySeconds))
                {
                    doer = doers[delaySeconds];
                }
                else
                {
                    doer = new DelayedDoer(delaySeconds);
                    doers.Add(delaySeconds, doer);
                }
            }


            lock (doer.listToDo)
            {
                doer.listToDo.Enqueue(new ToDoRecord() { doTime = doer.nowTime + delaySeconds, toDo = whatToDo });
            }
        }
    }
}
