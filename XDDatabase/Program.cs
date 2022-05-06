using static System.Console;
using PokemonPRNG.LCG32.GCLCG;

public static class XDDatabase
{
    public static void CreateDB()
    {
        var seedList = PreAdvance.GetSeeds();
        var table = new List<(uint hp, uint seed)>();
        for (int i = 0; i < seedList.Length; i++)
        {
            SetCursorPosition(0, CursorTop);
            Write($"{i} / {seedList.Length}");

            var seed = seedList[i];
            var set = new HashSet<uint>();
            for (uint h = 0; h < 0x100; h++)
            {
                var hp = Generate((h << 24) | seed).HP;
                if (!set.Contains(hp))
                {
                    table.Add((hp, seed));
                    set.Add(hp);
                }
            }
        }

        WriteLine();
        WriteLine($"Sort {DateTime.Now}");

        table.Sort((a, b) => a.hp.CompareTo(b.hp));
        if (!Directory.Exists("./Result"))
            Directory.CreateDirectory("./Result");
        using (var bw = new BinaryWriter(new FileStream(@"./Result/XDDB.bin", FileMode.Create)))
        {
            foreach (var (hp, seed) in table)
            {
                bw.Write(hp);
                bw.Write(seed);
            }
        }
    }

    public static (uint HP, uint seed) Generate(uint seed)
    {
        seed.Advance(4); // PlaynerName + playerTeam + enemyTeam + ???

        var EnemyTSV = seed.GetRand() ^ seed.GetRand();

        seed.Advance(2);
        var h0 = seed.GetRand() & 0x1F;
        seed.Advance(2);
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
        h0 += seed.GenerateEVs() / 4;

        seed.Advance(2);
        var h1 = seed.GetRand() & 0x1F;
        seed.Advance(2);
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
        h1 += seed.GenerateEVs() / 4;

        seed.Advance(5);
        var h2 = seed.GetRand() & 0x1F;
        seed.Advance(4); // 色回避は考えない、いいね？
        h2 += seed.GenerateEVs() / 4;

        seed.Advance(2);
        var h3 = seed.GetRand() & 0x1F;
        seed.Advance(4); // 色回避は考えない、いいね？
        h3 += seed.GenerateEVs() / 4;

        return ((h0 << 24) | (h1 << 16) | (h2 << 8) | (h3), seed);
    }

    static List<(uint hp, uint seed)> LoadDB()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        var stream = asm.GetManifestResourceStream("XDDB.bin");
        if (stream == null) throw new Exception();

        var seedList = new List<(uint, uint)>();
        using (var br = new BinaryReader(stream))
        {
            var bs = br.BaseStream;
            while (bs.Position != bs.Length)
            {
                seedList.Add((br.ReadUInt32(), br.ReadUInt32()));
            }
        }
        WriteLine($"{seedList.Count}個のseedを読み込みました.");

        return seedList;
    }
    public static List<uint> SearchSeed(int pIndex, int eIndex, uint[] hp, int pIndex2, int eIndex2, uint[] hp2, uint playersTSV = 0x10000)
    {
        var database = LoadDB();
        var result = new List<uint>();

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

        hp[0] -= eBaseHP[eIndex].First;
        hp[1] -= eBaseHP[eIndex].Second;
        hp[2] -= pBaseHP[pIndex].First;
        hp[3] -= pBaseHP[pIndex].Second;

        var key = (hp[0] << 24) + (hp[1] << 16) + (hp[2] << 8) + (hp[3]);
        var idx = database.Select(_ => _.hp).ToList().BinarySearch(key);
        if (idx < 0)
        {
            WriteLine("見つかりませんでした.");
            return result;
        }

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
            for (uint h8 = 0; h8 < 0x100; h8++)
            {
                var res = GenerateQuickBattle(h8 << 24 | seed, playersTSV);
                var next = GenerateQuickBattle(res.seed, playersTSV);
                if (res.HP == key && res.pIndex == pIndex && res.eIndex == eIndex)
                    if (next.HP == key2 && next.pIndex == pIndex2 && next.eIndex == eIndex2)
                    {
                        WriteLine($"{h8 << 24 | seed:X8} {next.seed:X8}");
                        result.Add(next.seed);
                    }

                // 『単一のseed(24bit)に対し、上位8bitだけ異なるが、Generateを通すと同じseedに帰着する』という例が存在するため、この検索処理では不完全。
                // (検索結果の重複がごくごくまれに発生してしまう)
                // デオキシス サンダー 257 648 326 281
                // ジラーチ サンダー 349 325 336 313
                // ↑ の結果、0xF03F7EC1が重複して出力される。

                // また、XDDB.binはプレイヤー側のPID再計算が発生することを考慮していないので、たま～に結果が出ないことがあります。
            }
        }
        return result;
    }

    public static (uint pIndex, uint eIndex, uint HP, uint seed) GenerateQuickBattle(uint seed, uint pTsv = 0x10000)
    {
        seed.Advance(); // PlaynerName
        var playerTeamIndex = seed.GetRand() % 5;
        var enemyTeamIndex = seed.GetRand() % 5;

        var hp = new uint[4];

        seed.Advance();
        uint EnemyTSV = seed.GetRand() ^ seed.GetRand();

        // 相手1匹目
        seed.Advance();
        seed.Advance();
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
        seed.Advance();
        seed.Advance();

        // プレイヤー1匹目
        seed.Advance();
        seed.Advance();
        hp[2] = seed.GetRand() & 0x1F;
        seed.Advance();
        seed.Advance();
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ pTsv) >= 8) break; }
        hp[2] += seed.GenerateEVs() / 4;

        // プレイヤー2匹目
        seed.Advance();
        seed.Advance();
        hp[3] = seed.GetRand() & 0x1F;
        seed.Advance();
        seed.Advance();
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ pTsv) >= 8) break; }
        hp[3] += seed.GenerateEVs() / 4;

        return (playerTeamIndex, enemyTeamIndex, (hp[0] << 24) + (hp[1] << 16) + (hp[2] << 8) + (hp[3]), seed);
    }
}

