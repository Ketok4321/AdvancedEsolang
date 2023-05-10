namespace AdvancedEsolang.InterpreterCS;

public static class BuiltinMethods
{
    private static string GetString(BuiltinMethodCtx ctx, AdvObject obj)
    {
        if (obj is AdvString str) return str.Value;

        throw AdvException.CallInvalidArgument(ctx.Self.Class, ctx.Method.name);
    }
    
    private static int GetInt(BuiltinMethodCtx ctx, AdvObject obj)
    {
        if (int.TryParse(obj.Class.name, out var i)) return i;

        throw AdvException.CallInvalidArgument(ctx.Self.Class, ctx.Method.name);
    }
    
    public static void AddAll(AdvInterpreter i)
    {
        AddPrimitives(i);
        AddIO(i);
        AddMirror(i);
    }

    public static void AddPrimitives(AdvInterpreter i)
    {
        i.AddBuiltinMethod(("String", "equals"), false, ctx =>
        {
            var str1 = GetString(ctx, ctx.Self);
            var str2 = GetString(ctx, ctx.Args[0]);

            return (str1 == str2).ToAdvObject();
        });
        
        i.AddBuiltinMethod(("String", "+"), false, ctx =>
        {
            var str1 = GetString(ctx, ctx.Self);
            var str2 = GetString(ctx, ctx.Args[0]);

            return (str1 + str2).ToAdvObject();
        });
        
        i.AddBuiltinMethod(("String", "at"), false, ctx =>
        {
            var str = GetString(ctx, ctx.Self);
            var index = GetInt(ctx, ctx.Args[0]);

            if (index >= str.Length || index < 0) return null;
            
            return str[index].ToString().ToAdvObject();
        });
    }

    public static void AddIO(AdvInterpreter i)
    {
        i.AddBuiltinMethod(("Output", "write"), true, ctx => { Console.WriteLine(ctx.Args[0]); });

        i.AddBuiltinMethod(("Input", "read"), true, ctx =>
        {
            Console.Write("> ");
            return Console.ReadLine()?.ToAdvObject();
        });
    }
    
    public static void AddMirror(AdvInterpreter i)
    {
        i.AddBuiltinMethod(("Mirror", "instantiate"), false, ctx =>
        {
            var name = GetString(ctx, ctx.Args[0]);
            var type = ctx.Interpreter.Classes.GetValueOrDefault(name) ?? throw AdvException.NameNotFound(name);

            return new AdvObject(type);
        });

        i.AddBuiltinMethod(("Mirror", "typeName"), false, ctx => { return ctx.Args[0].Class.name.ToAdvObject(); });

        Class GetToReflect(BuiltinMethodCtx ctx)
        {
            var name = GetString(ctx,ctx.Self.GetField("toReflect"));
            return ctx.Interpreter.Classes.GetValueOrDefault(name) ?? throw AdvException.NameNotFound(name);
        }
        
        i.AddBuiltinMethod(("Mirror", "parent"), false, ctx =>
        {
            var type = GetToReflect(ctx);
            return type.parent.ToNullable()?.name.ToAdvObject();
        });
        
        i.AddBuiltinMethod(("Mirror", "isAbstract"), false, ctx =>
        {
            var type = GetToReflect(ctx);
            return type.isAbstract.ToAdvObject();
        });

        Func<BuiltinMethodCtx, AdvObject?> GetByIndex<T>(Func<Class, IReadOnlyList<T>> listProvider) where T : ClassMember
        {
            return ctx =>
            {
                var type = GetToReflect(ctx);
                var index = GetInt(ctx, ctx.Args[0]);

                var list = listProvider(type);

                if (index >= list.Count || index < 0) return null;
                return list[index].name.ToAdvObject();
            };
        }
        
        i.AddBuiltinMethod(("Mirror", "field"), false, GetByIndex(t => t.fields));
        i.AddBuiltinMethod(("Mirror", "method"), false, GetByIndex(t => t.methods));
    }
}