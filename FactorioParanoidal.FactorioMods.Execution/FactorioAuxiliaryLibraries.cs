using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Lua;
using Microsoft.Extensions.Logging;

namespace FactorioParanoidal.FactorioMods.Execution;

internal static class FactorioAuxiliaryLibraries {
    public static void Install(LuaState state, ILogger logger) {
        state.Environment["loadfile"] = LuaValue.Nil;
        state.Environment["dofile"] = LuaValue.Nil;
        state.Environment["coroutine"] = LuaValue.Nil;
        state.Environment["io"] = LuaValue.Nil;
        state.Environment["os"] = LuaValue.Nil;

        var debug = state.Environment[(LuaValue)"debug"].Read<LuaTable>();
        var restrictedDebug = new LuaTable();
        restrictedDebug["getinfo"] = debug["getinfo"];
        restrictedDebug["traceback"] = debug["traceback"];
        state.Environment["debug"] = restrictedDebug;

        state.Environment["log"] = CreateOutputFunction(logger, "Lua: {Message}");
        state.Environment["localised_print"] = CreateOutputFunction(logger, "Lua stdout: {Message}");
        state.Environment["table_size"] = new LuaFunction("table_size", (context, _) => {
            var table = context.GetArgument<LuaTable>(0);
            var count = 0;
            foreach (var entry in table) count++;
            return new ValueTask<int>(context.Return(count));
        });

        state.Environment["serpent"] = CreateSerpent();
        InstallRandom(state);
        InstallStringPack(state);
    }

    private static LuaFunction CreateOutputFunction(ILogger logger, string format) {
        return new LuaFunction(async (context, _) => {
            if (context.ArgumentCount > 0) logger.LogInformation(format, FormatLocalisedString(context.GetArgument(0)));
            return context.Return();
        });
    }

    private static string FormatLocalisedString(LuaValue value) {
        if (!value.TryRead<LuaTable>(out var table)) return value.ToString();

        var parts = new List<string>();
        foreach (var entry in table) parts.Add(FormatLocalisedString(entry.Value));
        return string.Concat(parts);
    }

    private static LuaTable CreateSerpent() {
        var serpent = new LuaTable();
        serpent["dump"] = new LuaFunction("serpent.dump", (context, _) =>
            new ValueTask<int>(context.Return(Serialize(context.GetArgument(0), compact: true))));
        serpent["line"] = new LuaFunction("serpent.line", (context, _) =>
            new ValueTask<int>(context.Return(Serialize(context.GetArgument(0), compact: true))));
        serpent["block"] = new LuaFunction("serpent.block", (context, _) =>
            new ValueTask<int>(context.Return(Serialize(context.GetArgument(0), compact: false))));
        return serpent;
    }

    private static string Serialize(LuaValue value, bool compact) {
        var seen = new HashSet<LuaTable>(ReferenceEqualityComparer.Instance);
        return SerializeValue(value, compact, 0, seen);
    }

    private static string SerializeValue(LuaValue value, bool compact, int level, HashSet<LuaTable> seen) {
        if (value == LuaValue.Nil) return "nil";
        if (value.TryRead<bool>(out var boolean)) return boolean ? "true" : "false";
        if (value.TryRead<double>(out var number)) return number.ToString("G17", CultureInfo.InvariantCulture);
        if (value.TryRead<string>(out var text)) return Quote(text);
        if (!value.TryRead<LuaTable>(out var table)) return Quote(value.ToString());
        if (!seen.Add(table)) return "nil --[[ref]]";

        var entries = new List<string>();
        var arrayIndex = 1;
        foreach (var entry in table) {
            var serializedValue = SerializeValue(entry.Value, compact, level + 1, seen);
            if (entry.Key.TryRead<double>(out var numericKey) && numericKey == arrayIndex) {
                entries.Add(serializedValue);
                arrayIndex++;
            }
            else {
                var key = entry.Key.TryRead<string>(out var stringKey) && IsIdentifier(stringKey)
                    ? stringKey
                    : $"[{SerializeValue(entry.Key, true, level + 1, seen)}]";
                entries.Add($"{key} = {serializedValue}");
            }
        }

        seen.Remove(table);
        if (compact) return $"{{{string.Join(", ", entries)}}}";
        if (entries.Count == 0) return "{}";
        var indent = new string(' ', (level + 1) * 2);
        return $"{{\n{indent}{string.Join($",\n{indent}", entries)}\n{new string(' ', level * 2)}}}";
    }

    private static bool IsIdentifier(string value) {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_')) return false;
        return value.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static string Quote(string value) {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value) {
            builder.Append(c switch {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c.ToString()
            });
        }

        return builder.Append('"').ToString();
    }