static class EVsExt
{
    public static void Shave(this byte[] evs)
    {
        var sum = evs.Sum(_ => _);

        var k = 0;
        while (sum > 510)
        {
            if (evs[k] != 0) { evs[k]--; sum--; }
            if (++k == 6) k = 0;
        }
    }
    public static void Fill(this byte[] evs)
    {
        var sum = evs.Sum(_ => _);

        var k = 0;
        while (sum < 510)
        {
            if (evs[k] < 255) { evs[k]++; sum++; }
            if (++k == 6) k = 0;
        }
    }

    private static readonly uint[] evsCache = new uint[0x1000000];

    public static void GetSample(uint i)
    {
        WriteLine($"{evsCache[i] & 0xFFFFFF}");
        WriteLine($"{evsCache[i + 10] & 0xFFFFFF}");
        WriteLine($"{evsCache[i + 30] & 0xFFFFFF}");
    }

    // 努力値生成処理を通した後のseedを返します。
    public static uint AdvanceEVs(this uint seed)
    {
        var ini = seed;

        if (evsCache[seed & 0xFFFFFF] != 0) return ini.NextSeed(evsCache[seed & 0xFFFFFF] & 0xFFFFFF);

        var seeds = new List<(uint ini, uint diff)>() { (seed & 0xFFFFFF, 0) };

        var evs = new byte[6];
        int sumEV = 0;
        for (uint i = 0; i < 101; i++)
        {
            for (int j = 0; j < 6; j++) evs[j] += (byte)(seed.GetRand() & 0xFF);
            sumEV = evs.Sum(_ => _);

            if (sumEV == 510)
            {
                var index = seed.GetIndex(ini);
                foreach (var (s, diff) in seeds)
                    evsCache[s] = (index - diff) | ((uint)evs[0] << 24);

                return seed;
            }

            else if (sumEV <= 490) continue;

            else if (sumEV < 530)
            {
                evs.Fill();
                evs.Shave();

                var index = seed.GetIndex(ini);
                foreach (var (s, diff) in seeds)
                    evsCache[s] = (index - diff) | ((uint)evs[0] << 24);

                return seed;
            }

            else if (i != 100)
            {
                Array.Clear(evs, 0, 6);
                seeds.Add((seed & 0xFFFFFF, (i + 1) * 6u));
            }
        }

        evs.Fill();
        evs.Shave();

        evsCache[ini & 0xFFFFFF] = seed.GetIndex(ini) | ((uint)evs[0] << 24);
        return seed;
    }

    // 努力値生成処理を通し、HPの努力値を返します。
    public static uint GenerateEVs(this ref uint seed)
    {
        var ini = seed;

        if (evsCache[seed & 0xFFFFFF] != 0)
        {
            var val = evsCache[seed & 0xFFFFFF];
            seed.Advance(val & 0xFFFFFF);
            return val >> 24;
        }

        var seeds = new List<(uint ini, uint diff)>() { (seed & 0xFFFFFF, 0) };

        var evs = new byte[6];
        int sumEV = 0;
        for (uint i = 0; i < 101; i++)
        {
            for (int j = 0; j < 6; j++) evs[j] += (byte)(seed.GetRand() & 0xFF);
            sumEV = evs.Sum(_ => _);

            if (sumEV == 510)
            {
                var index = seed.GetIndex(ini);
                foreach (var (s, diff) in seeds)
                    evsCache[s] = (index - diff) | ((uint)evs[0] << 24);

                return evs[0];
            }

            else if (sumEV <= 490) continue;

            else if (sumEV < 530)
            {
                evs.Fill();
                evs.Shave();
                var index = seed.GetIndex(ini);
                foreach (var (s, diff) in seeds)
                    evsCache[s] = (index - diff) | ((uint)evs[0] << 24);

                return evs[0];
            }

            else if (i != 100)
            {
                Array.Clear(evs, 0, 6);
                seeds.Add((seed & 0xFFFFFF, (i + 1) * 6u));
            }
        }

        evs.Fill();
        evs.Shave();

        evsCache[ini & 0xFFFFFF] = seed.GetIndex(ini) | ((uint)evs[0] << 24);
        return evs[0];
    }
}

