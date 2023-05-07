namespace AdvancedEsolang.InterpreterCS;

[Serializable]
public class AdvException : Exception
{
    public static AdvException ProgramNotFound(Library programLib) =>
        new($"Cannot run program '${programLib.name}': No program class found");
    public static AdvException NameNotFound(string name) =>
        new($"Cannot get '{name}': No such variable or class");

    private static AdvException Call(Class type, string method, string message) =>
        new($"Cannot call method '{method}' on an object of class '{type.name}': {message}");
    public static AdvException CallUndefined(Class type, string method) =>
        Call(type, method, "Class doesn't define such method");
    public static AdvException CallEmpty(Class type, string method) =>
        Call(type, method, "Method's body is empty (unoverriden abstract method?)");
    public static AdvException CallWrongArgc(Class type, Method method, int argc) =>
        Call(type, method.name, $"Wrong arg count. Expected {method.parameters.Length} ({String.Join(',', method.parameters)}), got {argc}");
    public static AdvException CallNoProgram(Class type, Method method) =>
        Call(type, method.name, "The 'program' field of the object is not set to the instance of the currently running program");
    public static AdvException CallWrongProgram(Class type, Method method) =>
        Call(type, method.name, "The object isn't the currently running program");
    public static AdvException CallInvalidArgument(Class type, string method) =>
        Call(type, method, "One of the arguments (or the object on which the call was performed) is not valid for that method");

    public static AdvException FieldUndefined(Class type, string field) =>
        new($"Cannot set '{field}' field of an object of class '{type.name}': No such field");
    public static AdvException Is(AdvObject obj, string type) =>
        new($"Cannot check if '{obj}' is '{type}': No such class");
    public static AdvException Abstract(Class type) =>
        new($"Cannot instantiate an object of '{type.name}' class: The class is abstract");


    private AdvException(string message) : base(message) { }
}