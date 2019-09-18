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
            //test1();

            //measure length of window. If enough, emit a tick.
            //await test2();

            //test3();

            //test4();
            //await test5();
            await test6();

            Console.WriteLine(">>> Le Fin <<<");
            Console.Read();
        }


        /// <summary>
        /// In Rx, it's just Throttle, no Debounce. They should mean the same thing.
        /// Debounce is used in electrical world, for ignoring the fasting changing state that happens when a switch is flipped.
        /// Throttle here just continues to ignore fasting changing observables, the time period specified as Throttle(TimeSpan)
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
        /// 'mostrecent' - non-blocking, thus side effect rules here.
        /// 'latest' - blocking, thus pipeline rules.
        /// </summary>
        private static void test3()
        {
            var t = Observable.Interval(TimeSpan.FromSeconds(1)).Latest()
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