static class PreAdvance
{
    private static void AdvanceStep1(uint seed, HashSet<uint> set, HashSet<uint> setInit)
    {
        var init = seed;
        seed.Advance(4);
        var enemyTSV = seed.GetRand() ^ seed.GetRand();

        // 相手1匹目
        seed = seed * 0x284A930Du + 0xA2974C77u; // advance 5
        {
            var psv = seed.GetRand() ^ seed.GetRand();
            AdvanceStep2(seed, init, enemyTSV, set, setInit);
            if (((psv ^ enemyTSV) & 0xFF) < 8)
            {
                psv = seed.GetRand() ^ seed.GetRand();
                AdvanceStep2(seed, init, enemyTSV, set, setInit);
                if (((psv ^ enemyTSV) & 0xFF) < 8)
                {
                    psv = seed.GetRand() ^ seed.GetRand();
                    AdvanceStep2(seed, init, enemyTSV, set, setInit);
                }
            }
        }
    }
    private static void AdvanceStep2(uint seed, uint init, uint enemyTSV, HashSet<uint> set, HashSet<uint> setInit)
    {
        seed = seed.AdvanceEVs();

        // 相手2匹目
        seed = seed * 0x284A930Du + 0xA2974C77u; // advance 5
        {
            var psv = seed.GetRand() ^ seed.GetRand();
            AdvanceStep3(seed, init, set, setInit);
            if (((psv ^ enemyTSV) & 0xFF) < 8)
            {
                psv = seed.GetRand() ^ seed.GetRand();
                AdvanceStep3(seed, init, set, setInit);
                if (((psv ^ enemyTSV) & 0xFF) < 8)
                {
                    psv = seed.GetRand() ^ seed.GetRand();
                    AdvanceStep3(seed, init, set, setInit);
                }
            }
        }
    }
    private static void AdvanceStep3(uint seed, uint init, HashSet<uint> set, HashSet<uint> setInit)
    {
        seed = seed.AdvanceEVs();

        seed = seed * 0x67FBEEA9u + 0x77948382u; // advance 3 + 7
        seed = seed.AdvanceEVs();

        seed = seed * 0x0C287375u + 0x20AD96A9u; // advance 7
        seed = seed.AdvanceEVs();

        if (!set.Contains(seed & 0xFFFFFF))
            setInit.Add(init);
    }

    private static void AdvanceSimple(uint seed, HashSet<uint> set)
    {
        seed = seed * 0x284A930Du + 0xA2974C77u; // advance 5

        // 相手1匹目
        seed = seed * 0x0C287375u + 0x20AD96A9u; // advance 7
        seed = seed.AdvanceEVs();

        // 相手2匹目
        seed = seed * 0x0C287375u + 0x20AD96A9u; // advance 7
        seed = seed.AdvanceEVs();

        seed = seed * 0x67FBEEA9u + 0x77948382u; // advance 3 + 7
        seed = seed.AdvanceEVs();

        seed = seed * 0x0C287375u + 0x20AD96A9u; // advance 7
        seed = seed.AdvanceEVs();

        seed &= 0xFFFFFF;
        set.Add(seed);
    }

    private static uint AdvanceStrict(uint seed)
    {
        seed.Advance(4);
        var enemyTSV = seed.GetRand() ^ seed.GetRand();

        // 相手1匹目
        seed.Advance(5);
        while (true) { if ((enemyTSV ^ seed.GetRand() ^ seed.GetRand()) >= 8) break; }
        seed = seed.AdvanceEVs();

        // 相手2匹目
        seed.Advance(5);
        while (true) { if ((enemyTSV ^ seed.GetRand() ^ seed.GetRand()) >= 8) break; }
        seed = seed.AdvanceEVs();

        seed = seed * 0x67FBEEA9u + 0x77948382u; // advance 3 + 7
        seed = seed.AdvanceEVs();

        seed = seed * 0x0C287375u + 0x20AD96A9u; // advance 7
        seed = seed.AdvanceEVs();

        return seed;
    }

    public static uint[] GetSeeds()
    {
        // 敵トレーナー側の色回避処理が発生しないと仮定して計算する。
        var set = new HashSet<uint>();
        {
            for (uint seed = 0; seed < 0x1000000; seed++)
            {
                AdvanceSimple(seed, set);
            }
        }

        // 上位8bit次第で色回避が発生する可能性のある初期seedのうち、
        // 終了時のseedが ↑ の結果に含まれないようなものだけ抜き出す。
        var list = new HashSet<uint>();
        {
            for (uint seed = 0; seed < 0x1000000; seed++)
            {
                AdvanceStep1(seed, set, list);
            }
        }

        // 色回避が発生する可能性のある下位24bitに上位8bitを補って全探索して付け加える。
        foreach (var u24 in list)
        {
            for (uint h8 = 0x0; h8 < 0x100; h8++)
            {
                var seed = (h8 << 24) | u24;

                var res = AdvanceStrict(seed) & 0xFFFFFF;
                set.Add(res);
            }
        }

        WriteLine($"パーティ生成後の候補: {set.Count}");

        return set.ToArray();
    }
}
