using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using static XDDatabase.Input;

namespace XDDatabase
{
    static class Program
    {
        static void Main(string[] args)
        {
            // MakeDB();
            // Load().CreateDB();
            while (true)
            {
                LoadDB().SearchSeed();
            }
            // Console.WriteLine($"終わったよ {DateTime.Now}");
            // Console.ReadKey();
        }
        static void MakeDB()
        {
            Console.Write("上書きとか発生するけどええんか？ (承諾するならy+Enter)> ");
            if (Console.ReadLine() != "y") return;
            MakeSeedDB();
            Console.WriteLine($"計算終了...重複の除去に進みます... {DateTime.Now}");
            Distinct();
        }
        static void MakeSeedDB()
        {
            var fs = Enumerable.Range(0, 0x100).Select(_ => new BinaryWriter(new FileStream($"./Data/{_:X}.bin", FileMode.Create))).ToArray();
            Parallel.For(0, 0x1000000, low24 =>
            {
                var seedList = Enumerable.Range(0, 0x100).Select(_ => Generate(((uint)_ << 24) | (uint)low24).seed & 0xFFFFFF).Distinct();
                foreach (var seed in seedList)
                {
                    var key = seed >> 16;
                    ushort data = (ushort)(seed & 0xFFFF);
                    lock (fs[key]) { fs[key].Write(data); }
                }
            });
            foreach (var s in fs) s.Dispose();
        }
        static void Distinct()
        {
            for(uint i=0x0; i<0x100; i++)
            {
                Console.WriteLine(i);
                using(var br = new BinaryReader(new FileStream($"./Data/{i:X}.bin", FileMode.Open)))
                using (var wfs = new BinaryWriter(new FileStream($"./Distinct/{i:X}.bin", FileMode.Create)))
                {
                    var bs = br.BaseStream;
                    var seedList = new List<uint>();
                    while(bs.Position != bs.Length)
                    {
                        uint seed = (i << 16) | br.ReadUInt16();
                        seedList.Add(seed);
                    }
                    foreach(var seed in seedList.Distinct())
                    {
                        wfs.Write((ushort)(seed & 0xFFFF));
                    }
                }
            }
        }

        static List<uint> LoadData()
        {
            var seedList = new List<uint>();
            for (uint i = 0x0; i < 0x100; i++)
            {
                using (var br = new BinaryReader(new FileStream($"./Distinct/{i:X}.bin", FileMode.Open)))
                {
                    var bs = br.BaseStream;
                    while (bs.Position != bs.Length)
                    {
                        uint seed = (i << 16) | br.ReadUInt16();
                        seedList.Add(seed);
                    }
                }
            }
            Console.WriteLine($"{seedList.Count}個のseedを読み込みました.");

            return seedList;
        }
        static void CreateDB(this List<uint> seedList)
        {
            var table = new List<(uint hp, uint seed)>();
            for (int i = 0; i < seedList.Count; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{i} / {seedList.Count}");
                uint seed = seedList[i];
                var temp = new List<uint>();
                for (uint h = 0; h < 0x100; h++)
                {
                    temp.Add(Generate((h << 24) | seed).HP);
                }
                foreach (var hp in temp.Distinct())
                {
                    table.Add((hp, seed));
                }
            }
            table.Sort((a, b) => a.hp.CompareTo(b.hp));
            using (var bw = new BinaryWriter(new FileStream(@"./Completed/XDDB.bin", FileMode.Create)))
            {
                foreach(var item in table)
                {
                    bw.Write(item.hp);
                    bw.Write(item.seed);
                }
            }
        }

