namespace CreditCardsSystem.Domain.Models.Customer
{
    public class CustomerLookupData
    {
        public KFHItem[]? JobTitleLookupData { get; set; }
        public KFHItem[]? EducationLookupData { get; set; }
        public KFHItem[]? IdTypeLookupData { get; set; }
        public KFHItem[]? RimClassLookupData { get; set; }
        public KFHItem[]? NationalityLookupData { get; set; }
        public KFHItem[]? ReligionLookupData { get; set; }
        public KFHItem[]? LegalNatureLookupData { get; set; }
        public KFHItem[]? EconomicActivityLookupData { get; set; }
        public KFHItem[]? CustomerTypeLookupData { get; set; }
        public KFHItem[]? OccupationLookupData { get; set; }
        public KFHItem[]? CustomerActivityLookupData { get; set; }
        public List<KFHItem> GenderLookupData { get; set; }
        public List<KFHItem> MaritalStatuslLookupData { get; set; }
        public List<KFHItem> Residence { get; set; }
        public List<KFHItem>? AreaCode { get; set; }
    }

    public class KFHItem
    {
        public KFHItem()
        {

        }
        //public KFHItem(object? key, object? value)
        //{
        //    Key = key;
        //    Value = value;
        //}
        public int Key { get; set; }
        public string Value { get; set; }
    }
}
