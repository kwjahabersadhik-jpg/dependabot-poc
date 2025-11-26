using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Application;

public static class DictionaryExtension
{

    public static T ConvertKeyValueDataToObject<T>(IEnumerable<KeyValueTable> inputList) where T : class, IDisposable, new()
    {
        using var result = (T)Activator.CreateInstance(typeof(T))!;
        foreach (var property in result?.GetType().GetProperties()!)
        {
            var _columnNameFromAttribute = property.CustomAttributes
                .FirstOrDefault(x => x.AttributeType == typeof(ColumnAttribute))?
                .ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (_columnNameFromAttribute is null) continue;

            var _columnValueFromTable = inputList.FirstOrDefault(x => x.ColumnName.ToUpper() == _columnNameFromAttribute.ToUpper())?.ColumnValue;

            if (_columnValueFromTable is not null)
                property.SetValue(result, _columnValueFromTable);
        }

        return result;
    }

    public static Dictionary<string, string> ConvertObjectToDictionary<T>(T source) where T : class, new()
    {
        var keyValues = new Dictionary<string, string>();

        foreach (var property in source.GetType().GetProperties())
        {
            var propertyValue = property.GetValue(source, null);
            if (propertyValue is null || string.IsNullOrEmpty(propertyValue?.ToString())) continue;
            if (decimal.TryParse(propertyValue?.ToString(), out decimal _numericValue) && _numericValue == 0) continue;

            var _columnNameFromAttribute = property.CustomAttributes
              .FirstOrDefault(x => x.AttributeType == typeof(ColumnAttribute))?
              .ConstructorArguments.FirstOrDefault().Value?.ToString();

            var columnName = _columnNameFromAttribute ?? property.Name;
            keyValues.Add(columnName, propertyValue?.ToString()!);
        }


        return keyValues;
    }


}
