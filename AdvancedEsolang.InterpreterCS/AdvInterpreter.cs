﻿namespace AdvancedEsolang.InterpreterCS;

public record BuiltinMethodCtx(AdvInterpreter Interpreter, AdvObject Self, AdvObject[] Args, Method Method);
internal record BuiltinMethod(bool RequiresProgram, Func<BuiltinMethodCtx, AdvObject?> OnCall);

public sealed class AdvInterpreter
{
    public readonly Library ProgramLib;
    public readonly AdvObject ProgramObj;

    public readonly IReadOnlyDictionary<string, Class> Classes;

    public readonly Func<string, IEnumerable<Statement>>? EvalParser;

    private readonly Dictionary<(string, string), BuiltinMethod> builtinMethods =
        new Dictionary<(string, string), BuiltinMethod>();

    public static IEnumerable<string> GetPrograms(Library lib)
    {
        return lib.classes.Where(c => c.@is(BuiltinTypes.Program) && !c.isAbstract).Select(c => c.name);
    }
    
    public AdvInterpreter(Library programLib, string programClass, Func<string, IEnumerable<Statement>>? evalParser = null)
    {
        ProgramLib = programLib;
        ProgramObj = new AdvObject(programLib.getClass(programClass).ToNullable()!);
        Classes = ProgramLib.classDict;
        EvalParser = evalParser;
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

    public void Run()
    {
        var mainMethod = ProgramObj.Class.get<Method>("main").ToNullable()!; // This should never return null, because method 'main' is already defined in the parent class

        RunMethod(ProgramObj, mainMethod, Array.Empty<AdvObject>());
    }

    public AdvObject RunMethod(AdvObject self, Method method, AdvObject[] args)
    {
        var methodBody = method.body.ToNullable();

        if (methodBody == null) return HandleEmptyBody(self, method, args);

        var locals = new Dictionary<string, AdvObject>();

        for (int i = 0; i < args.Length; i++)
        {
            locals[method.parameters[i]] = args[i];
        }

        AdvObject RunCall(Expression objExpr, string methodName, FSharpList<Expression> args)
        {
            var obj = RunExpression(objExpr);
            var method = obj.Class.get<Method>(methodName).ToNullable() ?? throw AdvException.CallUndefined(obj.Class, methodName);

            if (args.Length != method.parameters.Length) throw AdvException.CallWrongArgc(obj.Class, method, args.Length);

            return RunMethod(obj, method, args.Select(RunExpression).ToArray());
        }

        AdvObject RunExpression(Expression expression)
        {
            return expression switch
            {
                Expression.Get { name: "this" } => self,
                Expression.Get { name: var name } when locals!.ContainsKey(name) => locals[name],
                Expression.Get { name: var name } when Classes.ContainsKey(name) => new AdvObject(Classes[name]),
                Expression.Get { name: var name } => throw AdvException.NameNotFound(name),
                Expression.CallExpr { objExpr: var objExpr, methodName: var methodName, args: var args} => RunCall(objExpr, methodName, args),
                Expression.GetF { objExpr: var objExpr, fieldName: var fieldName } => RunExpression(objExpr).GetField(fieldName),
                Expression.Is { objExpr: var objExpr, className: var className } when Classes.ContainsKey(className) => RunExpression(objExpr).Class.@is(Classes[className]).ToAdvObject(),
                Expression.Is { objExpr: var objExpr, className: var className } => throw AdvException.Is(RunExpression(objExpr), className),
                Expression.Equals {objExpr1: var objExpr1, objExpr2: var objExpr2} => (RunExpression(objExpr1) == RunExpression(objExpr2)).ToAdvObject(),
                Expression.String { text: var text } => new AdvString(text),
                _ => throw new System.Diagnostics.UnreachableException()
            };
        }

        AdvObject? RunStatements(IEnumerable<Statement> statements)
        {
            foreach (var statement in statements)
            {
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
                        return RunExpression(res);
                    case Statement.If { condition: var condition, stmts: var stmts }:
                        if (RunExpression(condition).Class.@is(BuiltinTypes.True))
                        {
                            var res = RunStatements(stmts);
                            if (res != null) return res;
                        }
                        break;
                    case Statement.While { condition: var condition, stmts: var stmts }:
                        while (RunExpression(condition).Class.@is(BuiltinTypes.True))
                        {
                            var res = RunStatements(stmts);
                            if (res != null) return res;
                        }
                        break;
                    case Statement.Eval { expr: var expr }:
                        if (EvalParser == null)
                        {
                            throw AdvException.CallUndefined(BuiltinTypes.Program, "eval"); // The class provided is kinda a lie but that's not a priority right now
                        }
                        
                        if(RunExpression(expr) is AdvString { Value: var code })
                        {
                            IEnumerable<Statement> eStmts = EvalParser(code);
                            var res = RunStatements(eStmts);
                            if (res != null) return res;
                        }
                        else
                        {
                            throw AdvException.CallInvalidArgument(BuiltinTypes.Program, "eval"); // The class provided is kinda a lie but that's not a priority right now
                        }
                        break;
                }
            }

            return null;
        }
        
        return RunStatements(methodBody) ?? AdvObject.Null;
    }

    private AdvObject HandleEmptyBody(AdvObject self, Method method, AdvObject[] args)
    {
        Class GetDefiningClass(Class type, ClassMember member)
        {
            if (type.ownMembers.Contains(member)) return type;

            return GetDefiningClass(type.parent.ToNullable()!, member);
        }
        
        var definingClass = GetDefiningClass(self.Class, method);

        if (builtinMethods.TryGetValue((definingClass.name, method.name), out var builtinMethod))
        {
            if (definingClass.name == "Program" && self != ProgramObj)
                throw AdvException.CallWrongProgram(self.Class, method);
            
            if (builtinMethod.RequiresProgram && self.GetField("program") != ProgramObj)
                throw AdvException.CallNoProgram(self.Class, method);

            return builtinMethod.OnCall(new BuiltinMethodCtx(this, self, args, method)) ?? AdvObject.Null;
        }

        throw AdvException.CallEmpty(self.Class, method.name);
    }
}