        static List<(uint hp, uint seed)> LoadDB()
        {
            var seedList = new List<(uint, uint)>();
            using (var br = new BinaryReader(new FileStream($"./Completed/XDDB.bin", FileMode.Open)))
            {
                var bs = br.BaseStream;
                while (bs.Position != bs.Length)
                {
                    seedList.Add((br.ReadUInt32(), br.ReadUInt32()));
                }
            }
            Console.WriteLine($"{seedList.Count}個のseedを読み込みました.");

            return seedList;
        } 
        static void SearchSeed(this List<(uint hp, uint seed)> database)
        {
            var pBaseHP = new (uint First, uint Second)[]
            {
                (322, 340),
                (310, 290),
                (210, 620),
                (320, 230),
                (310, 310),
            };
            var eBaseHP = new (uint First, uint Second)[]
            {
                (290, 310),
                (290, 270),
                (290, 250),
                (320, 270),
                (270, 230),
            };
            WriteLine("ミュウツー, ミュウ, デオキシス, レックウザ, ジラーチ");
            WriteLine("フリーザー, サンダー, ファイヤー, ガルーラ, ラティアス");
            Write("パーティ(こちら あちら) >");
            InputLine(out int pIndex, out int eIndex);
            Write("HP(こちら あちら) >");
            var hp = new uint[4];
            InputLine(out hp[2], out hp[3], out hp[0], out hp[1]);

            hp[0] -= eBaseHP[eIndex].First;
            hp[1] -= eBaseHP[eIndex].Second;
            hp[2] -= pBaseHP[pIndex].First;
            hp[3] -= pBaseHP[pIndex].Second;

            var key = (hp[0] << 24) + (hp[1] << 16) + (hp[2] << 8) + (hp[3]);
            var idx = database.Select(_ => _.hp).ToList().BinarySearch(key);
            if (idx < 0)
            {
                WriteLine("見つかりませんでした.");
                return;
            }

            Write("パーティ(こちら あちら) >");
            InputLine(out int pIndex2, out int eIndex2);
            Write("HP(こちら あちら) >");
            var hp2 = new uint[4];
            InputLine(out hp2[2], out hp2[3], out hp2[0], out hp2[1]);

            hp2[0] -= eBaseHP[eIndex2].First;
            hp2[1] -= eBaseHP[eIndex2].Second;
            hp2[2] -= pBaseHP[pIndex2].First;
            hp2[3] -= pBaseHP[pIndex2].Second;

            var key2 = (hp2[0] << 24) + (hp2[1] << 16) + (hp2[2] << 8) + (hp2[3]);
            WriteLine($"{key2:X}");

            var seedList = new List<uint>();
            for (int i = idx; database[i].hp == key && i >= 0; i--) seedList.Add(database[i].seed);
            for (int i = idx + 1; database[i].hp == key && i < database.Count; i++) seedList.Add(database[i].seed);

            foreach (var seed in seedList)
            {
                for(uint h8 = 0; h8<0x100; h8++)
                {
                    var res = Generate(h8 << 24 | seed);
                    var next = Generate(res.seed);
                    if (res.HP == key && res.pIndex == pIndex && res.eIndex == eIndex)
                    if (next.HP == key2 && next.pIndex == pIndex2 && next.eIndex == eIndex2)
                            WriteLine($"{h8 << 24 | seed:X8} {next.seed:X8}");

                    // 『単一のseed(24bit)に対し、上位8bitだけ異なるが、Generateを通すと同じseedに帰着する』という例が存在するため、この検索処理では不完全。
                    // (検索結果の重複がごくごくまれに発生してしまう)
                    // デオキシス サンダー 257 648 326 281
                    // ジラーチ サンダー 349 325 336 313
                    // ↑ の結果、0xF03F7EC1が重複して出力される。
                }
            }
        }

