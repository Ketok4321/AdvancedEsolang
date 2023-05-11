namespace AdvancedEsolang.InterpreterCS;

public class AdvObject
{
    public static AdvObject Null => new AdvObject(BuiltinTypes.Null);
    
    public readonly Class Class;
    private readonly Dictionary<string, AdvObject> fields;

    public AdvObject(Class type)
    {
        if (type.isAbstract) throw AdvException.Abstract(type);
        
        Class = type;
        fields = new Dictionary<string, AdvObject>(); //TODO: This could probably be optimized by setting a capacity; The fields should ideally initialize to Null on object creation
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

public sealed class AdvString : AdvObject
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
