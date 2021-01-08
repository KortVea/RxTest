using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using ReactiveUI;

namespace playground1
{
    class Program
    {
        public enum Status
        {
            Connected,
            Disconnected
        }

        static async Task Main(string[] args)
        {
            test21();
            Console.WriteLine(">>> Le Fin <<<");
            Console.Read();
        }

        private static void test21()
        {
            var sut = Observable.Generate(1, _ => true, i => i+=1, i => i, i => TimeSpan.FromSeconds(Math.Sin(i)));
            sut
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Subscribe(Console.WriteLine);
        }

        /// <summary>
        /// Execute().Subscribe() execute the command once, and only get the last result when the command obs `completes`. And the result is behavioral.
        /// command.Subscribe() simply observes whatever ticks through. but without Execute, nothing will tick.
        /// 
        /// // An asynchronous command created from IObservable<int> that 
        /// // waits 2 seconds and then returns 42 integer.
        /// var command = ReactiveCommand.CreateFromObservable<Unit, int>(
        ///    _ => Observable.Return(42).Delay(TimeSpan.FromSeconds(2)));
        ///
        /// Subscribing to the observable returned by `Execute()` will 
        /// tick through the value `42` with a 2-second delay.
        /// command.Execute(Unit.Default).Subscribe();
        ///
        /// We can also subscribe to _all_ values that a command
        /// emits by using the `Subscribe()` method on the
        /// ReactiveCommand itself.
        /// command.Subscribe(value => Console.WriteLine(value));
        ///
        /// Regardless of whether your command is synchronous or asynchronous in nature, you execute it via the Execute method.
        /// You get back an observable that will tick the command's result value when execution completes
        ///
        /// // Creates a hot Observable<T> that emits a new value every 5 
        /// minutes and invokes the SaveCommand<Unit, Unit>. Don't forget
        /// to dispose the subscription produced by InvokeCommand().
        /// var interval = TimeSpan.FromMinutes(5);
        /// Observable.Timer(interval, interval)
        /// .Select(time => Unit.Default)
        ///     .InvokeCommand(this, x => x.SaveCommand);
        /// Hint InvokeCommand respects the command's executability. That is, if the command's CanExecute method returns false, InvokeCommand will not execute the command when the source observable ticks.
        /// </summary>
        private static void test20()
        {
            var exe = Observable.Interval(TimeSpan.FromSeconds(1)).Take(3);
           
            var command = ReactiveCommand.CreateFromObservable<long>(() => exe, Observable.Return(false));
            command.Subscribe(x => Console.WriteLine($"cmd subbed: {x}"));//nothing happens with this.

            //command.Execute().Subscribe(x => Console.WriteLine($"exe subbed: {x}"));//now command sub sees all, while exe sub sees the final one.
            //command.FirstOrDefaultAsync().Subscribe(x => Console.WriteLine($"command ? exe? subbed: {x}")); 
            // command! first or async. without execute, doesn't work.

            // Execute() overrides canExecute ???? YES!!!!
            //command.FirstOrDefaultAsync().Subscribe(x => Console.WriteLine($"first or default : {x}")); // Not an answer.

            // InvokeCommand is the answer. It respects canExecute!
            Observable.Start((() => { })).InvokeCommand(command);
        }

        /// static functions can have local variables and they're retained IF the static function returns a pointer (FUNC) so that it's kept alive
        /// multiple tasks exist in once Observable is problematic in terms of reentrancy. Instead, each should be in an Observable.FromAsync, and use some kinda lock to prevent reentrancy.

        /// <summary>
        /// NOT good for when you wish each projected observable to complete before CONCAT with the next,
        /// because this is NOT a CONCAT. It'd be only a few different obs in existance.
        /// Use IEnumerable of IObserable then CONCAT.
        /// </summary>
        private static void test19()
        {
            var test = new[] {"a", "b", "c" };
            test
                .ToObservable()
                .Select(x => x + "-")
                .Subscribe(i => Console.Write(i));
        }
        /// <summary>
        /// Interlocked class to protect shared resource
        /// </summary>
        //private static void test18()
        //{
        //    MyInterlockedExchangeExampleClass.Main();
        //}

        //class MyInterlockedExchangeExampleClass
        //{
        //    //0 for false, 1 for true.
        //    private static int usingResource = 0;

        //    private const int numThreadIterations = 5;
        //    private const int numThreads = 10;

        //    public static void Main()
        //    {
        //        Thread myThread;
        //        Random rnd = new Random();

