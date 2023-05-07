namespace AdvancedEsolang.InterpreterCS;

public static class ConversionUtil
{
    public static AdvObject ToAdvObject(this bool b) => new AdvObject(b ? BuiltinTypes.True : BuiltinTypes.False);
    public static AdvObject ToAdvObject(this string s) => new AdvString(s);
}