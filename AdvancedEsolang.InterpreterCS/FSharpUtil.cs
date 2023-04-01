namespace AdvancedEsolang.InterpreterCS;

using Microsoft.FSharp.Core;

public static class FSharpUtil {
    public static T? ToNullable<T>(this FSharpOption<T> option) where T : class
    {
        return FSharpOption<T>.get_IsSome(option) ? option.Value : null;
    }
}