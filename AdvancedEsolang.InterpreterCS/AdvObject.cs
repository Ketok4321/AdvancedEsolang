namespace AdvancedEsolang.InterpreterCS;

internal static class ClassExt {
    private static System.Runtime.CompilerServices.ConditionalWeakTable<Class, ClassData> ClassDatas = new(); // This could be just a regular dictionary, actually

    public record ClassData(string[] AllFields) { }

    private static ClassData CreateData(Class @class)
    {
        return new ClassData(@class.allMembersOfType<Field>().Select(m => m.name).ToArray());
    }

    public static ClassData GetData(this Class @class)
    {
        if (@class == null) throw new ArgumentNullException();

        if (ClassDatas.TryGetValue(@class, out ClassData? d))
        {
            return d!;
        }
        else
        {
            d = CreateData(@class);
            ClassDatas.Add(@class, d);
            return d;
        }
    }
}

public class AdvObject
{
    public static AdvObject Null => new AdvObject(BuiltinTypes.Null);
    
    public readonly Class Class;
    private readonly ClassExt.ClassData ClassData;
    private readonly AdvObject[] fields;

    public AdvObject(Class type)
    {
        if (type.isAbstract) throw AdvException.Abstract(type);
        
        Class = type;
        ClassData = type.GetData();

        fields = ClassData.AllFields.Length > 0 ? new AdvObject[ClassData.AllFields.Length] : Array.Empty<AdvObject>();
        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = Null;
        }
    }

    public AdvObject GetField(string name)
    {
        int i = Array.IndexOf(ClassData.AllFields, name);
        if (i == -1) return Null; // Maybe it should throw an exception?

        return fields[i];
    }

    public void SetField(string name, AdvObject value)
    {
        int i = Array.IndexOf(ClassData.AllFields, name);
        if (i == -1) throw AdvException.FieldUndefined(Class, name);

        fields[i] = value;
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
