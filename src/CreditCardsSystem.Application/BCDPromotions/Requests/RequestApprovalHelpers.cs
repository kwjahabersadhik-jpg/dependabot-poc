using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Entities.PromoEntities;
using CreditCardsSystem.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CreditCardsSystem.Application.BCDPromotions.Requests;

public class RequestApprovalHelpers<T> : IRequestApprovalHelpers
    where T : class
{

    private FdrDBContext _fdrDbContext;
    IRequestsHelperMethods _requestsHelperMethods;

    public async Task<ApiResponseModel<object>> Approve(ApproveRequestDto approveRequestDto)
    {
        var response = new ApiResponseModel<object>();

        var getObjectFromLookupMethod = GetType().GetMethod(nameof(GetObjectFromLookup),
            BindingFlags.NonPublic | BindingFlags.Instance);

        var genericType = new[] { typeof(T) };

        var requestDetailsObjFromLookup = getObjectFromLookupMethod!
                            .MakeGenericMethod(genericType)
                            .Invoke(this, new object[] { approveRequestDto.RequestDetails });

        if (approveRequestDto.ActivityType == ActivityType.Add)
        {
            long id;

            if (typeof(T) != typeof(CardDefinition))
                id = await _requestsHelperMethods.GetNewRequestId(DetectSequence(typeof(T)));
            else
                id = (int)GetPrimaryKeyValue(requestDetailsObjFromLookup);

            SetPrimaryKeyValue(requestDetailsObjFromLookup, id);
            _fdrDbContext.Set<T>().Add((requestDetailsObjFromLookup as T)!);
        }
        else
        {
            var value = GetPrimaryKeyValue(requestDetailsObjFromLookup);
            var entity = await FindEntityByDynamicPrimaryKeyAsync(value);

            switch (approveRequestDto.ActivityType)
            {
                case ActivityType.Edit:
                    _fdrDbContext.Entry(entity).CurrentValues.SetValues(requestDetailsObjFromLookup!);
                    if (typeof(T) == typeof(CardDefinition))
                    {
                        var newCardDef = requestDetailsObjFromLookup as CardDefinition;
                        var oldCardDef = entity as CardDefinition;

                        foreach (var ext in newCardDef!.CardDefExts.ToList())
                        {
                            var oldExt = oldCardDef!.CardDefExts
                                .FirstOrDefault(e => e.Attribute == ext.Attribute);

                            //means this attribute is not exist
                            if (oldExt == null)
                                oldCardDef.CardDefExts.Add(ext);
                            else
                                oldExt.Value = ext.Value;
                        }
                    }
                    break;

                case ActivityType.Delete:
                    _fdrDbContext.Set<T>().Remove(entity!);
                    break;
            }

            var re = entity;

        }



        //TODO: Clear caches
        await _fdrDbContext.SaveChangesAsync();
        return response.Success(new { IsApproved = true });
    }

    public void SetFields(dynamic fdrDbContext, IRequestsHelperMethods requestsHelperMethods)
    {
        _fdrDbContext = fdrDbContext;
        _requestsHelperMethods = requestsHelperMethods;
    }

    private T GetObjectFromLookup<T>(List<RequestActivityDetailsDto> requestDetails)
    {
        var newItem = Activator.CreateInstance<T>();

        foreach (var prop in newItem!.GetType().GetProperties())
        {
            if (!prop.CanWrite) continue;

            // valid to be set to null value because it's a new added record and no need to lock it 
            if (prop.Name.Equals("islocked", StringComparison.OrdinalIgnoreCase)) continue;

            // ef navigation property need to be skipped
            if (prop.Name.Equals(nameof(PromotionCard.Promotion), StringComparison.OrdinalIgnoreCase)) continue;

            if (prop.Name.Equals(nameof(CardDefinition.CardDefExts), StringComparison.OrdinalIgnoreCase))
            {
                var extensionsList = GetCardDefExts(requestDetails);
                prop.SetValue(newItem, extensionsList);
            }
            else
            {
                var value = GetValueByParameter(requestDetails, prop.Name, newItem.GetType());

                if (!string.IsNullOrEmpty(value))
                {
                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                        prop.SetValue(newItem, Convert.ToDateTime(value));

                    else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                        prop.SetValue(newItem, decimal.Parse(value, CultureInfo.InvariantCulture));

                    else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                        prop.SetValue(newItem, long.Parse(value));

                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                        prop.SetValue(newItem, int.Parse(value));

                    else if (prop.PropertyType == typeof(short) || prop.PropertyType == typeof(short?))
                        prop.SetValue(newItem, short.Parse(value));

                    else if (prop.PropertyType == typeof(byte) || prop.PropertyType == typeof(byte?))
                        prop.SetValue(newItem, byte.Parse(value));

                    else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                    {
                        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                            prop.SetValue(newItem, true);
                        else
                            prop.SetValue(newItem, false);
                    }
                    else
                        prop.SetValue(newItem, Convert.ChangeType(value, prop.PropertyType));
                }
            }
        }
        return newItem;
    }

    private string GetValueByParameter(List<RequestActivityDetailsDto> requestDetails, string parameterName, Type modelType)
    {
        if (modelType == typeof(GroupAttribute))
        {
            if (parameterName.Equals(nameof(GroupAttribute.AttributeId), StringComparison.OrdinalIgnoreCase))
            {
                var oldAttributeId = requestDetails
                    .FirstOrDefault(r => r.Parameter.Equals("Old_AttributeID", StringComparison.OrdinalIgnoreCase));

                if (oldAttributeId != null)
                    return oldAttributeId.Value;
            }

        }

        var parameter = requestDetails.FirstOrDefault(x => x.Parameter.ToLower() == parameterName.ToLower());
        return parameter != null ? !string.IsNullOrEmpty(parameter.Value) ? parameter.Value : string.Empty : string.Empty;

    }

    private ICollection<CardDefinitionExtention> GetCardDefExts(List<RequestActivityDetailsDto> requestDetails)
    {

        var extensionsList = new List<CardDefinitionExtention>();

        var extensions = requestDetails
            .Where(r => r.Parameter.Contains("_ext_"))
            .ToList();

        if (extensions.Any())
        {

            var cardType = extensions.FirstOrDefault(e => e.Parameter.ToLower().Contains("cardtype"))!.Value;

            extensions.RemoveAll(e => e.Parameter.ToLower().Contains("cardtype_ext") ||
                                      e.Parameter.ToLower().Contains("id_ext"));

            for (var i = 1; i <= extensions.Count / 2; i++)
            {
                var extensionObj = extensions.Where(e => e.Parameter.ToLower().Contains($"_ext_{i}")).ToList();

                extensionsList.Add(new CardDefinitionExtention
                {
                    Attribute = extensionObj.FirstOrDefault(e => e.Parameter.ToLower().Contains("attribute"))!.Value,
                    Value = extensionObj.FirstOrDefault(e => e.Parameter.ToLower().Contains("value"))!.Value,
                    CardType = int.Parse(cardType)
                });
            }
        }

        return extensionsList;
    }

    private string DetectSequence(Type type)
    {
        if (type == typeof(Promotion) || type == typeof(PromotionCard))
            return "promo.PROMOTION_PRODUCT_SEQ";

        if (type == typeof(PromotionGroup))
            return "promo.PROMOTION_GROUP_SEQ";

        if (type == typeof(GroupAttribute))
            return "promo.PROMOTION_CLASS_SEQ";

        if (type == typeof(Pct))
            return "promo.PCT_SEQ";

        if (type == typeof(Service))
            return "promo.SERVICES_SEQ";

        if (type == typeof(CardtypeEligibilityMatix))
            return "promo.CARDTYPE_ELIGIBILITY_MATIX";


        return string.Empty;
    }

    private void SetPrimaryKeyValue(object? entity, object primaryKeyValue)
    {
        var entityType = _fdrDbContext.Model.FindEntityType(typeof(T));
        var primaryKeyProperty = entityType!.FindPrimaryKey()!.Properties.First();

        var property = typeof(T).GetProperty(primaryKeyProperty.Name);
        property!.SetValue(entity, Convert.ChangeType(primaryKeyValue, property.PropertyType));
    }

    private object GetPrimaryKeyValue(object? entity)
    {
        var entityType = _fdrDbContext.Model.FindEntityType(typeof(T));
        var primaryKeyProperty = entityType!.FindPrimaryKey()!.Properties.First();

        var property = typeof(T).GetProperty(primaryKeyProperty.Name);
        return property!.GetValue(entity)!;
    }

    private async Task<T?> FindEntityByDynamicPrimaryKeyAsync(object primaryKeyValue)
    {
        T? entity;
        var entityType = _fdrDbContext.Model.FindEntityType(typeof(T));
        var primaryKey = entityType!.FindPrimaryKey()!.Properties.First();
        var propertyName = primaryKey.Name;

        if (typeof(T) == typeof(CardDefinition))
            entity = await _fdrDbContext.Set<T>().Include(nameof(CardDefinition.CardDefExts)).FirstOrDefaultAsync(e => EF.Property<object>(e, propertyName) == primaryKeyValue);
        else
            entity = await _fdrDbContext.Set<T>().FirstOrDefaultAsync(e => EF.Property<object>(e, propertyName) == primaryKeyValue);

        return entity;
    }
}