        //        for (int i = 0; i < numThreads; i++)
        //        {
        //            myThread = new Thread(new ThreadStart(MyThreadProc));
        //            myThread.Name = String.Format("Thread{0}", i + 1);

        //            //Wait a random amount of time before starting next thread.
        //            Thread.Sleep(rnd.Next(0, 1000));
        //            myThread.Start();
        //        }
        //    }

        //    private static void MyThreadProc()
        //    {
        //        for (int i = 0; i < numThreadIterations; i++)
        //        {
        //            UseResource();

        //            //Wait 1 second before next attempt.
        //            Thread.Sleep(1000);
        //        }
        //    }

        //    //A simple method that denies reentrancy.
        //    static bool UseResource()
        //    {
        //        //0 indicates that the method is not in use.
        //        if (0 == Interlocked.Exchange(ref usingResource, 1))
        //        {
        //            Console.WriteLine("{0} acquired the lock", Thread.CurrentThread.Name);

        //            //Code to access a resource that is not thread safe would go here.

        //            //Simulate some work
        //            Thread.Sleep(500);

        //            Console.WriteLine("{0} exiting lock", Thread.CurrentThread.Name);

        //            //Release the lock
        //            Interlocked.Exchange(ref usingResource, 0);
        //            return true;
        //        }
        //        else
        //        {
        //            Console.WriteLine("   {0} was denied the lock", Thread.CurrentThread.Name);
        //            return false;
        //        }
        //    }
        //}
        /// <summary>
        /// RefCount is handy for
        /// 1. reference counting how many sub there are, if reaching 0 then disposing the CONNECTION automatically. Further sub will reset the pipeline (starting all over again)
        /// 2. same for the auto .Connect() maneuver - if goes from NONE to ONE, then .Connect() is called right when .sub is called.
        /// </summary>
        private static void test17()
        {
            var period = TimeSpan.FromSeconds(1);
            var observable = Observable.Interval(period)
                .Do(l => Console.WriteLine("Publishing {0}", l)) //side effect to show it is running
                .Publish()
                .RefCount();
            //observable.Connect(); Use RefCount instead now 
            Console.WriteLine("Press any key to subscribe");
            Console.ReadKey();
            var subscription = observable.Subscribe(i => Console.WriteLine("subscription : {0}", i));
            var subscription2 = observable.Subscribe(i => Console.WriteLine("subscription2 : {0}", i));
            Console.WriteLine("Press any key to unsubscribe one of the two.");
            Console.ReadKey();
            subscription.Dispose();
            Console.WriteLine("Press any key to subscribe");
            Console.ReadKey();
            var subscription3 = observable.Subscribe(i => Console.WriteLine("subscription3 : {0}", i));
            Console.WriteLine("Press any key to un sub 2 and 3, and sub 4.");
            Console.ReadKey();
            subscription3.Dispose();
            subscription2.Dispose();
            var sub4 = observable.Subscribe(i => Console.WriteLine("subscription4 : {0}", i));
            Console.WriteLine("Press any key to unsub 4.");
            Console.ReadKey();
            sub4.Dispose();

        }

        /// <summary>
        /// publish IS sharing, meaning the pipeline is working if Connected, no need of a subscription.
        /// Only disposal of the connection will possibly reset the hot observable.
        /// subscription is about 'client' side receiving. unsub doesn't affect the publishing / sharing at all.
        /// RefCount is about automatic disposal and lazy connection.
        /// </summary>
        private static void test16()
        {
            var period = TimeSpan.FromSeconds(2);
            var observable = Observable.Interval(period)
                .Do(l => Console.WriteLine("Publishing {0}", l)) //Side effect to show it is running
                .Publish();
            Console.WriteLine("Press any key to connect");
            Console.ReadKey();
            var conn = observable.Connect();
            Console.WriteLine("connected.");
            Console.WriteLine("Press any key to subscribe");
            Console.ReadKey();
            var subscription = observable.Subscribe(i => Console.WriteLine("subscription : {0}", i));
            Console.WriteLine("Press any key to unsubscribe.");
            Console.ReadKey();
            subscription.Dispose();
            Console.WriteLine("Press any key to disconnect.");
            Console.ReadKey();
            conn.Dispose();
            Console.WriteLine("Press any key to connect again.");
            Console.ReadKey();
            var conn2 = observable.Connect();
            
        }

