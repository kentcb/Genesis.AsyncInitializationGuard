namespace Genesis.AsyncInitializationGuard
{
    using System;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;

    /// <summary>
    /// Facilitates asynchronously initialization where the initialization can only occur once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of <c>InitializationGuard</c> can be used to protect against performing some asynchronous initialization
    /// logic more than once. The consuming code passes in the underlying asynchronous logic into the constructor, and then
    /// calls <see cref="Initialize"/> when required. The guard will transition into the
    /// <see cref="InitializationGuardState.Initializing"/> state, whereafter any subsequent invocations of
    /// <see cref="Initialize"/> will return the already-running asynchronous pipeline.
    /// </para>
    /// <para>
    /// Once initialization completes, the guard transitions to the <see cref="InitializationGuardState.Initialized"/> state.
    /// Thereafter, all invocations of <see cref="Initialize"/> will return an already-completed pipeline.
    /// </para>
    /// <para>
    /// If an error occurs during initialization (due to the provided initialization factory failing), the guard transitions
    /// back to the <see cref="InitializationGuardState.Uninitialized"/> state. Thus, a subsequent invocation of
    /// <see cref="Initialize"/> will request a new initialization pipeline via the provided factory.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{State}")]
    public sealed class InitializationGuard : IDisposable
    {
        private readonly IObservable<Unit> initialize;
        private readonly SerialDisposable connection;
        private readonly object connectionSync;
        private volatile InitializationGuardState state;
        private IObservable<Unit> connectedInitialize;
        private int disposed;

        /// <summary>
        /// Creates a new instance of the <c>InitializationGuard</c> class.
        /// </summary>
        /// <param name="initializeFactory">
        /// A factory that provides the underlying initialization logic.
        /// </param>
        public InitializationGuard(Func<IObservable<Unit>> initializeFactory)
        {
            this.initialize = Observable.Defer(initializeFactory);
            this.connection = new SerialDisposable();
            this.connectionSync = new object();
        }

        /// <summary>
        /// Gets the current state of the initialization guard.
        /// </summary>
        public InitializationGuardState State => this.state;

        /// <summary>
        /// Ensures that the underlying initialization logic is executed if necessary.
        /// </summary>
        /// <returns>
        /// An observable that ticks when the guard is initialized.
        /// </returns>
        public IObservable<Unit> Initialize()
        {
            if (this.state == InitializationGuardState.Initialized)
            {
                return Observable.Return(Unit.Default);
            }

            var connectedInitialize = this.connectedInitialize;

            if (connectedInitialize != null)
            {
                return connectedInitialize;
            }

            lock (this.connectionSync)
            {
                connectedInitialize = this.connectedInitialize;

                if (connectedInitialize != null)
                {
                    return connectedInitialize;
                }

                return this.Connect();
            }
        }

        /// <summary>
        /// Throws an <see cref="InitializationException"/> if this initialization guard is not yet initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            if (this.state != InitializationGuardState.Initialized)
            {
                throw new InitializationException("Not yet initialized.");
            }
        }

        /// <summary>
        /// Disposes of this initialization guard, aborting any in-flight initialization.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref this.disposed, 1, 0) != 0)
            {
                return;
            }

            this.connection.Dispose();
        }

        private IObservable<Unit> Connect()
        {
            this.state = InitializationGuardState.Initializing;

            var published = this
                .initialize
                .Do(
                    _ => { },
                    _ =>
                    {
                        this.state = InitializationGuardState.Uninitialized;

                        lock (this.connectionSync)
                        {
                            // init failed, so we reset the pipeline to null so the next attempt will reinstantiate it
                            this.connectedInitialize = null;
                        }
                    },
                    () => this.state = InitializationGuardState.Initialized)
                .PublishLast();

            this.connectedInitialize = published;
            this.connection.Disposable = published.Connect();

            return published;
        }
    }
}