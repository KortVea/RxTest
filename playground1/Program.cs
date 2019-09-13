using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
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
            await test2();


        }

        private static async Task test2()
        {
            var bandStatus = new BehaviorSubject<Status>(Status.Disconnected);
            var closingWindowCondition = bandStatus.Where(i => i != Status.Disconnected).DistinctUntilChanged();

            var openingWindowCondition = bandStatus.Where(i => i == Status.Disconnected).DistinctUntilChanged();

            var condition = bandStatus.Window(openingWindowCondition, _ => closingWindowCondition);

            var idx = 0;
            var initialdt = DateTimeOffset.Now;

            var signalingSub = new Subject<Unit>();

            condition.SubscribeOn(TaskPoolScheduler.Default).Subscribe(w =>
            {
                initialdt = DateTimeOffset.Now;
                Console.WriteLine($"Window {idx++} is here");
                w.Subscribe(_ => { }, () =>
                {
                    Console.WriteLine($"(Window {idx} closing)");
                    if (DateTimeOffset.Now - initialdt > TimeSpan.FromSeconds(3))
                    {
                        signalingSub.OnNext(Unit.Default);
                    }

                });
            });

            ///How to use DateTime with TestSchedular?
            //var scheduler = new TestScheduler();
            //signalingSub.ObserveOn(scheduler).Subscribe(_ => Console.WriteLine("valid disconnection period happened."));
            //bandStatus.OnNext(Status.Connected);
            //bandStatus.OnNext(Status.Disconnected);
            //scheduler.AdvanceBy(TimeSpan.FromSeconds(14).Ticks);
            //bandStatus.OnNext(Status.Connected);
            //scheduler.AdvanceBy(TimeSpan.FromSeconds(15).Ticks);
            //scheduler.AdvanceBy(TimeSpan.FromSeconds(2).Ticks);

            signalingSub.ObserveOn(TaskPoolScheduler.Default).Subscribe(_ => Console.WriteLine(">>>>>>> valid disconnection happened <<<<<<<"));

            //behavior: when connected, and if there has been x amount of time in disconnection, tick.
            //Even though the tighter condition would be that
            //if more than x sec has passed in disconnected state, this subject should tick already.
            //It doesn't make sense with Disconnected since we're upgrading.
            //so this behavior is ok.
            Console.WriteLine("first disconnected to open the window");
            bandStatus.OnNext(Status.Disconnected);
            await Task.Delay(TimeSpan.FromSeconds(4));
            Console.WriteLine("4 sec delay in Disconnected state, then Connected, done. Now it's valid time.");
            bandStatus.OnNext(Status.Connected);
            
            Console.WriteLine("===============\nnow Connected");
            Console.WriteLine("rest 1 sec");
            await Task.Delay(TimeSpan.FromSeconds(1));

            bandStatus.OnNext(Status.Disconnected);
            await Task.Delay(TimeSpan.FromSeconds(2));
            bandStatus.OnNext(Status.Connected);
            Console.WriteLine("2 sec of disconnection. Nothing should happen.");
            



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
