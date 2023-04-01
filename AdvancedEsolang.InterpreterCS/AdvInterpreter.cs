namespace AdvancedEsolang.InterpreterCS;

using System;
using Microsoft.FSharp.Collections;
using AdvancedEsolang.Syntax;

public record BuiltinMethodCtx(AdvInterpreter Interpreter, AdvObject Self, AdvObject[] Args);
internal record BuiltinMethod(bool RequiresProgram, Func<BuiltinMethodCtx, AdvObject?> OnCall);

public class AdvInterpreter
{
    public readonly Library ProgramLib;
    public readonly AdvObject ProgramObj;

    private readonly Dictionary<(string, string), BuiltinMethod> builtinMethods =
        new Dictionary<(string, string), BuiltinMethod>();

    public AdvInterpreter(Library programLib)
    {
        var programClass = programLib.classes.FirstOrDefault(c => c.@is(BuiltinTypes.Program) && !c.isAbstract) ?? throw new Exception(); //TODO
        
        ProgramLib = programLib;
        ProgramObj = new AdvObject(programClass);
        
        InitBuiltinMethods();
    }

    public void AddBuiltinMethod((string, string) name, bool requiresProgram, Func<BuiltinMethodCtx, AdvObject?> onCall)
    {
        builtinMethods[name] = new BuiltinMethod(requiresProgram, onCall);
    }
    
    public void AddBuiltinMethod((string, string) name, bool requiresProgram, Action<BuiltinMethodCtx> onCall)
    {
        AddBuiltinMethod(name, requiresProgram, ctx =>
        {
            onCall(ctx);
            return null;
        });
    }

    private void InitBuiltinMethods()
    {
        AddBuiltinMethod(("Output", "write"), true, ctx =>
        {
            Console.WriteLine(ctx.Args[0]);
        });
        
        AddBuiltinMethod(("Input", "read"), true, ctx =>
        {
            Console.Write("> ");
            return Console.ReadLine()?.ToAdvObject();
        });
        
        AddBuiltinMethod(("String", "equals"), false, ctx =>
        {
            var str1 = (AdvString) ctx.Self;
            var str2 = (AdvString) ctx.Args[0]; //TODO: Type safety
        
            return (str1.Value == str2.Value).ToAdvObject();
        });
        
        //TODO: String length
        
        //TODO: Program error
        
        AddBuiltinMethod(("TypeUtils", "create"), false, ctx =>
        {
            var name = ((AdvString) ctx.Args[0]).Value; //TODO: Safety
            var type = ctx.Interpreter.ProgramLib.classDict.GetValueOrDefault(name) ?? throw new Exception(); //TODO
            
            return new AdvObject(type);
        });
        
        AddBuiltinMethod(("TypeUtils", "typeName"), false, ctx =>
        {
            return ctx.Args[0].Class.name.ToAdvObject();
        });
    }
    
    public void Run()
    {
        var mainMethod = ProgramObj.Class.get<Method>("main").ToNullable() ?? throw new Exception(); //TODO

        RunMethod(ProgramObj, mainMethod, Array.Empty<AdvObject>());
    }

    private AdvObject RunMethod(AdvObject self, Method method, AdvObject[] args)
    {
        var methodBody = method.body.ToNullable();

        if (methodBody == null) return HandleEmptyBody(self, method, args);

        var locals = new Dictionary<string, AdvObject>();

        for (int i = 0; i < args.Length; i++)
        {
            locals[method.parameters[i]] =
                args[i]; //TODO: Make sure that i is never out of bounds of the parameters list
        }

        AdvObject RunCall(Expression objExpr, string methodName, FSharpList<Expression> args)
        {
            var obj = RunExpression(objExpr);
            var method = obj.Class.get<Method>(methodName).ToNullable() ?? throw new Exception(); //TODO

            if (args.Length != method.parameters.Length) throw new Exception(); //TODO

            return RunMethod(obj, method, args.Select(RunExpression).ToArray());
        }

        AdvObject RunExpression(Expression expression)
        {
            return expression switch
            {
                Expression.Get { name: "this" } => self,
                Expression.Get { name: var name } when locals.ContainsKey(name) => locals[name],
                Expression.Get { name: var name } => new AdvObject(ProgramLib.classDict.GetValueOrDefault(name) ?? throw new Exception()), //TODO
                Expression.CallExpr { objExpr: var objExpr, methodName: var methodName, args: var args} => RunCall(objExpr, methodName, args),
                Expression.GetF { objExpr: var objExpr, fieldName: var fieldName } => RunExpression(objExpr).GetField(fieldName),
                Expression.Is { objExpr: var objExpr, className: var className } => RunExpression(objExpr).Class.@is(ProgramLib.classDict.GetValueOrDefault(className) ?? throw new Exception()).ToAdvObject(), //TODO
                Expression.Equals {objExpr1: var objExpr1, objExpr2: var objExpr2} => (RunExpression(objExpr1) == RunExpression(objExpr2)).ToAdvObject(),
                Expression.String { text: var text } => new AdvString(text)
            };
        }

        AdvObject? result = null;
        
        void RunStatements(FSharpList<Statement> statements)
        {
            foreach (var statement in statements)
            {
                if (result != null) return;
                
                switch (statement)
                {
                    case Statement.SetV { varName: var varName, value: var value }:
                        locals[varName] = RunExpression(value);
                        break;
                    case Statement.SetF { objExpr: var objExpr, fieldName: var fieldName, value: var value }:
                        RunExpression(objExpr).SetField(fieldName, RunExpression(value));
                        break;
                    case Statement.CallStmt { objExpr: var objExpr, methodName: var methodName, args: var callArgs }:
                        RunCall(objExpr, methodName, callArgs);
                        break;
                    case Statement.Return { result: var res }:
                        result = RunExpression(res);
                        break;
                    case Statement.If { condition: var condition, stmts: var stmts }:
                        if (RunExpression(condition).Class.@is(BuiltinTypes.True))
                        {
                            RunStatements(stmts);
                        }
                        break;
                    case Statement.While { condition: var condition, stmts: var stmts }:
                        while (RunExpression(condition).Class.@is(BuiltinTypes.True))
                        {
                            RunStatements(stmts);
                        }
                        break;
                }
            }   
        }
        
        RunStatements(methodBody);

        return result ?? AdvObject.Null;
    }

    private AdvObject HandleEmptyBody(AdvObject self, Method method, AdvObject[] args)
    {
        var definingClass = self.Class; //TODO

        if (builtinMethods.TryGetValue((definingClass.name, method.name), out var builtinMethod))
        {
            if (definingClass.name == "Program" && self != ProgramObj) throw new Exception(); //TODO
            if (builtinMethod.RequiresProgram && self.GetField("program") != ProgramObj) throw new Exception(); //TODO

            return builtinMethod.OnCall(new BuiltinMethodCtx(this, self, args)) ?? AdvObject.Null;
        }

        throw new Exception(); //TODO
    }
}