using System.Text;

namespace VpnClient.UI;

public static class QrCode
{
    private static readonly int[] EccPerBlock =
    {
        0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26,
        26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28
    };

    private static readonly int[] Blocks =
    {
        0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16,
        17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49
    };

    public static bool[,] Encode(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var version = 1;
        while (DataCodewords(version) * 8 < 4 + (version < 10 ? 8 : 16) + bytes.Length * 8)
        {
            if (++version > 40)
                throw new ArgumentException("Слишком длинный текст для QR-кода");
        }

        var codewords = AddEcc(version, DataBits(version, bytes));
        var size = version * 4 + 17;
        var modules = new bool[size, size];
        var reserved = new bool[size, size];
        DrawPatterns(version, modules, reserved);
        PlaceData(codewords, modules, reserved);

        var best = 0;
        var bestPenalty = int.MaxValue;
        for (var mask = 0; mask < 8; mask++)
        {
            ApplyMask(mask, modules, reserved);
            DrawFormat(mask, modules);
            var penalty = Penalty(modules);
            if (penalty < bestPenalty)
            {
                best = mask;
                bestPenalty = penalty;
            }
            ApplyMask(mask, modules, reserved);
        }

        ApplyMask(best, modules, reserved);
        DrawFormat(best, modules);
        return modules;
    }

