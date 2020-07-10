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
            test17();
            Console.WriteLine(">>> Le Fin <<<");
            Console.Read();
        }

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