    private static void InstallRandom(LuaState state) {
        var random = new Random(0);
        var math = state.Environment[(LuaValue)"math"].Read<LuaTable>();
        math["randomseed"] = new LuaFunction("math.randomseed", (context, _) =>
            new ValueTask<int>(context.Return()));
        math["random"] = new LuaFunction("math.random", (context, _) => {
            if (context.ArgumentCount == 0) return new ValueTask<int>(context.Return(random.NextDouble()));

            var lower = context.ArgumentCount == 1 ? 1L : (long)Math.Floor(context.GetArgument<double>(0));
            var upperIndex = context.ArgumentCount == 1 ? 0 : 1;
            var upper = (long)Math.Floor(context.GetArgument<double>(upperIndex));
            if (lower > upper) throw new LuaRuntimeException(context.State, "interval is empty");
            return new ValueTask<int>(context.Return(random.NextInt64(lower, checked(upper + 1))));
        });
    }

    private static void InstallStringPack(LuaState state) {
        var strings = state.Environment[(LuaValue)"string"].Read<LuaTable>();
        strings["pack"] = new LuaFunction("string.pack", (context, _) => {
            var format = context.GetArgument<string>(0);
            var writer = new PackWriter(format);
            for (var i = 1; i < context.ArgumentCount; i++) writer.Write(context.GetArgument(i));
            return new ValueTask<int>(context.Return(writer.Finish()));
        });
        strings["packsize"] = new LuaFunction("string.packsize", (context, _) => {
            var writer = new PackWriter(context.GetArgument<string>(0));
            return new ValueTask<int>(context.Return(writer.GetFixedSize()));
        });
        strings["unpack"] = new LuaFunction("string.unpack", (context, _) => {
            var format = context.GetArgument<string>(0);
            var input = context.GetArgument<string>(1);
            var position = context.ArgumentCount > 2 ? context.GetArgument<int>(2) : 1;
            var values = PackReader.Read(format, input, position);
            return new ValueTask<int>(context.Return(values));
        });
    }

    private static PackOption ParseOption(char code, string format, ref int index) {
        var size = code switch {
            'b' or 'B' => 1,
            'h' or 'H' => 2,
            'l' or 'L' or 'f' => 4,
            'j' or 'J' or 'T' or 'd' or 'n' => 8,
            'i' or 'I' => ReadDigits(format, ref index, 4),
            's' => ReadDigits(format, ref index, 8),
            'c' => ReadDigits(format, ref index, -1),
            'z' or 'x' => code == 'x' ? 1 : 0,
            _ => throw new InvalidOperationException($"invalid format option '{code}'")
        };
        if (size < 0 || size > 8 && code is not 'c') throw new InvalidOperationException("invalid format size");
        return new PackOption(code, size);
    }

    private static int ReadDigits(string format, ref int index, int fallback) {
        var start = index;
        while (index < format.Length && char.IsAsciiDigit(format[index])) index++;
        return start == index ? fallback : int.Parse(format[start..index], CultureInfo.InvariantCulture);
    }

    private static byte[] ToBytes(string value) => value.Select(c => checked((byte)c)).ToArray();
    private static string FromBytes(IEnumerable<byte> value) => new(value.Select(b => (char)b).ToArray());

    private sealed class PackWriter {
        private readonly List<byte> _bytes = [];
        private readonly string _format;
        private int _formatIndex;
        private bool _littleEndian = BitConverter.IsLittleEndian;

        public PackWriter(string format) {
            _format = format;
        }

        public void Write(LuaValue value) {
            var option = NextOption();
            if (option.Code == 'c') {
                var bytes = ToBytes(value.Read<string>());
                if (bytes.Length != option.Size)
                    throw new InvalidOperationException("string length does not match pack format");
                _bytes.AddRange(bytes);
            }
            else if (option.Code == 'z') {
                var bytes = ToBytes(value.Read<string>());
                if (bytes.Contains((byte)0)) throw new InvalidOperationException("string contains zeros");
                _bytes.AddRange(bytes);
                _bytes.Add(0);
            }
            else if (option.Code == 's') {
                var bytes = ToBytes(value.Read<string>());
                WriteInteger((ulong)bytes.Length, option.Size);
                _bytes.AddRange(bytes);
            }
            else if (option.Code is 'f' or 'd' or 'n') {
                WriteFloat(value.Read<double>(), option.Size);
            }
            else {
                WriteInteger(unchecked((ulong)(long)Math.Floor(value.Read<double>())), option.Size);
            }
        }

        public string Finish() {
            while (TryNextOption(out var option)) {
                if (option.Code == 'x') _bytes.Add(0);
                else throw new InvalidOperationException("not enough values for pack format");
            }

            return FromBytes(_bytes);
        }

        public int GetFixedSize() {
            while (TryNextOption(out var option)) {
                if (option.Code is 'z' or 's') throw new InvalidOperationException("variable-length format");
                _bytes.AddRange(new byte[option.Size]);
            }

            return _bytes.Count;
        }

        private PackOption NextOption() {
            while (TryNextOption(out var option)) {
                if (option.Code == 'x') {
                    _bytes.Add(0);
                    continue;
                }

                return option;
            }

            throw new InvalidOperationException("too many values for pack format");
        }