        /// <summary>
        /// repeat with backoff
        /// </summary>
        private static void test15()
        {
            var source = Observable.Range(1, 3);
            source
                .RepeatWithBackoff(2, i => TimeSpan.FromSeconds(3))
                .Subscribe(i => Console.WriteLine($"{i}"),
                    (() => Console.WriteLine("completed")));

        }

        /// <summary>
        /// race with amb for repeating until a success.
        /// </summary>
        private static void test14()
        {
            //.Timeout won't work because timeout measure between two elements the length of time.

            var timer = Observable.Timer(TimeSpan.FromSeconds(6)).Select(_ => false);
            var built = Observable.FromAsync(() => getResult())
                //.Delay(TimeSpan.FromSeconds(0.1))
                .Repeat()
                .TakeWhileInclusive(succeeded => !succeeded)
                .Where(succeeded => succeeded)
                .Do(i => Console.WriteLine($"built pipeline reached an {i}"));

            Observable
                .Amb(built, timer)
                .Subscribe(i => Console.WriteLine($"amb ticked - {i}"),
                (() => Console.WriteLine($"completed")));

        }

        private static int count = 0;
        private static Task<bool> getResult()
        {
            count++;
            var result = count == 5;
            //await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine($"async getResult = {result}");
            return Task.FromResult(result);
        }

        private static void test13()
        {

        }

        /// <summary>
        /// how to delay a throw otherwise instantaneous.
        /// </summary>
        private static void test12()
        {
            Observable
                .Throw<TimeoutException>(new TimeoutException())
                .Materialize()
                .Delay(TimeSpan.FromSeconds(2))
                .Dematerialize()
                .Subscribe(x => Console.WriteLine(x.ToString()),
                    i => Console.WriteLine(i.ToString()),
                    ()=> Console.WriteLine("completed"));
        }
        enum Tenum
        {
            High, Low
        }
        private static void test111()
        {
            const Tenum t = Tenum.High;
            Console.WriteLine(t is Tenum.Low);
        }

        /// <summary>
        /// dispose doesn't make sub a null.
        /// </summary>
        /// <returns></returns>
        private static async Task test11()
        {
            var sub = new Subject<bool>();
            sub.OnCompleted();
            sub.Dispose();
            var sub2 = Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(_ => Console.WriteLine("sub 2 done"));
            await Task.Delay(TimeSpan.FromSeconds(2));
            Console.ReadKey();
            Console.WriteLine("unsubbing ...");
            sub2.Dispose();
        }
        /// <summary>
        /// one pipeline's step triggers the latest value from another pipe
        /// </summary>
        /// <returns></returns>
        private static async Task test10()
        {
            var origin = Observable.Interval(TimeSpan.FromSeconds(1)).Publish();
            var osub =origin.Subscribe(x => Console.WriteLine($"origin: {x}"));
            origin.Connect();

            var ctrl1 = new Subject<bool>();
            var sut = ctrl1
                .DistinctUntilChanged()
                .Where(f => f)
                .WithLatestFrom(origin, (_, num) => num)
                .Select(x => x.ToString())
                .Subscribe(i => Console.WriteLine($">>>{i}<<<"));

            ctrl1.OnNext(true);
            Console.WriteLine("ctrl = true, no step-up trigger, nothing should appear come out of it");
            await Task.Delay(TimeSpan.FromSeconds(2.1));
            ctrl1.OnNext(false);
            Console.WriteLine("ctrl = false, pre-condition; without the publish, the origin should've been disposed of.");
            await Task.Delay(TimeSpan.FromSeconds(2.1));
            ctrl1.OnNext(true);
            Console.WriteLine("ctrl = true, step-up trigger! the origin is subscribed from here ? its current value should be returned");
            await Task.Delay(TimeSpan.FromSeconds(2.1));
            ctrl1.OnNext(false);
            ctrl1.OnNext(true);
            Console.WriteLine("one step: ");
            await Task.Delay(TimeSpan.FromSeconds(2.1));
            ctrl1.OnNext(false);
            ctrl1.OnNext(true);
            Console.WriteLine("one step: ");
            await Task.Delay(TimeSpan.FromSeconds(2.1));
            sut.Dispose();
            Console.WriteLine("sut disposed");
            osub.Dispose();
            Console.WriteLine("origin disposed");
        }

