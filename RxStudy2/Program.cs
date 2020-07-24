using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace RxStudy2
{
    public class Sensor
    {
        // 値生成
        private static Random rdm = new Random();

        // 値生成間隔
        private Timer clock;

        // 名称
        public string Name { get; private set; }

        // コンストラクタ
        public Sensor(string name) 
        {
            this.Name = name;
        }

        // センサー動作開始
        public void Start() 
        {
            this.clock = new Timer(_ =>
            {
                this.OnPublish(rdm.Next(1001));
            },
            null,   // 引数なし
            0,      // 開始Delayなし
            1000);  // 1秒間隔
        }

        // 購読先への発行
        private void OnPublish(int value) 
        {
            this.Publish?.Invoke(this, new SensorEventArgs(this.Name, value));
        }

        // イベント
        public EventHandler<SensorEventArgs> Publish;
    }

    public class SensorEventArgs : EventArgs
    {
        public int Value { get; private set; }
        public string Name { get; private set; }
        public SensorEventArgs(string name, int value) 
        {
            this.Value = value;
            this.Name = name;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // センサーを生成
            var sensors = Enumerable.Range(1, 10).Select(i => new Sensor("Sensor#" + i.ToString("D02"))).ToArray();

            // 購読
            var s = Observable.Merge(
                sensors.Select(sensor => Observable.FromEvent<EventHandler<SensorEventArgs>, SensorEventArgs>(
                    h => (s,e) => h(e),
                    h => sensor.Publish += h,
                    h => sensor.Publish -= h)))
                .Buffer(TimeSpan.FromSeconds(10))
                .Select(values => values.Aggregate((x,y) => x.Value > y.Value ? x: y))
                .Subscribe(e => Console.WriteLine("{0}: {1}", e.Name, e.Value)
                );

            // センサー開始
            foreach (var sensor in sensors)
            {
                sensor.Start();
            }

            Console.WriteLine("Sensor started");
            Console.ReadLine();

            // Publishイベントの解除
            s.Dispose();

        }


        private static void Main0()
        {
            //https://blog.okazuki.jp/entry/20111205/1323056284
            Console.WriteLine("Hello World!");

            var s = new Subject<int>();

            Observable.Start(() =>
            {
                Console.WriteLine("Start latest loop");
                foreach (var i in s.MostRecent(-1))
                {
                    Console.WriteLine("LatestValue : {0}", i);
                    Thread.Sleep(500);
                }
                Console.WriteLine("End latest loop");
            });

            Observable.Start(() =>
            {
                foreach (var i in Enumerable.Range(1, 20))
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("OnNext({0})", i);
                    s.OnNext(i);
                }
                Console.WriteLine("OnCompleted()");
                s.OnCompleted();
            });


            Console.ReadLine();
        }
    }
}
