using System.Reflection;

namespace HabiHamAIAPI.Models;

public static class WorkoutImportTemplateFactory
{
    public static object BuildSessionTemplate()
    {
        return BuildTemplate<UpsertWorkoutSessionRequest>();
    }

    public static object BuildTemplate<TModel>() where TModel : new()
    {
        return BuildValue(typeof(TModel), typeof(TModel).Name, 0) ?? new TModel();
    }

    private static object? BuildValue(Type type, string propertyName, int depth)
    {
        if (depth > 6)
        {
            return null;
        }

        var targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(string))
        {
            return BuildStringSample(propertyName);
        }

        if (targetType == typeof(DateOnly))
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        if (targetType == typeof(DateTime))
        {
            return DateTime.UtcNow;
        }

        if (targetType == typeof(bool))
        {
            return false;
        }

        if (targetType == typeof(int))
        {
            return 0;
        }

        if (targetType == typeof(long))
        {
            return 0L;
        }

        if (targetType == typeof(decimal))
        {
            return 0m;
        }

        if (targetType == typeof(double))
        {
            return 0d;
        }

        if (targetType == typeof(float))
        {
            return 0f;
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Empty;
        }

        if (TryGetListItemType(targetType, out var listItemType))
        {
            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listItemType))!;
            var listItem = BuildValue(listItemType, listItemType.Name, depth + 1);
            if (listItem is not null)
            {
                list.Add(listItem);
            }
            return list;
        }

        if (!targetType.IsClass)
        {
            return null;
        }

        var instance = Activator.CreateInstance(targetType);
        if (instance is null)
        {
            return null;
        }

        foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = BuildValue(property.PropertyType, property.Name, depth + 1);
            property.SetValue(instance, value);
        }

        return instance;
    }

    private static bool TryGetListItemType(Type type, out Type itemType)
    {
        itemType = null!;

        if (type.IsArray)
        {
            itemType = type.GetElementType()!;
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        if (genericType != typeof(List<>) && genericType != typeof(ICollection<>) && genericType != typeof(IEnumerable<>))
        {
            return false;
        }

        itemType = type.GetGenericArguments()[0];
        return true;
    }

    private static string BuildStringSample(string propertyName)
    {
        var normalized = propertyName.Trim().ToLowerInvariant();
        return normalized switch
        {
            "sessioncode" => "workout::1700000000000",
            "programcode" => "upper-body-a",
            "day" => "День ног",
            "notes" => "Импортировано из JSON",
            "name" => "Присед со штангой",
            "meta" => "Комментарий к упражнению",
            "comment" => "Техника и акцент",
            "sourceexerciseid" => "00000000-0000-0000-0000-000000000000",
            "weight" => "60",
            "reps" => "10",
            "rpe" => "8",
            _ => string.Empty
        };
    }
}
