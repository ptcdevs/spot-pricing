using System.ComponentModel;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

public class OnDemandPrice
{
    [Name("")] public string Id;
    [Name("CreatedAt")] public DateTimeOffset CreatedAt;
    [Name("SKU")] public string SKU;
    [Name("OnDemandCsvRowsId")] public long OnDemandCsvFilesId;
    [Name("OnDemandCsvRowsId")] public long OnDemandCsvRowsId;
    [Name("OfferTermCode")] public string OfferTermCode;
    [Name("RateCode")] public string RateCode;
    [Name("TermType")] public string TermType;
    [Name("PriceDescription")] public string PriceDescription;
    [Name("EffectiveDate")] public DateTime EffectiveDate;
    [Name("StartingRange")] public string StartingRange;
    [Name("EndingRange")] public string EndingRange;
    [Name("Unit")] public string Unit;
    [Name("PricePerUnit")] public decimal PricePerUnit;
    [Name("Currency")] public string Currency;
    [Name("RelatedTo")] public string RelatedTo;
    [Name("LeaseContractLength")] public string LeaseContractLength;
    [Name("PurchaseOption")] public string PurchaseOption;
    [Name("OfferingClass")] public string OfferingClass;
    [Name("Product Family")] public string ProductFamily;
    [Name("serviceCode")] public string serviceCode;
    [Name("Location")] public string Location;
    [Name("Location Type")] public string LocationType;
    [Name("Instance Type")] public string InstanceType;
    [Name("Current Generation")] public string CurrentGeneration;
    [Name("Instance Family")] public string InstanceFamily;
    [Name("vCPU")] public string vCPU;
    [Name("Physical Processor")] public string PhysicalProcessor;
    [Name("Clock Speed")] public string ClockSpeed;
    [Name("Memory")] public string Memory;
    [Name("Storage")] public string Storage;
    [Name("Network Performance")] public string NetworkPerformance;
    [Name("Processor Architecture")] public string ProcessorArchitecture;
    [Name("Tenancy")] public string Tenancy;
    [Name("Operating System")] public string OperatingSystem;
    [Name("License Model")] public string LicenseModel;
    [Name("GPU")] public string GPU;
    [Name("GPU Memory")] public string GpuMemory;
    [Name("instanceSKU")] public string instanceSKU;
    [Name("MarketOption")] public string MarketOption;
    [Name("Normalization Size Factor")] public string NormalizationSizeFactor;
    [Name("Physical Cores")] public string PhysicalCores;
    [Name("Processor Features")] public string ProcessorFeatures;
    [Name("Region Code")] public string RegionCode;
    [Name("serviceName")] public string serviceName;

    public static OnDemandPrice Convert(long csvFileId, DateTime createdAt, Dictionary<string, object> recordDictionary)
    {
        var onDemandPrice = new OnDemandPrice()
        {
            SKU = recordDictionary["SKU"].ToString(),
            CreatedAt = createdAt,
            OnDemandCsvFilesId = long.Parse(recordDictionary["OnDemandCsvFilesId"].ToString()),
            OnDemandCsvRowsId = long.Parse(recordDictionary["OnDemandCsvRowsId"].ToString()),
            OfferTermCode = recordDictionary["OfferTermCode"].ToString(),
            RateCode = recordDictionary["RateCode"].ToString(),
            TermType = recordDictionary["TermType"].ToString(),
            PriceDescription = recordDictionary["PriceDescription"].ToString(),
            EffectiveDate = DateTime.ParseExact(recordDictionary["EffectiveDate"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            StartingRange = recordDictionary["StartingRange"].ToString(),
            EndingRange = recordDictionary["EndingRange"].ToString(),
            Unit = recordDictionary["Unit"].ToString(),
            PricePerUnit = decimal.Parse(recordDictionary["PricePerUnit"].ToString()),
            Currency = recordDictionary["Currency"].ToString(),
            RelatedTo = recordDictionary["RelatedTo"].ToString(),
            LeaseContractLength = recordDictionary["LeaseContractLength"].ToString(),
            PurchaseOption = recordDictionary["PurchaseOption"].ToString(),
            OfferingClass = recordDictionary["OfferingClass"].ToString(),
            ProductFamily = recordDictionary["Product Family"].ToString(),
            serviceCode = recordDictionary["serviceCode"].ToString(),
            Location = recordDictionary["Location"].ToString(),
            LocationType = recordDictionary["Location Type"].ToString(),
            InstanceType = recordDictionary["Instance Type"].ToString(),
            CurrentGeneration = recordDictionary["Current Generation"].ToString(),
            InstanceFamily = recordDictionary["Instance Family"].ToString(),
            vCPU = recordDictionary["vCPU"].ToString(),
            PhysicalProcessor = recordDictionary["Physical Processor"].ToString(),
            ClockSpeed = recordDictionary["Clock Speed"].ToString(),
            Memory = recordDictionary["Memory"].ToString(),
            Storage = recordDictionary["Storage"].ToString(),
            NetworkPerformance = recordDictionary["Network Performance"].ToString(),
            ProcessorArchitecture = recordDictionary["Processor Architecture"].ToString(),
            Tenancy = recordDictionary["Tenancy"].ToString(),
            OperatingSystem = recordDictionary["Operating System"].ToString(),
            LicenseModel = recordDictionary["License Model"].ToString(),
            GPU = recordDictionary["GPU"].ToString(),
            GpuMemory = recordDictionary["GPU Memory"].ToString(),
            instanceSKU = recordDictionary["instanceSKU"].ToString(),
            MarketOption = recordDictionary["MarketOption"].ToString(),
            NormalizationSizeFactor = recordDictionary["Normalization Size Factor"].ToString(),
            PhysicalCores = recordDictionary["Physical Cores"].ToString(),
            ProcessorFeatures = recordDictionary["Processor Features"].ToString(),
            RegionCode = recordDictionary["Region Code"].ToString(),
            serviceName = recordDictionary["serviceName"].ToString(),
        };
        return onDemandPrice;
    }
}