    private static int RawModules(int version)
    {
        var result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            var align = version / 7 + 2;
            result -= (25 * align - 10) * align - 55;
            if (version >= 7)
                result -= 36;
        }
        return result;
    }

    private static int DataCodewords(int version) =>
        RawModules(version) / 8 - EccPerBlock[version] * Blocks[version];

    private static byte[] DataBits(int version, byte[] bytes)
    {
        var bits = new List<bool>();
        void Add(int value, int count)
        {
            for (var i = count - 1; i >= 0; i--)
                bits.Add(((value >> i) & 1) != 0);
        }

        Add(0b0100, 4);
        Add(bytes.Length, version < 10 ? 8 : 16);
        foreach (var b in bytes)
            Add(b, 8);

        var capacity = DataCodewords(version) * 8;
        Add(0, Math.Min(4, capacity - bits.Count));
        Add(0, (8 - bits.Count % 8) % 8);
        for (var pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11)
            Add(pad, 8);

        var result = new byte[bits.Count / 8];
        for (var i = 0; i < bits.Count; i++)
        {
            if (bits[i])
                result[i / 8] |= (byte)(0x80 >> (i % 8));
        }
        return result;
    }

    private static byte[] AddEcc(int version, byte[] data)
    {
        var blockCount = Blocks[version];
        var eccLength = EccPerBlock[version];
        var raw = RawModules(version) / 8;
        var shortBlocks = blockCount - raw % blockCount;
        var shortLength = raw / blockCount - eccLength;
        var divisor = Generator(eccLength);

        var dataBlocks = new List<byte[]>();
        var eccBlocks = new List<byte[]>();
        for (int i = 0, offset = 0; i < blockCount; i++)
        {
            var length = shortLength + (i < shortBlocks ? 0 : 1);
            var block = data.Skip(offset).Take(length).ToArray();
            offset += length;
            dataBlocks.Add(block);
            eccBlocks.Add(Remainder(block, divisor));
        }

        var result = new List<byte>();
        for (var i = 0; i <= shortLength; i++)
        {
            foreach (var block in dataBlocks)
            {
                if (i < block.Length)
                    result.Add(block[i]);
            }
        }
        for (var i = 0; i < eccLength; i++)
        {
            foreach (var block in eccBlocks)
                result.Add(block[i]);
        }
        return result.ToArray();
    }

    private static byte[] Generator(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        var root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                result[j] = Multiply(result[j], root);
                if (j + 1 < degree)
                    result[j] ^= result[j + 1];
            }
            root = Multiply(root, 2);
        }
        return result;
    }

    private static byte[] Remainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (var b in data)
        {
            var factor = b ^ result[0];
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[result.Length - 1] = 0;
            for (var i = 0; i < result.Length; i++)
                result[i] ^= Multiply(divisor[i], factor);
        }
        return result;
    }

    private static byte Multiply(int x, int y)
    {
        var z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }

    private static void DrawPatterns(int version, bool[,] modules, bool[,] reserved)
    {
        var size = modules.GetLength(0);
        void Set(int x, int y, bool dark)
        {
            modules[y, x] = dark;
            reserved[y, x] = true;
        }

        for (var i = 0; i < size; i++)
        {
            Set(6, i, i % 2 == 0);
            Set(i, 6, i % 2 == 0);
        }

        foreach (var (cx, cy) in new[] { (3, 3), (size - 4, 3), (3, size - 4) })
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                for (var dx = -4; dx <= 4; dx++)
                {
                    var x = cx + dx;
                    var y = cy + dy;
                    var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        Set(x, y, distance != 2 && distance != 4);
                }
            }
        }

        var positions = AlignmentPositions(version, size);
        for (var i = 0; i < positions.Length; i++)
        {
            for (var j = 0; j < positions.Length; j++)
            {
                var corner = (i == 0 && j == 0) || (i == 0 && j == positions.Length - 1) || (i == positions.Length - 1 && j == 0);
                if (corner)
                    continue;

                for (var dy = -2; dy <= 2; dy++)
                {
                    for (var dx = -2; dx <= 2; dx++)
                        Set(positions[i] + dx, positions[j] + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                }
            }
        }

        for (var i = 0; i < 9; i++)
        {
            reserved[8, i] = true;
            reserved[i, 8] = true;
        }
        for (var i = 0; i < 8; i++)
        {
            reserved[8, size - 1 - i] = true;
            reserved[size - 1 - i, 8] = true;
        }
        Set(8, size - 8, true);

        if (version < 7)
            return;

        var rem = version;
        for (var i = 0; i < 12; i++)
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        var bits = (version << 12) | rem;
        for (var i = 0; i < 18; i++)
        {
            var dark = ((bits >> i) & 1) != 0;
            var a = size - 11 + i % 3;
            var b = i / 3;
            Set(a, b, dark);
            Set(b, a, dark);
        }
    }

    private static int[] AlignmentPositions(int version, int size)
    {
        if (version == 1)
            return Array.Empty<int>();

        var count = version / 7 + 2;
        var step = (version * 8 + count * 3 + 5) / (count * 4 - 4) * 2;
        var result = new int[count];
        result[0] = 6;
        for (int i = count - 1, position = size - 7; i >= 1; i--, position -= step)
            result[i] = position;
        return result;
    }

    private static void PlaceData(byte[] codewords, bool[,] modules, bool[,] reserved)
    {
        var size = modules.GetLength(0);
        var bit = 0;
        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
                right = 5;

            for (var vertical = 0; vertical < size; vertical++)
            {
                for (var j = 0; j < 2; j++)
                {
                    var x = right - j;
                    var upward = ((right + 1) & 2) == 0;
                    var y = upward ? size - 1 - vertical : vertical;
                    if (reserved[y, x] || bit >= codewords.Length * 8)
                        continue;

                    modules[y, x] = ((codewords[bit / 8] >> (7 - bit % 8)) & 1) != 0;
                    bit++;
                }
            }
        }
    }

    private static void ApplyMask(int mask, bool[,] modules, bool[,] reserved)
    {
        var size = modules.GetLength(0);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (reserved[y, x])
                    continue;

                var invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    _ => ((x + y) % 2 + x * y % 3) % 2 == 0
                };
                if (invert)
                    modules[y, x] = !modules[y, x];
            }
        }
    }

    private static void DrawFormat(int mask, bool[,] modules)
    {
        var size = modules.GetLength(0);
        var data = mask;
        var rem = data;
        for (var i = 0; i < 10; i++)
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        var bits = ((data << 10) | rem) ^ 0x5412;
        bool Bit(int i) => ((bits >> i) & 1) != 0;

        for (var i = 0; i <= 5; i++)
            modules[i, 8] = Bit(i);
        modules[7, 8] = Bit(6);
        modules[8, 8] = Bit(7);
        modules[8, 7] = Bit(8);
        for (var i = 9; i < 15; i++)
            modules[8, 14 - i] = Bit(i);

        for (var i = 0; i < 8; i++)
            modules[8, size - 1 - i] = Bit(i);
        for (var i = 8; i < 15; i++)
            modules[size - 15 + i, 8] = Bit(i);
    }

    private static int Penalty(bool[,] modules)
    {
        var size = modules.GetLength(0);
        var penalty = 0;
        var dark = 0;

        for (var a = 0; a < size; a++)
        {
            int rowRun = 1, columnRun = 1;
            for (var b = 1; b < size; b++)
            {
                rowRun = modules[a, b] == modules[a, b - 1] ? rowRun + 1 : 1;
                columnRun = modules[b, a] == modules[b - 1, a] ? columnRun + 1 : 1;
                penalty += rowRun == 5 ? 3 : rowRun > 5 ? 1 : 0;
                penalty += columnRun == 5 ? 3 : columnRun > 5 ? 1 : 0;
            }
        }

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (modules[y, x])
                    dark++;
                if (x > 0 && y > 0 && modules[y, x] == modules[y - 1, x] &&
                    modules[y, x] == modules[y, x - 1] && modules[y, x] == modules[y - 1, x - 1])
                    penalty += 3;
            }
        }

        var total = size * size;
        penalty += Math.Abs(dark * 20 - total * 10) / total * 10;
        return penalty;
    }
}
