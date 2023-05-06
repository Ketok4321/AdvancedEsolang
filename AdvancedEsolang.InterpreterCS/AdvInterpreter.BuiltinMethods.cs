using AdvancedEsolang.Syntax;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace AdvancedEsolang.InterpreterCS;

public sealed partial class AdvInterpreter
{
    private void InitBuiltinMethods()
    {
        InitPrimitives();
        InitIO();
        InitMirror();
    }

    private void InitPrimitives()
    {
        AddBuiltinMethod(("String", "equals"), false, ctx =>
        {
            var str1 = (AdvString) ctx.Self;
            var str2 = (AdvString) ctx.Args[0]; //TODO: Type safety

            return (str1.Value == str2.Value).ToAdvObject();
        });

        //TODO: String length
    }

    private void InitIO()
    {
        AddBuiltinMethod(("Output", "write"), true, ctx => { Console.WriteLine(ctx.Args[0]); });

        AddBuiltinMethod(("Input", "read"), true, ctx =>
        {
            Console.Write("> ");
            return Console.ReadLine()?.ToAdvObject();
        });
    }
    
    private void InitMirror()
    {
        AddBuiltinMethod(("Mirror", "instantiate"), false, ctx =>
        {
            var name = ((AdvString) ctx.Args[0]).Value; //TODO: Safety
            var type = ctx.Interpreter.Classes.GetValueOrDefault(name) ?? throw AdvException.NameNotFound(name);

            return new AdvObject(type);
        });

        AddBuiltinMethod(("Mirror", "typeName"), false, ctx => { return ctx.Args[0].Class.name.ToAdvObject(); });

        Class GetToReflect(BuiltinMethodCtx ctx)
        {
            var name = ((AdvString) ctx.Self.GetField("toReflect")).Value; //TODO: Safety
            return ctx.Interpreter.Classes.GetValueOrDefault(name) ?? throw AdvException.NameNotFound(name);
        }
        
        AddBuiltinMethod(("Mirror", "parent"), false, ctx =>
        {
            var type = GetToReflect(ctx);
            return type.parent.ToNullable()?.name.ToAdvObject();
        });
        
        AddBuiltinMethod(("Mirror", "isAbstract"), false, ctx =>
        {
            var type = GetToReflect(ctx);
            return type.isAbstract.ToAdvObject();
        });

        Func<BuiltinMethodCtx, AdvObject?> GetByIndex<T>(Func<Class, IReadOnlyList<T>> listProvider) where T : ClassMember
        {
            return ctx =>
            {
                var type = GetToReflect(ctx);
                var index = int.Parse(ctx.Args[0].Class.name); //TODO: Safety

                var list = listProvider(type);

                if (index >= list.Count || index < 0) return null;
                return list[index].name.ToAdvObject();
            };
        }
        
        AddBuiltinMethod(("Mirror", "field"), false, GetByIndex(t => t.fields));
        AddBuiltinMethod(("Mirror", "method"), false, GetByIndex(t => t.methods));
    }
}