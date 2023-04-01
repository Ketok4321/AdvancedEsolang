namespace AdvancedEsolang.InterpreterCS;

using AdvancedEsolang.Syntax;

public class AdvObject
{
    public static AdvObject Null => new AdvObject(BuiltinTypes.Null);
    
    public readonly Class Class;
    private Dictionary<string, AdvObject> fields = new Dictionary<string, AdvObject>();

    public AdvObject(Class @class)
    {
        if (@class.isAbstract) throw new Exception(); //TODO
        
        Class = @class;
    }

    public AdvObject GetField(string name)
    {
        return fields.GetValueOrDefault(name, Null);
    }

    public void SetField(string name, AdvObject value)
    {
        if (!Class.fields.Any(f => f.name == name)) throw new Exception(); //TODO

        fields[name] = value;
    }

    public override string ToString()
    {
        return Class.name;
    }
}

public class AdvString : AdvObject
{
    public readonly string Value;

    public AdvString(string value) : base(BuiltinTypes.String)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}
