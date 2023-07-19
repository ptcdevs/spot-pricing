using System.ComponentModel;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using Npgsql;
using NpgsqlTypes;

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

    public static OnDemandPrice Convert(Dictionary<string, object> recordDictionary)
    {
        var onDemandPrice = new OnDemandPrice()
        {
            SKU = recordDictionary["SKU"].ToString(),
            OnDemandCsvFilesId = long.Parse(recordDictionary["OnDemandCsvFilesId"].ToString()),
            OnDemandCsvRowsId = long.Parse(recordDictionary["OnDemandCsvRowsId"].ToString()),
            OfferTermCode = recordDictionary["OfferTermCode"].ToString(),
            RateCode = recordDictionary["RateCode"].ToString(),
            TermType = recordDictionary["TermType"].ToString(),
            PriceDescription = recordDictionary["PriceDescription"].ToString(),
            EffectiveDate = DateTime.ParseExact(recordDictionary["EffectiveDate"].ToString(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
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

    public static async Task BulkCopy(NpgsqlBinaryImporter pgPricingBulkCopier, DateTimeOffset createdAt,
        OnDemandPrice onDemandPrice)
    {
        await pgPricingBulkCopier.StartRowAsync();
        await pgPricingBulkCopier.WriteAsync(createdAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.SKU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvFilesId, NpgsqlDbType.Bigint);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvRowsId, NpgsqlDbType.Bigint);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferTermCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RateCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.TermType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PriceDescription, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.EffectiveDate, NpgsqlDbType.Date);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.StartingRange, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.EndingRange, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Unit, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PricePerUnit, NpgsqlDbType.Money);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Currency, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RelatedTo, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LeaseContractLength, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PurchaseOption, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferingClass, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProductFamily, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Location, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LocationType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.CurrentGeneration, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceFamily, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.vCPU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalProcessor, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ClockSpeed, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Memory, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Storage, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.NetworkPerformance, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorArchitecture, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Tenancy, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OperatingSystem, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LicenseModel, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.GPU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.GpuMemory, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.instanceSKU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.MarketOption, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.NormalizationSizeFactor, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalCores, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorFeatures, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RegionCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceName, NpgsqlDbType.Text);
    }
}