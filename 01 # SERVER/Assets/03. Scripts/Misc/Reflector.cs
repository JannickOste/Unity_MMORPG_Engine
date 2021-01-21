using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

enum ReflectionField
{
    position, rotation, scale
}

public sealed class Reflector
{
    private static bool enableLogging = false;

    private static IEnumerable<System.Type> enumTypes = Assembly.GetAssembly(typeof(Server)).GetTypes().Where(type => type.IsEnum);
    private static Dictionary<ReflectionField, string> reflectionFieldNames = new Dictionary<ReflectionField, string>()
    {
        { ReflectionField.rotation, "rotation" },
        { ReflectionField.position, "position" },
        { ReflectionField.scale, "scale" }
    };

    public static IEnumerable<System.Type> GetEnumTypes()
    {
        return enumTypes;
    }

    #region Internal::Getters/Setters(Func actions)

    private static readonly Dictionary<Type, Func<object, string, object>> getMemberActions = new Dictionary<Type, Func<object, string, object>>()
    {
        { typeof(MemberInfo[]),   new Func<object, string, object>((targetObject, memberName) => string.IsNullOrEmpty(memberName) ? targetObject.GetType().GetMembers() : targetObject.GetType().GetMember(memberName))},
        { typeof(PropertyInfo),   new Func<object, string, object>((targetObject, memberName) => targetObject.GetType().GetProperty(memberName))},
        { typeof(PropertyInfo[]), new Func<object, string, object>((targetObject, memberName) => targetObject.GetType().GetMembers())},
        { typeof(FieldInfo),      new Func<object, string, object>((targetObject, memberName) => targetObject.GetType().GetField(memberName))},
        { typeof(FieldInfo[]),    new Func<object, string, object>((targetObject, memberName) => targetObject.GetType().GetFields()) }
    };
    private static readonly Dictionary<Type, Func<object, string, object>> getValueActions = new Dictionary<Type, Func<object, string, object>>()
    {
        { typeof(FieldInfo),      new Func<object, string, object>((targetObject, memberName) => GetMember<FieldInfo>(targetObject, memberName).GetValue(targetObject)) },
        { typeof(PropertyInfo),   new Func<object, string, object>((targetObject, memberName) => GetMember<PropertyInfo>(targetObject, memberName).GetValue(targetObject)) }
    };

    private static readonly Dictionary<Type, Action<object, string, object>> setMemberActions = new Dictionary<Type, Action<object, string, object>>()
    {
        { typeof(FieldInfo),      new Action<object, string, object>((targetObject, memberName, value) => GetMember<FieldInfo>(targetObject, memberName).SetValue(targetObject, value))},
        { typeof(PropertyInfo),   new Action<object, string, object>((targetObject, memberName, value) => GetMember<PropertyInfo>(targetObject, memberName).SetValue(targetObject, value))},
    };

    private static readonly Dictionary<ReflectionField, Action<object, object>> setCustomActions = new Dictionary<ReflectionField, Action<object, object>>()
    {
        {ReflectionField.scale,    new Action<object, object>((targetObject, input) => ((MonoBehaviour)targetObject).transform.localScale = (Vector3)input)},
        {ReflectionField.position, new Action<object, object>((targetObject, input) => ((MonoBehaviour)targetObject).transform.position = (Vector3)input)},
        {ReflectionField.rotation, new Action<object, object>((targetObject, input) => ((MonoBehaviour)targetObject).transform.rotation = Quaternion.Euler(((Quaternion)input).x, ((Quaternion)input).y, ((Quaternion)input).z))},
    };
    #endregion

    #region Internal::Getters/Setter(methods)
    /// <summary>Get a member(field, property, etc..) from an object.</summary>
    /// <param name="targetObject">Object instance</param>
    /// <param name="name">Member name</param>
    private static T GetMember<T>(object targetObject, string name = null) => (T)GetMember(typeof(T), targetObject, name);
    private static object GetMember(System.Type type, object instance, string name = null, bool raw = true)
    {
        object output = null;

        if (getMemberActions.ContainsKey(type))
        {
            Func<object, string, object> getFunc = getMemberActions[type];
            var rawOut = getFunc(instance, name);

            output = rawOut;
        }

        return output;
    }

    /// <summary>Get a set of members(field, property, etc..) from an object.</summary>
    /// <param name="targetObject">Object instance</param>
    /// <param name="names">Member names</param>
    private static T GetMembers<T>(object targetObject, IEnumerable<string> names) => (T)Convert.ChangeType(GetMembers(typeof(T), targetObject, names), typeof(T));
    private static IEnumerator<object> GetMembers(System.Type type, object instance, IEnumerable<string> names, bool raw = true)
    {
        object[] output = new object[names.Count()];

        if (getMemberActions.ContainsKey(type))
        {
            for (int index = 0; index < names.Count(); index++)
            {
                Func<object, string, object> getFunc = getMemberActions[type];
                var rawOut = getFunc(instance, names.ElementAt(index));
                yield return rawOut;
            }
        }
    }