        /// <summary>
        /// a case of picking out the very first tick
        /// </summary>
        private static void test9()
        {
            var ins = new BehaviorSubject<string>("");
            ins
                .Scan((count: (long)0, val: (string) null),
                    (acc, val) => (++ acc.count, val))
                .Select(info => info.val == string.Empty 
                    ? Observable.Interval(TimeSpan.FromSeconds(1)).Select(i => i.ToString())
                    : Observable.Return(info.val))
                .Switch()
                .Subscribe(x => Console.WriteLine($"{x}"));

            Thread.Sleep(TimeSpan.FromSeconds(3.1));
            ins.OnNext("a");
            
            ins.OnNext("");
            Thread.Sleep(TimeSpan.FromSeconds(3.1));
            ins.OnNext("b");
            ins.OnNext("");
            Thread.Sleep(TimeSpan.FromSeconds(3.1));
            ins.OnNext("c");
            ins.OnCompleted();
        }

        /// <summary>
        /// weird case of 
        /// </summary>
        private static void test8()
        {
            var epoch = 0;
            var instigator = new Subject<bool>();
            var pipe = instigator
                .DistinctUntilChanged()
                .Do(_ => epoch++)
                .Where(x => !x)
                .Select(_ => Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Do(i => Console.WriteLine($"epoch {epoch} value {i}")))
                .FirstOrDefaultAsync()
                .Switch()
                .Subscribe();
            instigator.OnNext(false); // working pipe
            instigator.OnNext(true); // it won't stop
            instigator.OnNext(false); // no new working
            instigator.OnNext(true); // no stop (it's the first still)

        }

        /// <summary>
        /// ref types are also passed by value. 
        /// without ref, function modifyEmpoyee has its own copy of data.
        /// </summary>
        private static void test7()
        {
            var e1 = new Employee { MyProperty = 1 };
            modifyEmpoyee(ref e1);
            Console.WriteLine(e1.MyProperty);
        }

        static void modifyEmpoyee(ref Employee e)
        {
            e = new Employee { MyProperty = 5 };
            Console.WriteLine(e.MyProperty);
        }
        class Employee
        {
            public int MyProperty { get; set; }
            
        }

        /// <summary>
        /// In Rx, it's just Throttle, no Debounce. They should mean the same thing.
        /// Debounce is used in electrical world, for ignoring the fasting changing state that happens when a switch is flipped.
        /// Throttle here just make it continue to ignore fast changing observables, the time period specified as Throttle(TimeSpan)
        /// </summary>
        /// <returns></returns>
        private static async Task test6()
        {
            //var s = new TestScheduler();
            //var t= new Subject<int>();
            var t = Observable.Generate(1, _ => true, i => i+1, i => i, i => TimeSpan.FromSeconds(Math.Abs(Math.Sin(i))))
                .Do(_ => Console.WriteLine("-"))
                .Throttle(TimeSpan.FromSeconds(0.5)).Timestamp();
            t.Select(i => mock( DateTime.Now - TimeSpan.FromHours(8), DateTime.Now)).Subscribe();
            //t.OnNext(1);
            //await Task.Delay(TimeSpan.FromSeconds(2));
            //t.OnNext(2);
            //t.OnNext(3);
            //t.OnNext(4);
            //t.OnNext(5);

        }

        private static async Task test5()
        {
            var t = DateTime.Now;
            var d = t.ToString("O");
            Console.WriteLine(d);
            var p = DateTime.Parse(d);
            Console.WriteLine(p);
        }

        static int mock( DateTime start, DateTime end)
        {
            Console.WriteLine("- mock called");
            return 3;
        }

        private static void test4()
        {
            var s = new List<int>();
            s = null;
            var b = s as IList<int>; //? new List<int>{1, 2} : new List<int>{3, 4};
            Console.WriteLine(b);
        }

        /// <summary>
        /// the speed of the stream is determined by
        /// 'mostrecent' - non-blocking, thus side effect^ rules here.
        /// 'latest' - blocking, thus pipeline rules.
        ///     ^ refers to the 10ms sleep in the subscription.
        /// </summary>
        private static void test3()
        {
            var t = Observable.Interval(TimeSpan.FromSeconds(1)).MostRecent(0)
                .ToObservable().Subscribe(i =>
                {
                    Thread.Sleep(10);
                    Console.WriteLine("latest i" + i);
                });
        }

