namespace Genesis.AsyncInitializationGuard.Utility
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Xunit;

    public sealed class InitializationGuardFixture
    {
        [Fact]
        public void initialization_only_executes_once_if_successful()
        {
            var count = 0;
            var sut = new InitializationGuard(() => Observable.Start(() => { ++count; }, ImmediateScheduler.Instance));

            Assert.Equal(0, count);

            sut
                .Initialize()
                .Subscribe()
                .Dispose();
            Assert.Equal(1, count);

            sut
                .Initialize()
                .Subscribe()
                .Dispose();
            Assert.Equal(1, count);
        }

        [Fact]
        public void initialization_only_executes_once_even_when_there_are_multiple_initialization_attempts_at_once()
        {
            var count = 0;
            var init = new Subject<Unit>();
            var sut = new InitializationGuard(() => Observable.Start(() => { ++count; }, ImmediateScheduler.Instance).SelectMany(_ => init));

            Assert.Equal(0, count);

            sut
                .Initialize()
                .Subscribe()
                .Dispose();

            sut
                .Initialize()
                .Subscribe()
                .Dispose();

            sut
                .Initialize()
                .Subscribe()
                .Dispose();

            init.OnCompleted();
            Assert.Equal(InitializationGuardState.Initialized, sut.State);
            Assert.Equal(1, count);
        }

        [Fact]
        public void initialization_pipeline_must_complete_before_being_considered_done()
        {
            var initialize = new Subject<Unit>();
            var sut = new InitializationGuard(() => initialize);

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Initializing, sut.State);

            initialize.OnNext(Unit.Default);
            Assert.Equal(InitializationGuardState.Initializing, sut.State);

            initialize.OnNext(Unit.Default);
            Assert.Equal(InitializationGuardState.Initializing, sut.State);

            initialize.OnCompleted();
            Assert.Equal(InitializationGuardState.Initialized, sut.State);
        }

        [Fact]
        public void initialization_can_be_reattempted_if_it_fails()
        {
            var attempt = 0;
            var sut = new InitializationGuard(
                () => Observable
                    .Start(() => ++attempt, ImmediateScheduler.Instance)
                    .Select(a => a == 2)
                    .SelectMany(succeed => succeed ? Observable.Return(Unit.Default) : Observable.Throw<Unit>(new InvalidOperationException())));

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Uninitialized, sut.State);

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Initialized, sut.State);
        }

        [Fact]
        public void an_uninitialized_guard_has_a_state_of_uninitialized()
        {
            var sut = new InitializationGuard(() => Observable.Return(Unit.Default));
            Assert.Equal(InitializationGuardState.Uninitialized, sut.State);
        }

        [Fact]
        public void an_initializing_guard_has_a_state_of_initializing()
        {
            var sut = new InitializationGuard(() => Observable.Never<Unit>());

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Initializing, sut.State);
        }

        [Fact]
        public void an_initialized_guard_has_a_state_of_initialized()
        {
            var sut = new InitializationGuard(() => Observable.Return(Unit.Default));

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Initialized, sut.State);
        }

        [Fact]
        public void state_resets_to_uninitialized_if_initialization_fails()
        {
            var sut = new InitializationGuard(() => Observable.Throw<Unit>(new InvalidOperationException()));

            sut
                .Initialize()
                .Subscribe();
            Assert.Equal(InitializationGuardState.Uninitialized, sut.State);
        }

        [Fact]
        public void ensure_initialized_succeeds_if_initialized()
        {
            var sut = new InitializationGuard(() => Observable.Return(Unit.Default));
            sut
                .Initialize()
                .Subscribe();

            sut.EnsureInitialized();
        }

        [Fact]
        public void error_propagates_if_initialization_fails()
        {
            var sut = new InitializationGuard(() => Observable.Throw<Unit>(new InvalidOperationException("Whatever")));
            Exception exception = null;
            sut
                .Initialize()
                .Subscribe(
                    _ => { },
                    ex => exception = ex);

            Assert.NotNull(exception);
            Assert.Equal("Whatever", exception.Message);
        }

        [Fact]
        public void ensure_initialized_fails_if_not_yet_initialized()
        {
            var sut = new InitializationGuard(() => Observable.Return(Unit.Default));
            var ex = Assert.Throws<InitializationException>(() => sut.EnsureInitialized());
            Assert.Equal("Not yet initialized.", ex.Message);
        }

        [Fact]
        public void disposing_disconnects_the_initialization_pipeline()
        {
            var init = new Subject<Unit>();
            var sut = new InitializationGuard(() => init);

            var initialized = false;
            sut
                .Initialize()
                .Subscribe(_ => initialized = true);

            sut.Dispose();
            init.OnCompleted();

            Assert.False(initialized);
        }
    }
}