        private bool TryNextOption(out PackOption option) {
            while (_formatIndex < _format.Length) {
                var code = _format[_formatIndex++];
                if (char.IsWhiteSpace(code)) continue;
                if (code == '<') {
                    _littleEndian = true;
                    continue;
                }

                if (code == '>') {
                    _littleEndian = false;
                    continue;
                }

                if (code == '=') {
                    _littleEndian = BitConverter.IsLittleEndian;
                    continue;
                }

                if (code == '!') {
                    ReadDigits(1);
                    continue;
                }

                option = ParseOption(code, _format, ref _formatIndex);
                return true;
            }

            option = default;
            return false;
        }

        private int ReadDigits(int fallback) =>
            FactorioAuxiliaryLibraries.ReadDigits(_format, ref _formatIndex, fallback);

        private void WriteInteger(ulong value, int size) {
            for (var i = 0; i < size; i++) {
                var shift = _littleEndian ? i * 8 : (size - i - 1) * 8;
                _bytes.Add((byte)(value >> shift));
            }
        }

        private void WriteFloat(double value, int size) {
            Span<byte> bytes = stackalloc byte[8];
            if (size == 4) BinaryPrimitives.WriteSingleLittleEndian(bytes, (float)value);
            else BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
            if (!_littleEndian) bytes[..size].Reverse();
            _bytes.AddRange(bytes[..size].ToArray());
        }
    }

    private static class PackReader {
        public static LuaValue[] Read(string format, string input, int luaPosition) {
            var bytes = ToBytes(input);
            var offset = luaPosition > 0 ? luaPosition - 1 : bytes.Length + luaPosition + 1;
            var formatIndex = 0;
            var littleEndian = BitConverter.IsLittleEndian;
            var values = new List<LuaValue>();
            while (formatIndex < format.Length) {
                var code = format[formatIndex++];
                if (char.IsWhiteSpace(code)) continue;
                if (code == '<') {
                    littleEndian = true;
                    continue;
                }

                if (code == '>') {
                    littleEndian = false;
                    continue;
                }

                if (code == '=') {
                    littleEndian = BitConverter.IsLittleEndian;
                    continue;
                }

                if (code == '!') {
                    ReadDigits(format, ref formatIndex, 1);
                    continue;
                }

                var option = ParseOption(code, format, ref formatIndex);
                if (code == 'x') {
                    offset++;
                    continue;
                }

                if (code == 'z') {
                    var end = Array.IndexOf(bytes, (byte)0, offset);
                    if (end < 0) throw new InvalidOperationException("unfinished string for format 'z'");
                    values.Add(FromBytes(bytes[offset..end]));
                    offset = end + 1;
                    continue;
                }

                if (code == 's') {
                    var length = checked((int)ReadInteger(bytes, ref offset, option.Size, littleEndian));
                    values.Add(FromBytes(bytes[offset..(offset + length)]));
                    offset += length;
                    continue;
                }

                if (offset + option.Size > bytes.Length) throw new InvalidOperationException("data string too short");
                if (code == 'c') values.Add(FromBytes(bytes[offset..(offset + option.Size)]));
                else if (code is 'f' or 'd' or 'n') values.Add(ReadFloat(bytes, offset, option.Size, littleEndian));
                else {
                    var integer = ReadInteger(bytes, ref offset, option.Size, littleEndian);
                    values.Add(IsSignedInteger(code) ? ToSignedDouble(integer, option.Size) : (double)integer);
                }

                if (code is 'c' or 'f' or 'd' or 'n') offset += option.Size;
            }

            values.Add(offset + 1);
            return [..values];
        }

        private static ulong ReadInteger(byte[] bytes, ref int offset, int size, bool littleEndian) {
            if (offset + size > bytes.Length) throw new InvalidOperationException("data string too short");
            ulong value = 0;
            for (var i = 0; i < size; i++) {
                var shift = littleEndian ? i * 8 : (size - i - 1) * 8;
                value |= (ulong)bytes[offset + i] << shift;
            }

            offset += size;
            return value;
        }

        private static double ReadFloat(byte[] bytes, int offset, int size, bool littleEndian) {
            Span<byte> value = stackalloc byte[8];
            bytes.AsSpan(offset, size).CopyTo(value);
            if (!littleEndian) value[..size].Reverse();
            return size == 4
                ? BinaryPrimitives.ReadSingleLittleEndian(value)
                : BinaryPrimitives.ReadDoubleLittleEndian(value);
        }

        private static bool IsSignedInteger(char code) => code is 'b' or 'h' or 'l' or 'j' or 'i';

        private static double ToSignedDouble(ulong value, int size) {
            if (size == 8) return unchecked((long)value);
            var bits = size * 8;
            var signBit = 1UL << (bits - 1);
            return (long)((value ^ signBit) - signBit);
        }
    }

    private readonly record struct PackOption(char Code, int Size);
}