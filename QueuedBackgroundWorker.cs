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

        private Thread thread;
        private bool stopping;
        private bool started;
        private SynchronizationContext context;

        private Queue<WorkItem>[] items;
        private Dictionary<object, bool> cancelledItems;

        private readonly SendOrPostCallback workCompletedCallback;
        #endregion

        #region WorkItem Class
        /// <summary>
        /// Represents a work item in the thread queue.
        /// </summary>
        private class WorkItem
        {
            #region Properties
            /// <summary>
            /// Gets the key identifying this item.
            /// </summary>
            public object UserState { get; private set; }
            /// <summary>
            /// Gets user data for this item.
            /// </summary>
            public object Argument { get; private set; }
            #endregion

            #region Constructor
            /// <summary>
            /// Initializes a new instance of the <see cref="WorkItem"/> class.
            /// </summary>
            /// <param name="userState">The key identifying this item.</param>
            /// <param name="argument">User data for this item.</param>
            /// <param name="priority">The priority of this item.</param>
            public WorkItem(object userState, object argument)
            {
                UserState = userState;
                Argument = argument;
            }
            #endregion
        }
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

            thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;

            // Work items
            items = new Queue<WorkItem>[6];
            cancelledItems = new Dictionary<object, bool>();

            // The loader complete callback
            workCompletedCallback = new SendOrPostCallback(this.WorkerCompletedCallback);
        }
        #endregion

        #region RunWorkerAsync
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        /// <param name="userState">A object identifying this work item.</param>
        /// <param name="argument">A parameter for use by the background operation 
        /// to be executed in the <see cref="DoWork"/> event handler.</param>
        /// <param name="priority">A value between 0 and 5 indicating the priority of this item.
        /// An item with a higher priority will be processed before items with lower priority.</param>
        public void RunWorkerAsync(object userState, object argument, int priority)
        {
            if (priority < 0 || priority > 5)
                throw new ArgumentException("priority must be between 0 and 5 inclusive.", "priority");

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
                items[priority].Enqueue(new WorkItem(userState, argument));
                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        /// <param name="userState">A object identifying this work item.</param>
        /// <param name="argument">A parameter for use by the background operation 
        /// to be executed in the <see cref="DoWork"/> event handler.</param>
        public void RunWorkerAsync(object userState, object argument)
        {
            RunWorkerAsync(userState, argument, 0);
        }
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        /// <param name="userState">A object identifying this work item.</param>
        public void RunWorkerAsync(object userState)
        {
            RunWorkerAsync(userState, null, 0);
        }
        /// <summary>
        /// Starts processing a new background operation.
        /// </summary>
        public void RunWorkerAsync()
        {
            RunWorkerAsync(null, null, 0);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the QueuedBackgroundWorker being stopped.
        /// </summary>
        private bool Stopping { get { lock (lockObject) { return stopping; } } }
        #endregion

        #region Cancel and Stop
        /// <summary>
        /// Cancels all pending operations.
        /// </summary>
        public void CancelAllAsync()
        {
            lock (lockObject)
            {
                foreach (Queue<WorkItem> queue in items)
                    queue.Clear();

                Monitor.Pulse(lockObject);
            }
        }
        /// <summary>
        /// Cancels processing the item with the given key.
        /// </summary>
        public void CancelAsync(object userState)
        {
            lock (lockObject)
            {
                if (!cancelledItems.ContainsKey(userState))
                {
                    cancelledItems.Add(userState, false);
                    Monitor.Pulse(lockObject);
                }
            }
        }
        /// <summary>
        /// Cancels all pending operations and stops the worker thread.
        /// </summary>
        public void Stop()
        {
            lock (lockObject)
            {
                if (!stopping)
                {
                    stopping = true;
                    Monitor.Pulse(lockObject);
                }
            }
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Used to call OnRunWorkerCompleted by the SynchronizationContext.
        /// </summary>
        /// <param name="arg">The argument.</param>
        private void WorkerCompletedCallback(object arg)
        {
            OnRunWorkerCompleted((QueuedWorkerCompletedEventArgs)arg);
        }
        /// <summary>
        /// Raises the RunWorkerCompleted event.
        /// </summary>
        /// <param name="e">An RunWorkerCompletedEventArgs that contains event data.</param>
        protected virtual void OnRunWorkerCompleted(QueuedWorkerCompletedEventArgs e)
        {
            if (RunWorkerCompleted != null)
                RunWorkerCompleted(this, e);
        }
        /// <summary>
        /// Raises the DoWork event.
        /// </summary>
        /// <param name="e">An QueuedDoWorkEventArgs that contains event data.</param>
        protected virtual void OnDoWork(QueuedDoWorkEventArgs e)
        {
            if (DoWork != null)
                DoWork(this, e);
        }
        #endregion

        #region Apartment State Methods
        /// <summary>
        /// Gets the apartment state of the worker thread.
        /// </summary>
        protected ApartmentState GetApartmentState()
        {
            return thread.GetApartmentState();
        }
        /// <summary>
        /// Sets the apartment state of the worker thread. The apartment state
        /// cannot be changed after any work is added to the work queue.
        /// </summary>
        protected void SetApartmentState(ApartmentState state)
        {
            thread.SetApartmentState(state);
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when the background operation of an item has completed,
        /// has been canceled, or has raised an exception.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when the background operation of an item has completed.")]
        public event RunWorkerCompletedEventHandler RunWorkerCompleted;
        /// <summary>
        /// Occurs when <see cref="RunWorkerAsync(object, object, bool)" /> is called.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when RunWorkerAsync is called.")]
        public event DoWorkEventHandler DoWork;
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
                    foreach (Queue<WorkItem> queue in items)
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
                    WorkItem request = null;
                    lock (lockObject)
                    {
                        // Check queues
                        for (int i = 5; i >= 0; i--)
                        {
                            if (items[i].Count > 0)
                            {
                                request = items[i].Dequeue();
                                break;
                            }
                        }

                        // Check if the item was removed
                        if (request != null && cancelledItems.ContainsKey(request.UserState))
                            request = null;
                    }

                    if (request != null)
                    {
                        object result = null;
                        Exception error = null;
                        bool cancelled = false;
                        // Read the image
                        try
                        {
                            // Raise the do work complete event
                            QueuedDoWorkEventArgs arg = new QueuedDoWorkEventArgs(request.UserState, request.Argument);
                            OnDoWork(arg);
                            result = arg.Result;
                            cancelled = arg.Cancel;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        // Raise the work complete event
                        QueuedWorkerCompletedEventArgs arg2 = new QueuedWorkerCompletedEventArgs(request.UserState,
                            result, error, cancelled);
                        context.Post(workCompletedCallback, arg2);
                    }

                    // Check if the cache is exhausted
                    lock (lockObject)
                    {
                        queueFull = false;
                        foreach (Queue<WorkItem> queue in items)
                        {
                            if (queue.Count > 0)
                            {
                                queueFull = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    #region Event Delegates
    /// <summary>
    /// Represents the method that will handle the RunWorkerCompleted event.
    /// </summary>
    /// <param name="sender">The object that is the source of the event.</param>
    /// <param name="e">A QueuedWorkerCompletedEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RunWorkerCompletedEventHandler(object sender, QueuedWorkerCompletedEventArgs e);
    /// <summary>
    /// Represents the method that will handle the DoWork event.
    /// </summary>
    /// <param name="sender">The object that is the source of the event.</param>
    /// <param name="e">A QueuedDoWorkEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void DoWorkEventHandler(object sender, QueuedDoWorkEventArgs e);
    #endregion

    #region Event Arguments
    /// <summary>
    /// Represents the event arguments of the RunWorkerCompleted event.
    /// </summary>
    public class QueuedWorkerCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Gets a value that represents the result of an asynchronous operation.
        /// </summary>
        public object Result { get; private set; }

        /// <summary>
        /// Initializes a new instance of the QueuedWorkerCompletedEventArgs class.
        /// </summary>
        /// <param name="userState">The unique identifier for the asynchronous task.</param>
        /// <param name="result">The result of an asynchronous operation.</param>
        /// <param name="error">The error that occurred while loading the image.</param>
        /// <param name="cancelled">A value indicating whether the asynchronous operation was canceled.</param>
        public QueuedWorkerCompletedEventArgs(object userState, object result, Exception error, bool cancelled)
            : base(error, cancelled, userState)
        {
            Result = result;
        }
    }
    /// <summary>
    /// Represents the event arguments of the DoWork event.
    /// </summary>
    public class QueuedDoWorkEventArgs : DoWorkEventArgs
    {
        /// <summary>
        /// Gets the unique identifier for the asynchronous task.
        /// </summary>
        public object UserState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the QueuedDoWorkEventArgs class.
        /// </summary>
        /// <param name="userState">The unique identifier for the asynchronous task.</param>
        /// <param name="argument">The argument of an asynchronous operation.</param>
        public QueuedDoWorkEventArgs(object userState, object argument)
            : base(argument)
        {
            UserState = userState;
        }
    }
    #endregion
}
