﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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


            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
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
            ThreadLocal<Card[]> lists = new ThreadLocal<Card[]>(() => new Card[54]);
            ThreadLocal<int[]> bufs = new ThreadLocal<int[]>(() => new int[15]);
            Parallel.For(0, int.MaxValue, new ParallelOptions { MaxDegreeOfParallelism = 64 }, i =>
            {
                total++;
                var list = lists.Value;
                var buf = bufs.Value;
                Array.Copy(cards, 0, list, 0, 54);
                list.Shuffle(i);
                
                ProcessCards(list, buf, i, 1);
                for (int a = 0; a < 17; a++)
                {
                    var tmp = list[a];
                    list[a] = list[a + 34];
                    list[a + 34] = tmp;
                }
                ProcessCards(list, buf, i, 2);
                for (int a = 17; a < 34; a++)
                {
                    var tmp = list[a];
                    list[a] = list[a + 17];
                    list[a + 17] = tmp;
                }
                ProcessCards(list, buf, i, 3);
            });
            outs.Close();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ProcessCards(Card[] list, int[] buf, int i, int extra)
            {
                var doubleKing = false;
                int ptr = 0, cnt = 0;
                for (var r = 1; r <= 3; r++)
                {
                    int rightPtr = r * 17;
                    if (r == 3) rightPtr = 54;

                    int lcnt2 = 0;
                    for (; ptr < rightPtr; ptr++)
                    {
                        var c = list[ptr];
                        var amount = c.Amount.Amount;
                        buf[amount]++;
                        if (buf[amount] == 4)
                            cnt++;
                        if (c.Type == CardType.King)
                            lcnt2++;
                    }

                    if (lcnt2 == 2) doubleKing = true;
                    for (var a = 0; a < 15; a++) buf[a] = 0;
                }

                if (cnt <= 6) return;
                valid++;

                var t = (DateTime.Now - startTime).TotalMilliseconds;
                var str =
                    $"Bomb count: {cnt}, seed {i} , doubleKing {doubleKing}, TotalCount {total}, validCount {valid}, time {t / 1000}s, totalSpeed {total / t}/ms, validS {valid / t * 1000 * 60}/min {extra}\r\n";
                var str2 = $"{i} {cnt} {doubleKing} {t / 1000} {extra}\r\n";
                var bts = Encoding.UTF8.GetBytes(str2);
                lock (_flock)
                {
                    outs.WriteAsync(bts, 0, bts.Length);
                }
                Console.ForegroundColor = doubleKing ? ConsoleColor.Yellow : ConsoleColor.White;
                Console.Write(str);
            }
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
