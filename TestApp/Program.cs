using static System.Console;
using static Input;

var app = ConsoleApp.Create(args);
app.AddCommands<TestApp>();
app.Run();

public class TestApp : ConsoleAppBase
{
    public void Create()
    {
        WriteLine($"開始 {DateTime.Now}");
        XDDatabase.CreateDB();
        WriteLine($"終わったよ {DateTime.Now}");
        ReadKey();
    }

    public void Search()
    {
        // 1回目パーティ入力
        WriteLine("ミュウツー, ミュウ, デオキシス, レックウザ, ジラーチ");
        WriteLine("フリーザー, サンダー, ファイヤー, ガルーラ, ラティアス");
        Write("パーティ(こちら あちら) >");
        InputLine(out int pIndex, out int eIndex);
        Write("HP(こちら あちら) >");
        var hp = new uint[4];
        InputLine(out hp[2], out hp[3], out hp[0], out hp[1]);

        // 2回目パーティ入力
        Write("パーティ(こちら あちら) >");
        InputLine(out int pIndex2, out int eIndex2);
        Write("HP(こちら あちら) >");
        var hp2 = new uint[4];
        InputLine(out hp2[2], out hp2[3], out hp2[0], out hp2[1]);

        var result = XDDatabase.SearchSeed(pIndex, eIndex, hp, pIndex2, eIndex2, hp2); // TSVがわかっているなら入力したほうがいいです。
        foreach (var seed in result)
            WriteLine(Convert.ToString(seed, 16));
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
