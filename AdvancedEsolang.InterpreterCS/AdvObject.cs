namespace AdvancedEsolang.InterpreterCS;

public class AdvObject
{
    public static AdvObject Null => new AdvObject(BuiltinTypes.Null);
    
    public readonly Class Class;
    private readonly Dictionary<string, AdvObject> fields;

    public AdvObject(Class @class)
    {
        if (@class.isAbstract) throw AdvException.Abstract(@class);
        
        Class = @class;
        fields = @class.fields.ToDictionary(f => f.name, _ => Null);
    }

    public AdvObject GetField(string name)
    {
        return fields.GetValueOrDefault(name, Null);
    }

    public void SetField(string name, AdvObject value)
    {
        if (Class.get<Field>(name).ToNullable() == null) throw AdvException.FieldUndefined(Class, name);

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