        static uint Advance(ref this uint seed)
        {
            return (seed = seed * 0x343FD + 0x269EC3);
        }
        static uint GetRand(ref this uint seed)
        {
            return (seed = seed * 0x343FD + 0x269EC3) >> 16;
        }
        public static (uint pIndex, uint eIndex, uint HP, uint seed) Generate(uint seed)
        {
            seed.Advance(); // PlaynerName
            var playerTeamIndex = seed.GetRand() % 5;
            var enemyTeamIndex = seed.GetRand() % 5;

            var hp = new uint[4];

            seed.Advance(); 
            uint EnemyTSV = seed.GetRand() ^ seed.GetRand();

            // 相手1匹目
            seed.Advance(); // dummyPID
            seed.Advance(); // dummyPID
            hp[0] = seed.GetRand() & 0x1F;
            seed.Advance(); // SCD
            seed.Advance(); // Ability
            while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
            hp[0] += seed.GenerateEVs() / 4;

            // 相手2匹目
            seed.Advance();
            seed.Advance();
            hp[1] = seed.GetRand() & 0x1F;
            seed.Advance();
            seed.Advance();
            while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
            hp[1] += seed.GenerateEVs() / 4;

            seed.Advance(); 
            uint PlayerTSV = seed.GetRand() ^ seed.GetRand();

            // プレイヤー1匹目
            seed.Advance();
            seed.Advance();
            hp[2] = seed.GetRand() & 0x1F;
            seed.Advance();
            seed.Advance();
            while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ PlayerTSV) >= 8) break; }
            hp[2] += seed.GenerateEVs() / 4;

            // プレイヤー2匹目
            seed.Advance();
            seed.Advance();
            hp[3] = seed.GetRand() & 0x1F;
            seed.Advance();
            seed.Advance();
            while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ PlayerTSV) >= 8) break; }
            hp[3] += seed.GenerateEVs() / 4;

            return (playerTeamIndex, enemyTeamIndex, (hp[0]<<24) + (hp[1] << 16) + (hp[2] << 8) + (hp[3]), seed);
        }
        private static uint GenerateEVs(ref this uint seed)
        {
            var EVs = new byte[6];
            int sumEV = 0;
            for (var i = 0; i < 101; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    byte ev = (byte)(seed.GetRand() & 0xFF);
                    EVs[j] += ev;
                }
                sumEV = EVs.Sum(_ => _);

                if (sumEV == 510) return EVs[0];
                if (sumEV <= 490) continue;
                if (sumEV < 530) break;
                if (i != 100) EVs = new byte[6];
            }
            var k = 0;
            while (sumEV != 510)
            {
                if (sumEV < 510 && EVs[k] < 255) { EVs[k]++; sumEV++; }
                if (sumEV > 510 && EVs[k] != 0) { EVs[k]--; sumEV--; }
                k = (k + 1) % 6;
            }
            return EVs[0];
        }
    }
    static class Input
    {
        public static void InputLine(out int a, out int b) { var input = ScanIntArray(); a = input[0]; b = input[1]; }
        public static void InputLine(out int a, out int b, out int c) { var input = ScanIntArray(); a = input[0]; b = input[1]; c = input[2]; }
        public static void InputLine(out int a, out int b, out int c, out int d) { var input = ScanIntArray(); a = input[0]; b = input[1]; c = input[2]; d = input[3]; }
        public static void InputLine(out uint a, out uint b, out uint c, out uint d) { var input = ScanUintArray(); a = input[0]; b = input[1]; c = input[2]; d = input[3]; }

        public static void InputLine(out long a, out long b) { var input = ScanLongArray(); a = input[0]; b = input[1]; }
        public static void InputLine(out long a, out long b, out long c) { var input = ScanLongArray(); a = input[0]; b = input[1]; c = input[2]; }
        public static void InputLine(out long a, out long b, out long c, out long d) { var input = ScanLongArray(); a = input[0]; b = input[1]; c = input[2]; d = input[3]; }

        public static void InputLine(out string a, out string b) { var input = ScanStrArray(); a = input[0]; b = input[1]; }

        public static int ScanInt() { return int.Parse(ReadLine()); }
        static public uint ScanUint() { return uint.Parse(ReadLine()); }
        static public double ScanDouble() { return double.Parse(ReadLine()); }
        static public long ScanLong() { return long.Parse(ReadLine()); }
        static public ulong ScanUlong() { return ulong.Parse(ReadLine()); }
        static public int[] ScanIntArray() { return ReadLine().Split().Select(x => int.Parse(x)).ToArray(); }
        static public uint[] ScanUintArray() { return ReadLine().Split().Select(x => uint.Parse(x)).ToArray(); }
        static public double[] ScanDoubleArray() { return ReadLine().Split().Select(x => double.Parse(x)).ToArray(); }
        static public long[] ScanLongArray() { return ReadLine().Split().Select(x => long.Parse(x)).ToArray(); }
        static public ulong[] ScanUlongArray() { return ReadLine().Split().Select(x => ulong.Parse(x)).ToArray(); }

        static public string ScanStr() { return ReadLine(); }
        static public string[] ScanStrArray() { return ScanStr().Split(); }
    }
}
