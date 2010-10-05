using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Threading;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// A background worker with a work queue.
    /// </summary>
    [Description("A background worker with a work queue.")]
    [DefaultEvent("DoWork")]
    public class QueuedBackgroundWorker : Component
    {
        #region Member Variables
        private readonly object lockObject;

        private int priorityQueues;
        private Thread thread;
        private bool stopping;
        private bool started;
        private SynchronizationContext context;
        private bool disposed;

        private Queue<object>[] items;
        private Dictionary<object, bool> cancelledItems;

        private readonly SendOrPostCallback workCompletedCallback;
        private readonly SendOrPostCallback queueEmptyCallback;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueuedBackgroundWorker"/> class.
        /// </summary>
        public QueuedBackgroundWorker()
        {
            lockObject = new object();
            stopping = false;
            started = false;
            context = null;
            disposed = false;

            thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;

            // Work items
            priorityQueues = 5;
            items = new Queue<object>[priorityQueues];
            for (int i = 0; i < priorityQueues; i++)
                items[i] = new Queue<object>();
            cancelledItems = new Dictionary<object, bool>();

            // The loader complete callback
            workCompletedCallback = new SendOrPostCallback(this.RunWorkerCompletedCallback);
            queueEmptyCallback = new SendOrPostCallback(this.QueueEmptyCallback);
        }
        #endregion

        #region RunWorkerAsync
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        /// <param name="argument">The argument of an asynchronous operation.</param>
        /// <param name="priority">A value between 0 and <see cref="PriorityQueues"/> indicating the priority of this item.
        /// An item with a higher priority will be processed before items with lower priority.</param>
        public void RunWorkerAsync(object argument, int priority)
        {
            if (priority < 0 || priority >= priorityQueues)
                throw new ArgumentException("priority must be between 0 and " + (priorityQueues - 1).ToString() + "  inclusive.", "priority");

            // Start the worker thread
            if (!started)
            {
                // Get the current synchronization context
                context = SynchronizationContext.Current;

                // Start the thread
                thread.Start();
                while (!thread.IsAlive) ;

                started = true;
            }

            lock (lockObject)
            {
                items[priority].Enqueue(argument);
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        /// <param name="argument">The argument of an asynchronous operation.</param>
        public void RunWorkerAsync(object argument)
        {
            RunWorkerAsync(argument, 0);
        }
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        public void RunWorkerAsync()
        {
            RunWorkerAsync(null, 0);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the number of priority queues. Number of queues
        /// cannot be changed after any work is added to the work queue.
        /// </summary>
        [Browsable(true), Category("Behaviour"), DefaultValue(5)]
        public int PriorityQueues
        {
            get { return priorityQueues; }
            set
            {
                if (started)
                    throw new System.Threading.ThreadStateException("The thread has already been started.");

                priorityQueues = value;
                items = new Queue<object>[priorityQueues];
                for (int i = 0; i < priorityQueues; i++)
                    items[i] = new Queue<object>();
            }
        }
        /// <summary>
        /// Determines whether the <see cref="QueuedBackgroundWorker"/> started working.
        /// </summary>
        public bool Started { get { return started; } }
        /// <summary>
        /// Determines whether the <see cref="QueuedBackgroundWorker"/> being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        #endregion

        #region Cancel
        /// <summary>
        /// Cancels all pending operations.
        /// </summary>
        public void CancelAllAsync()
        {
            lock (lockObject)
            {
                foreach (Queue<object> queue in items)
                    queue.Clear();

                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Cancels processing the item with the given key.
        /// </summary>
        /// <param name="argument">The argument of an asynchronous operation.</param>
        public void CancelAsync(object argument)
        {
            lock (lockObject)
            {
                if (!cancelledItems.ContainsKey(argument))
                {
                    cancelledItems.Add(argument, false);
                    Monitor.Pulse(lockObject);
                }
            }
        }
        #endregion

        #region Delegate Callbacks
        /// <summary>
        /// Used to call <see cref="OnRunWorkerCompleted"/> by the synchronization context.
        /// </summary>
        /// <param name="arg">The argument.</param>
        private void RunWorkerCompletedCallback(object arg)
        {
            OnRunWorkerCompleted((QueuedWorkerCompletedEventArgs)arg);
        }
        /// <summary>
        /// Used to call <see cref="OnWorkerFinished"/> by the synchronization context.
        /// </summary>
        /// <param name="arg">The argument.</param>
        private void QueueEmptyCallback(object arg)
        {
            OnWorkerFinished((EventArgs)arg);
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Raises the RunWorkerCompleted event.
        /// </summary>
        /// <param name="e">A <see cref="QueuedWorkerCompletedEventArgs"/> that contains event data.</param>
        protected virtual void OnRunWorkerCompleted(QueuedWorkerCompletedEventArgs e)
        {
            if (RunWorkerCompleted != null)
                RunWorkerCompleted(this, e);
        }
        /// <summary>
        /// Raises the DoWork event.
        /// </summary>
        /// <param name="e">A <see cref="QueuedWorkerDoWorkEventArgs"/> that contains event data.</param>
        protected virtual void OnDoWork(QueuedWorkerDoWorkEventArgs e)
        {
            if (DoWork != null)
                DoWork(this, e);
        }
        /// <summary>
        /// Raises the WorkerFinished event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs"/> that contains event data.</param>
        protected virtual void OnWorkerFinished(EventArgs e)
        {
            if (WorkerFinished != null)
                WorkerFinished(this, e);
        }
        #endregion

        #region Get/Set Apartment State
        /// <summary>
        /// Gets the apartment state of the worker thread.
        /// </summary>
        public ApartmentState GetApartmentState()
        {
            return thread.GetApartmentState();
        }
        /// <summary>
        /// Sets the apartment state of the worker thread. The apartment state
        /// cannot be changed after any work is added to the work queue.
        /// </summary>
        public void SetApartmentState(ApartmentState state)
        {
            thread.SetApartmentState(state);
        }
        /// <summary>
        /// Gets or sets a value indicating whether or not the worker thread is a background thread.
        /// </summary>
        [Browsable(true), Description("Gets or sets a value indicating whether or not the worker thread is a background thread."), Category("Behavior")]
        public bool IsBackground
        {
            get
            {
                return thread.IsBackground;
            }
            set
            {
                thread.IsBackground = value;
            }
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when the background operation of an item has completed,
        /// has been canceled, or has raised an exception.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when the background operation of an item has completed.")]
        public event RunQueuedWorkerCompletedEventHandler RunWorkerCompleted;
        /// <summary>
        /// Occurs when <see cref="RunWorkerAsync(object, int)" /> is called.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when RunWorkerAsync is called.")]
        public event QueuedWorkerDoWorkEventHandler DoWork;
        /// <summary>
        /// Occurs after all items in the queue is processed.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs after all items in the queue is processed.")]
        public event QueuedWorkerFinishedEventHandler WorkerFinished;
        #endregion

        #region Worker Method
        /// <summary>
        /// Used by the worker thread to process items.
        /// </summary>
        private void Run()
        {
            while (!Stopping)
            {
                lock (lockObject)
                {
                    // Wait until we have pending work items
                    bool hasItems = false;
                    foreach (Queue<object> queue in items)
                    {
                        if (queue.Count > 0)
                        {
                            hasItems = true;
                            break;
                        }
                    }

                    if (!hasItems)
                        Monitor.Wait(lockObject);
                }

                // Loop until we exhaust the queue
                bool queueFull = true;
                while (queueFull && !Stopping)
                {
                    // Get an item from the queue
                    object request = null;
                    int priority = 0;
                    lock (lockObject)
                    {
                        // Check queues
                        for (int i = priorityQueues - 1; i >= 0; i--)
                        {
                            if (items[i].Count > 0)
                            {
                                priority = i;
                                request = items[i].Dequeue();
                                break;
                            }
                        }

                        // Check if the item was removed
                        if (request != null && cancelledItems.ContainsKey(request))
                            request = null;
                    }

                    if (request != null)
                    {
                        object result = null;
                        Exception error = null;
                        bool cancelled = false;
                        // Start the work
                        try
                        {
                            // Raise the do work event
                            QueuedWorkerDoWorkEventArgs arg = new QueuedWorkerDoWorkEventArgs(request, priority);
                            OnDoWork(arg);
                            cancelled = arg.Cancel;
                            if (!cancelled)
                                result = arg.Result;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        // Raise the work complete event
                        QueuedWorkerCompletedEventArgs arg2 = new QueuedWorkerCompletedEventArgs(request,
                            result, priority, error, cancelled);
                        context.Post(workCompletedCallback, arg2);
                    }

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        queueFull = false;
                        foreach (Queue<object> queue in items)
                        {
                            if (queue.Count > 0)
                            {
                                queueFull = true;
                                break;
                            }
                        }
                    }
                }
                // Done processing queue
                context.Post(queueEmptyCallback, new EventArgs());
            }
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; 
        /// false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposed)
                return;

            lock (lockObject)
            {
                if (!stopping)
                {
                    stopping = true;
                    Monitor.Pulse(lockObject);
                }
            }

            disposed = true;
        }
        #endregion
    }
}
