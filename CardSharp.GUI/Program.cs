using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CardSharp.GameComponents;

namespace CardSharp.GUI
{
    class Program
    {
        static void Main(string[] args)
        {


            while (true) {
                Console.WriteLine("-CardSharp v0 Test Menu-");
                Console.WriteLine();
                Console.WriteLine("1.Play a round");
                Console.WriteLine("2.Run auto test");
                Console.WriteLine("3.Generate fucking seeds");
                Console.Write("Your choice: ");
                var r = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine();
                switch (r.Key) {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        RunTest();
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        RunAutoTest();
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        SeedGen();
                        break;
                }
                Console.WriteLine();

            }
        }

        static volatile uint total = 0;
        static volatile uint valid = 0;
        static readonly object _flock = new object();
        private static void SeedGen()
        {
            var cards = Desk.GenerateCards().ToArray();

            File.Delete("seeds.txt");
            var outs = new BufferedStream(File.OpenWrite("seeds.txt"));
            ThreadPool.SetMinThreads(16, 16);
            ThreadPool.SetMaxThreads(64, 64);
            var startTime = DateTime.Now;
            
            Parallel.For(int.MinValue, int.MaxValue, new ParallelOptions { MaxDegreeOfParallelism = 64 }, i =>
            {
                total++;
                var list = new Card[cards.Length];
                Array.Copy(cards, 0, list, 0, 54);
                list.Shuffle(i);

                var doubleKing = false;
                int ptr = 0, cnt = 0;
                for (var r = 1; r <= 3; r++)
                {
                    var nCards = 17;
                    if (r == 3) nCards = 20;
                    var lcds = new Card[nCards];
                    Array.Copy(list, ptr, lcds, 0, nCards);
                    ptr += nCards;
                    Array.Sort(lcds);
                    
                    int prev = -1, lcnt = 0, lcnt2 = 0;
                    for (var x = 0;x < nCards; x++)
                    {
                        var c = lcds[x];
                        var amount = c.Amount.Amount;
                        if (amount == prev)
                            lcnt++;
                        else
                            lcnt = 0;
                        if (lcnt == 3)
                            cnt++;
                        if (c.Type == CardType.King)
                            lcnt2++;
                        prev = amount;
                    }

                    if (lcnt2 == 2) doubleKing = true;
                }

                if (cnt <= 5) return;
                valid++;
                
                var t = (DateTime.Now - startTime).TotalMilliseconds;
                var str = $"Bomb count: {cnt}, seed {i} , doubleKing {doubleKing}, TotalCount {total}, validCount {valid}, time {t/60}s, totalSpeed {total / t}/ms, validS {valid / t * 1000 * 60}/min\r\n";
                var str2 = $"{i} {cnt} {doubleKing} {t/60} {total/t} {valid/t*1000*60}";
                var bts = Encoding.UTF8.GetBytes(str2);
                lock (_flock)
                {
                    outs.WriteAsync(bts, 0, bts.Length);
                }

                if (cnt <= 6) return;
                Console.ForegroundColor = doubleKing ? ConsoleColor.Yellow : ConsoleColor.White;
                Console.Write(str);
            });
            outs.Close();
        }

        private static int _count;
        private static void RunAutoTest()
        {
            var sw = Stopwatch.StartNew();
            var desk = Desk.GetOrCreateDesk(Rng.NextDouble().ToString(CultureInfo.InvariantCulture));
            desk.AddPlayer(new FakePlayer(desk));
            desk.AddPlayer(new FakePlayer(desk));
            desk.AddPlayer(new FakePlayer(desk));

            desk.Start();
            _count++;
            Console.WriteLine($"Test successful: {_count}\tUsed {sw.ElapsedMilliseconds}ms.");
        }

        private static readonly Random Rng = new Random("fork you kamijoutoma".GetHashCode());
        private static void RunTest()
        {
            var desk = Desk.GetOrCreateDesk(Rng.NextDouble().ToString(CultureInfo.InvariantCulture));
            desk.AddPlayer(new Player("1"));
            desk.AddPlayer(new FakePlayer(desk));
            desk.AddPlayer(new FakePlayer(desk));

            desk.Start();

            Task.Run(() => { ShowMessage(desk); });

            ParseMessage(desk);
        }

        private static void ParseMessage(Desk desk)
        {
            while (desk.State != GameState.Unknown) {
                var line = Console.ReadLine();
                desk.ParseCommand(desk.CurrentPlayer.PlayerId, line);
                Thread.Sleep(10);
            }
        }

        private static void ShowMessage(Desk desk)
        {
            while (desk.State != GameState.Unknown) {
                if (desk.Message != null)
                    ShowMessage(desk, "[Desk]:    ");

                foreach (var player in desk.Players.Where(p => !(p is FakePlayer)))
                    ShowMessage(player, $"[{player.PlayerId}]: ");

                Thread.Sleep(10);
            }
        }

        private static void ShowMessage(IMessageSender sender, string id)
        {
            if (sender.Message != null) {
                Console.WriteLine(id + sender.Message);
                sender.ClearMessage();
            }
        }
    }
}
