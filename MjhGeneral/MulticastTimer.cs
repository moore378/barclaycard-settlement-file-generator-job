using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MjhGeneral
{
    /// <summary>
    /// A timer that notifies multiple destinations of expiry using weak references
    /// </summary>
    public class MulticastTimer_old
    {
        private Timer timer;
        private object toRemoveLock = new object();
        private object toRemove = null;
        private List<WeakReference> timerSubscribers = new List<WeakReference>();
        

        public MulticastTimer_old(int period)
        {
            this.timer = new Timer(new TimerCallback(timer_Elapsed), null, 0, period);
        }

        /// <summary>
        /// Subscribe a target to receive elapsed notifications
        /// </summary>
        /// <param name="target">Target to receive notifications</param>
        /// <returns>An object representing subscription</returns>
        public Subscription Subscribe(ITarget target)
        {
            return new Subscription(target, this);
        }

        private void timer_Elapsed(object state)
        {
            try
            {
                if (!Monitor.TryEnter(timerSubscribers))
                    return;
                try
                {
                    foreach (var subscriberRef in timerSubscribers)
                    {
                        object target = subscriberRef.Target;

                        if (target != null)
                        {
                            Subscription sub = (Subscription)target;
                            Task.Factory.StartNew(sub.TimerElapsed);
                        }
                        else
                            RemoveSubscription(subscriberRef);
                    }


                    lock (toRemoveLock)
                    {
                        if (toRemove != null)
                        {
                            if (toRemove is WeakReference)
                                timerSubscribers.Remove((WeakReference)toRemove);
                            else if (toRemove is List<WeakReference>)
                                foreach (var item in (List<WeakReference>)toRemove)
                                    timerSubscribers.Remove(item);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(timerSubscribers);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unhandled exception in timer: " + Environment.NewLine + e.ToString());
                Environment.FailFast("Unhandled exception in timer", e);
            }
        }


        private void RemoveSubscription(WeakReference subscriberRef)
        {
            lock (toRemoveLock)
            {
                if (toRemove == null) // Most common case
                    toRemove = subscriberRef;
                else if (toRemove is List<WeakReference>)
                    ((List<WeakReference>)toRemove).Add(subscriberRef);
                else // Upgrade to list
                {
                    WeakReference first = (WeakReference)toRemove;
                    var newList = new List<WeakReference>();
                    newList.Add(first);
                    toRemove = newList;
                }
            }
        }

        public interface ITarget
        {
            void TimerElapsed();
        }

        public class Subscription : IDisposable
        {
            private ITarget inner;
            private MulticastTimer_old host;
            private WeakReference subscriptionRef;
            private object reentryLock = new object();

            public Subscription(ITarget inner, MulticastTimer_old host)
            {
                this.inner = inner;
                this.host = host;
                this.subscriptionRef = host.AddSubscription(this);
            }

            public void TimerElapsed()
            {
                // Prevent multiple entries
                if (!Monitor.TryEnter(reentryLock))
                    return;
                try
                {
                    inner.TimerElapsed();
                }
                finally
                {
                    Monitor.Exit(reentryLock);
                }
            }

            public void Dispose()
            {
                Unsubscribe();
                GC.SuppressFinalize(this);
            }

            ~Subscription()
            {
                Unsubscribe();
            }

            public void Unsubscribe()
            {
                host.RemoveSubscription(subscriptionRef);
            }
        }

        private WeakReference AddSubscription(Subscription subscription)
        {
            lock (timerSubscribers)
            {
                var newRef = new WeakReference(subscription);
                timerSubscribers.Add(newRef);
                return newRef;
            }
        }
    }

    public class MulticastTimer
    {
        
        private Timer timer;
        private LinkedList<WeakReference> subscriptions = new LinkedList<WeakReference>();
        private ReaderWriterLockSlim subscriptionsLock = new ReaderWriterLockSlim();

        public MulticastTimer(int period)
        {
            this.timer = new Timer(new TimerCallback(timer_Elapsed), null, 0, period);
        }

        public Subscription Subscribe(ITarget target)
        {
            return new Subscription(AddSubscription, target);
        }

        private WeakReference AddSubscription(Subscription subscription)
        {
            subscriptionsLock.EnterWriteLock();
            try
            {
                WeakReference reference = new WeakReference(subscription);
                subscriptions.AddLast(reference);
                Console.WriteLine("Timer subscriptions: " + subscriptions.Count);
                return reference;
            }
            finally
            {
                subscriptionsLock.ExitWriteLock();
            }
        }

        private void timer_Elapsed(object state)
        {
            subscriptionsLock.EnterUpgradeableReadLock();
            try
            {
                var current = subscriptions.First;
                while (current != null)
                {
                    var next = current.Next;
                    if (current.Value.IsAlive)
                    {
                        // Note: this could be done more efficiently
                        Task.Factory.StartNew(CallTimer, current.Value);
                    }
                    else
                    {
                        subscriptionsLock.EnterWriteLock();
                        try
                        {
                            subscriptions.Remove(current);
                        }
                        finally
                        {
                            subscriptionsLock.ExitWriteLock();
                        }
                        Console.WriteLine("Timer subscriptions: " + subscriptions.Count);
                    }
                    current = next;
                }
            }
            finally
            {
                subscriptionsLock.ExitUpgradeableReadLock();
            }
        }

        private static void CallTimer(object subscriptionRef)
        {
                object target = ((WeakReference)subscriptionRef).Target;
                if (target != null)
                {
                    var subscription = (Subscription)target;
                    //Stopwatch stopwatch = Stopwatch.StartNew();
                    subscription.TimerElapsed();
//                    if (stopwatch.ElapsedMilliseconds > 5000)
  //                      Debugger.Break();
                }
        }

        public class Subscription : IDisposable
        {
            private WeakReference subscriptionReference;
            private ITarget target;
            private int fired = 0;

            internal Subscription(Func<Subscription, WeakReference> subscriptionReference, ITarget target)
            {
                if (target == null)
                    throw new ArgumentNullException("target");
                this.target = target;
                this.subscriptionReference = subscriptionReference(this);
            }

            internal void TimerElapsed()
            {
                if (Interlocked.CompareExchange(ref fired, 0, 1) == 0)
                    try
                    {
                        target.TimerElapsed();
                    }
                    finally
                    {
                        Interlocked.Exchange(ref fired, 0);
                    }
            }

            public void Dispose()
            {
                subscriptionReference.Target = null;
            }
        }

        public interface ITarget
        {
            void TimerElapsed();
        }
    }
}