    /// <summary>Extract the value from a member on an object instance.</summary>
    /// <param name="targetObject">Object instance</param>
    /// <param name="name">Member name</param>
    private static object GetValue<T>(object targetObject, string name) => GetValue(typeof(T), targetObject, name);
    private static object GetValue(System.Type membertype, object targetObject, string name)
    {
        Func<object, string, object> valueFetcher;

        if (getValueActions.TryGetValue(membertype, out valueFetcher))
            return valueFetcher(targetObject, name);
        return null;
    }

    /// <summary>Set a member attribute on an object instance.</summary>
    /// <param name="targetObject">Object instance</param>
    /// <param name="name">Member name</param>
    /// <param name="input">Value to set property to</param>
    private static void Set<T>(object targetObject, string name, object input) => Set(typeof(T), targetObject, name, input);
    private static void Set(System.Type type, object targetObject, string name, object input)
    {
        if (GetMember<MemberInfo[]>(targetObject, name).FirstOrDefault() == null)
        {
            if (reflectionFieldNames.Keys.Select(i => i.ToString().ToLower()).Contains(name))
            {
                MonoBehaviour monoInstance = (MonoBehaviour)targetObject;
                ReflectionField targetField = (ReflectionField)reflectionFieldNames.Keys.ToArray().GetValue(reflectionFieldNames.Values.ToList().IndexOf(name));

                if (setCustomActions.ContainsKey(targetField)) setCustomActions[targetField](targetObject, input);
                else if (enableLogging) Debug.Log($"No custom setter found for {targetField}");
            }
        }
        else
        {
            Action<object, string, object> setAction;

            try
            {
                if (setMemberActions.TryGetValue(type, out setAction)) setAction(targetObject, name, input);
                else if (enableLogging) Debug.Log($"No member information found for {name}");
            }
            catch (NullReferenceException ex)
            {
                if (enableLogging) Debug.Log($"Field {name} not found in instace: {ex.Message}");
            }
        }
    }

    /// <summary> Convert (enum)MemberTypes -> System.Type  </summary>
    /// <param name="type">MemberTypes property</param>
    private static System.Type MemberTypeToType(MemberTypes type)
    {
        switch (type)
        {
            case MemberTypes.Field: return typeof(FieldInfo);
            case MemberTypes.Property: return typeof(PropertyInfo);
            case MemberTypes.Method: return typeof(MethodInfo);
        }
        return null;
    }
    #endregion

    #region External::Reflection methods

    /// <summary> Enable/Disable all logging output </summary>
    /// <param name="set">T: active, F: disable</param>
    public static void EnableLogPrinting(bool set) => enableLogging = set;
    public static object StringToObject(string dataString, string conversionType)
    {
        System.Type enumType = enumTypes.Where(enumtype => conversionType.Contains(enumtype.FullName)).FirstOrDefault();
        if (enumType != null) return System.Enum.Parse(enumType, dataString);

        System.Type type = System.Type.GetType(conversionType);
        Debug.Log(conversionType);
        string[] pieces = dataString.Split(',');
        if (type == null) Debug.Log("Failed to fetch object type");
        else
        {
            switch (type.ToString())
            {
                case "System.Int32[]": return pieces.Select(i => int.Parse(i)).ToArray();
                case "System.String[]": return pieces.Select(i => i.Trim()).ToArray();
                case "System.Single[]": return pieces.Select(i => float.Parse(i)).ToArray();
                case "System.Boolean[]": return pieces.Select(i => bool.Parse(i)).ToArray();

                default:
                    if (type == null) Debug.Log($"Type for {conversionType} not found");
                    return Convert.ChangeType(dataString, type);
            }
        }
        return null;
    }

    /// <summary> Set data from a dictionary to an object </summary>
    /// <param name="properties">Collection::Member name/value</param>
    /// <param name="targetObject">Target object for reflection</param>
    public static void DictionaryToInstance(Dictionary<string, object> properties, object targetObject)
    {
        
        if (targetObject == null)
        {
            Debug.Log("[Reflector]: Instance attempting to write to is null");
            return;
        }

        foreach (string fieldName in properties.Keys)
        {
            if (reflectionFieldNames.Values.Contains(fieldName)) Set(null, targetObject, fieldName, properties[fieldName]);
            else
            {
                MemberInfo member = GetMember<MemberInfo[]>(targetObject, fieldName).FirstOrDefault();
                if (member != null) Set(MemberTypeToType(member.MemberType), targetObject, fieldName, properties[fieldName]);
            }
        }

    }

    /// <summary> Reflect all / specific object members to another object </summary>
    /// <param name="reflectObject">Source object</param>
    /// <param name="targetObject">Writing object</param>
    /// <param name="names">Write only names</param>
    public static void ObjectToObject(object reflectObject, object targetObject, IEnumerable<string> names = null)
    {
        MemberInfo[] memberData = names == null ? GetMember<MemberInfo[]>(reflectObject) : GetMembers<MemberInfo[]>(reflectObject, names);

        foreach (MemberInfo member in memberData)
        {
            System.Type memberType = MemberTypeToType(member.MemberType);

            if (memberType != null)
            {
                object reflectValue = GetValue(memberType, reflectObject, member.Name);

                Set(memberType, targetObject, member.Name, reflectValue);
            }
            else Debug.Log($"Unable to fetch MemberType for {member.Name}");
        }
    }
    #endregion

}