        /// <summary>
        /// window function and scheduler for testing.
        /// </summary>
        /// <returns></returns>
        private static async Task test2()
        {
            var scheduler = new TestScheduler();
            var bandStatus = new BehaviorSubject<Status>(Status.Disconnected);
            var closingWindowCondition = bandStatus.Where(i => i != Status.Disconnected).DistinctUntilChanged();

            var openingWindowCondition = bandStatus.Where(i => i == Status.Disconnected).DistinctUntilChanged();

            var condition = bandStatus.Window(openingWindowCondition, _ => closingWindowCondition);

            var idx = 0;
            var initialdt = scheduler.Now;

            var signalingSub = new Subject<Unit>();

            condition
                .ObserveOn(scheduler)
                .Subscribe(w =>
            {
                initialdt = scheduler.Now;
                Console.WriteLine($"Window {idx++} is here");
                w.Subscribe(_ => { }, () =>
                {
                    Console.WriteLine($">>>>> Window {idx} closing");
                    if (scheduler.Now - initialdt > TimeSpan.FromSeconds(3))
                    {
                        signalingSub.OnNext(Unit.Default);
                    }

                });
            });

            ///How to use DateTime with TestSchedular? : IScheduler.Now

            signalingSub.Subscribe(_ => Console.WriteLine(">>>>>>> valid disconnection happened <<<<<<<"));

            //behavior: when connected, and if there has been x amount of time in disconnection, tick.
            //Even though the tighter condition would be that
            //if more than x sec has passed in disconnected state, this subject should tick already.
            //It doesn't make sense with Disconnected since we're upgrading.
            //so this behavior is ok.
            Console.WriteLine("first disconnected to open the window");
            bandStatus.OnNext(Status.Disconnected);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);
            Console.WriteLine("4 sec delay in Disconnected state, then Connected, done. Now it's valid time.");
            bandStatus.OnNext(Status.Connected);
            Console.WriteLine("===============\nnow Connected");
            Console.WriteLine("rest 1 sec");
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            bandStatus.OnNext(Status.Disconnected);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(2).Ticks);
            bandStatus.OnNext(Status.Connected);
            Console.WriteLine("2 sec of disconnection happened. No tick should happen.");
            



        }

        private static void test1()
        {
            var scheduler = new TestScheduler();

            var windowOpeningSubject = new Subject<Unit>();
            var windowClosingSubject = new Subject<Unit>();

            var truckSpeed = new Subject<int>();
            var truckConnection = new Subject<Status>();

            var idx = 0;
            truckConnection.Window(windowOpeningSubject, _ => windowClosingSubject)
                .Subscribe(s =>
                {
                    var startTime = DateTimeOffset.Now;
                    Console.WriteLine($"Window {idx}:");
                    s.Subscribe(status => Console.WriteLine(status),
                        () => Console.WriteLine($"Window {idx++} Completed from inner one."));
                }, () =>
                {
                    Console.WriteLine($"All windows Completed.");
                });

            truckConnection.OnNext(Status.Connected);

            windowOpeningSubject.OnNext(Unit.Default);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Connected);

            windowClosingSubject.OnNext(Unit.Default);

            //should be missing because it's not opened yet.
            truckConnection.OnNext(Status.Connected);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Connected);

            windowOpeningSubject.OnNext(Unit.Default);
            truckConnection.OnNext(Status.Connected);
            truckConnection.OnNext(Status.Connected);
            truckConnection.OnNext(Status.Connected);

            //opening without closing the last one
            //cause both windows to continue to work.
            windowOpeningSubject.OnNext(Unit.Default);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Disconnected);

            //closing both ?? Yes.
            windowClosingSubject.OnNext(Unit.Default);

            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Disconnected);
            truckConnection.OnNext(Status.Connected);

            truckConnection.OnCompleted();
        }
    }


}

namespace System.Reactive.Linq
{
    using System;

    public static class PaceExtensions
    {
        // see http://stackoverflow.com/a/21589238/5380
        // TODO: this implementation is terrible and doesn't allow control over scheduling. Replace with something sane.
        public static IObservable<T> Pace<T>(this IObservable<T> source, TimeSpan interval) =>
            source
                .Select(
                    i =>
                        Observable
                            .Empty<T>()
                            .Delay(interval)
                            .StartWith(i))
                .Concat();
    }
}
namespace System.Reactive.Linq
{
    public static class TakeWhileExtensions
    {
        // Emits matching values, but includes the value that failed the filter
        public static IObservable<T> TakeWhileInclusive<T>(
            this IObservable<T> source, Func<T, bool> predicate)
        {
            return source.Publish(co => co.TakeWhile(predicate)
                .Merge(co.SkipWhile(predicate).Take(1)));
        }
    }
}
