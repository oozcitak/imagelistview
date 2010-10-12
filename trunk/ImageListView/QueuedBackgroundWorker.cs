using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Drawing;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// A background worker with a work queue.
    /// </summary>
    [Description("A background worker with a work queue.")]
    [ToolboxBitmap(typeof(QueuedBackgroundWorker))]
    [DefaultEvent("DoWork")]
    public class QueuedBackgroundWorker : Component
    {
        #region Member Variables
        private readonly object lockObject;

        private ProcessingMode processingMode;
        private int threadCount;
        private Thread[] threads;
        private bool stopping;
        private bool started;
        private bool disposed;

        private int priorityQueues;
        private LinkedList<AsyncOperation>[] items;
        private Dictionary<object, bool> cancelledItems;

        private readonly SendOrPostCallback workCompletedCallback;
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
            disposed = false;

            // Threads
            threadCount = 5;
            CreateThreads();

            // Work items
            processingMode = ProcessingMode.FIFO;
            priorityQueues = 5;
            BuildWorkQueue();
            cancelledItems = new Dictionary<object, bool>();

            // The loader complete callback
            workCompletedCallback = new SendOrPostCallback(this.RunWorkerCompletedCallback);
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

            // Start the worker threads
            if (!started)
            {
                // Start the thread
                for (int i = 0; i < threadCount; i++)
                {
                    threads[i].Start();
                    while (!threads[i].IsAlive) ;
                }

                started = true;
            }

            lock (lockObject)
            {
                AddWork(argument, priority);
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

        #region Work Queue Access
        /// <summary>
        /// Determines if the work queue is empty.
        /// </summary>
        /// <returns>true if the work queue is empty; otherwise false.</returns>
        private bool IsWorkQueueEmpty()
        {
            foreach (LinkedList<AsyncOperation> queue in items)
            {
                if (queue.Count > 0)
                    return false;
            }

            return true;
        }
        /// <summary>
        /// Adds the operation to the work queue.
        /// </summary>
        /// <param name="argument">The argument of an asynchronous operation.</param>
        /// <param name="priority">A value between 0 and <see cref="PriorityQueues"/> indicating the priority of this item.
        /// An item with a higher priority will be processed before items with lower priority.</param>
        private void AddWork(object argument, int priority)
        {
            // Create an async operation for this work item
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(argument);

            if (processingMode == ProcessingMode.FIFO)
                items[priority].AddLast(asyncOp);
            else
                items[priority].AddFirst(asyncOp);
        }
        /// <summary>
        /// Gets a pending operation from the work queue.
        /// </summary>
        /// <returns>A 2-tuple whose first component is the the pending operation with 
        /// the highest priority from the qork queue and the second component is the
        /// priority.</returns>
        private Utility.Tuple<AsyncOperation, int> GetWork()
        {
            AsyncOperation request = null;
            int priority = 0;

            for (int i = priorityQueues - 1; i >= 0; i--)
            {
                if (items[i].Count > 0)
                {
                    priority = i;
                    request = items[i].First.Value;
                    items[i].RemoveFirst();
                    break;
                }
            }

            return Utility.Tuple.Create(request, priority);
        }
        /// <summary>
        /// Rebuilds the work queue.
        /// </summary>
        private void BuildWorkQueue()
        {
            items = new LinkedList<AsyncOperation>[priorityQueues];
            for (int i = 0; i < priorityQueues; i++)
                items[i] = new LinkedList<AsyncOperation>();
        }
        /// <summary>
        /// Clears the work queue.
        /// </summary>
        private void ClearWorkQueue()
        {
            for (int i = 0; i < priorityQueues; i++)
                ClearWorkQueue(i);
        }
        /// <summary>
        /// Clears the work queue with the given priority.
        /// </summary>
        /// <param name="priority">A value between 0 and <see cref="PriorityQueues"/> 
        /// indicating the priority queue to cancel.</param>
        private void ClearWorkQueue(int priority)
        {
            while (items[priority].Count > 0)
            {
                AsyncOperation asyncOp = items[priority].First.Value;
                asyncOp.OperationCompleted();
                items[priority].RemoveFirst();
            }
        }
        #endregion

        #region Worker Threads
        /// <summary>
        /// Creates the thread array.
        /// </summary>
        private void CreateThreads()
        {
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(new ThreadStart(Run));
                threads[i].IsBackground = true;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Represents the mode in which the work items are processed.
        /// Processing mode cannot be changed after any work is added to the work queue.
        /// </summary>
        [Browsable(true), Category("Behaviour"), DefaultValue(typeof(ProcessingMode), "FIFO")]
        public ProcessingMode ProcessingMode
        {
            get { return processingMode; }
            set
            {
                if (started)
                    throw new System.Threading.ThreadStateException("The thread has already been started.");

                processingMode = value;
                BuildWorkQueue();
            }
        }
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
                BuildWorkQueue();
            }
        }
        /// <summary>
        /// Determines whether the <see cref="QueuedBackgroundWorker"/> started working.
        /// </summary>
        [Browsable(false), Description("Determines whether the QueuedBackgroundWorker started working."), Category("Behavior")]
        public bool Started { get { return started; } }
        /// <summary>
        /// Gets or sets a value indicating whether or not the worker thread is a background thread.
        /// </summary>
        [Browsable(true), Description("Gets or sets a value indicating whether or not the worker thread is a background thread."), Category("Behavior")]
        public bool IsBackground
        {
            get { return threads[0].IsBackground; }
            set
            {
                for (int i = 0; i < threadCount; i++)
                    threads[i].IsBackground = value;
            }
        }
        /// <summary>
        /// Determines whether the <see cref="QueuedBackgroundWorker"/> is being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        /// <summary>
        /// Gets or sets the number of worker threads. Number of threads
        /// cannot be changed after any work is added to the work queue.
        /// </summary>
        [Browsable(true), Category("Behaviour"), DefaultValue(5)]
        public int Threads
        {
            get { return threadCount; }
            set
            {
                if (started)
                    throw new System.Threading.ThreadStateException("The thread has already been started.");

                threadCount = value;
                CreateThreads();
            }
        }
        #endregion

        #region Cancel
        /// <summary>
        /// Cancels all pending operations in all queues.
        /// </summary>
        public void CancelAsync()
        {
            lock (lockObject)
            {
                ClearWorkQueue();
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Cancels all pending operations in the given queue.
        /// </summary>
        /// <param name="priority">A value between 0 and <see cref="PriorityQueues"/> 
        /// indicating the priority queue to cancel.</param>
        public void CancelAsync(int priority)
        {
            if (priority < 0 || priority >= priorityQueues)
                throw new ArgumentException("priority must be between 0 and " + (priorityQueues - 1).ToString() + "  inclusive.", "priority");

            lock (lockObject)
            {
                ClearWorkQueue(priority);
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
        #endregion

        #region Get/Set Apartment State
        /// <summary>
        /// Gets the apartment state of the worker thread.
        /// </summary>
        public ApartmentState GetApartmentState()
        {
            return threads[0].GetApartmentState();
        }
        /// <summary>
        /// Sets the apartment state of the worker thread. The apartment state
        /// cannot be changed after any work is added to the work queue.
        /// </summary>
        public void SetApartmentState(ApartmentState state)
        {
            for (int i = 0; i < threadCount; i++)
                threads[i].SetApartmentState(state);
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
                    if (IsWorkQueueEmpty())
                        Monitor.Wait(lockObject);
                }

                // Loop until we exhaust the queue
                bool queueFull = true;
                while (queueFull && !Stopping)
                {
                    // Get an item from the queue
                    AsyncOperation asyncOp = null;
                    object request = null;
                    int priority = 0;
                    lock (lockObject)
                    {
                        // Check queues
                        Utility.Tuple<AsyncOperation, int> work = GetWork();
                        asyncOp = work.Item1;
                        priority = work.Item2;
                        if (asyncOp != null)
                            request = asyncOp.UserSuppliedState;

                        // Check if the item was removed
                        if (request != null && cancelledItems.ContainsKey(request))
                            request = null;
                    }

                    if (request != null)
                    {
                        Exception error = null;
                        // Start the work
                        QueuedWorkerDoWorkEventArgs arg = new QueuedWorkerDoWorkEventArgs(request, priority);
                        try
                        {
                            // Raise the do work event
                            OnDoWork(arg);
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        // Raise the work complete event
                        QueuedWorkerCompletedEventArgs arg2 = new QueuedWorkerCompletedEventArgs(request,
                            arg.Result, priority, error, arg.Cancel);
                        if (!Stopping)
                            asyncOp.PostOperationCompleted(workCompletedCallback, arg2);
                    }
                    else if (asyncOp != null)
                        asyncOp.OperationCompleted();

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        queueFull = !IsWorkQueueEmpty();
                    }
                }
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
                    ClearWorkQueue();
                    cancelledItems.Clear();
                    Monitor.Pulse(lockObject);
                }
            }

            disposed = true;
        }
        #endregion
    }
}
