using System.ComponentModel;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using Npgsql;
using NpgsqlTypes;
using Serilog;

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
            EffectiveDate = DateTime.ParseExact(recordDictionary["EffectiveDate"].ToString(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            PricePerUnit = decimal.Parse(recordDictionary["PricePerUnit"].ToString()),
            OfferTermCode = recordDictionary["OfferTermCode"].ToString(),
            RateCode = recordDictionary["RateCode"].ToString(),
            TermType = recordDictionary["TermType"].ToString(),
            PriceDescription = recordDictionary["PriceDescription"].ToString(),
            StartingRange = recordDictionary["StartingRange"].ToString(),
            EndingRange = recordDictionary["EndingRange"].ToString(),
            Unit = recordDictionary["Unit"].ToString(),
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

        // onDemandPrice.OfferTermCode = onDemandPrice.OfferTermCode.Equals("") ? null : onDemandPrice.OfferTermCode;
        // onDemandPrice.RateCode = onDemandPrice.RateCode.Equals("") ? null : onDemandPrice.RateCode;
        // onDemandPrice.TermType = onDemandPrice.TermType.Equals("") ? null : onDemandPrice.TermType;
        // onDemandPrice.PriceDescription = onDemandPrice.PriceDescription.Equals("") ? null : onDemandPrice.PriceDescription;
        // onDemandPrice.StartingRange = onDemandPrice.StartingRange.Equals("") ? null : onDemandPrice.StartingRange;
        // onDemandPrice.EndingRange = onDemandPrice.EndingRange.Equals("") ? null : onDemandPrice.EndingRange;
        // onDemandPrice.Unit = onDemandPrice.Unit.Equals("") ? null : onDemandPrice.Unit;
        // onDemandPrice.Currency = onDemandPrice.Currency.Equals("") ? null : onDemandPrice.Currency;
        // onDemandPrice.RelatedTo = onDemandPrice.RelatedTo.Equals("") ? null : onDemandPrice.RelatedTo;
        // onDemandPrice.LeaseContractLength = onDemandPrice.LeaseContractLength.Equals("") ? null : onDemandPrice.LeaseContractLength;
        // onDemandPrice.PurchaseOption = onDemandPrice.PurchaseOption.Equals("") ? null : onDemandPrice.PurchaseOption;
        // onDemandPrice.OfferingClass = onDemandPrice.OfferingClass.Equals("") ? null : onDemandPrice.OfferingClass;
        // onDemandPrice.ProductFamily = onDemandPrice.ProductFamily.Equals("") ? null : onDemandPrice.ProductFamily;
        // onDemandPrice.serviceCode = onDemandPrice.serviceCode.Equals("") ? null : onDemandPrice.serviceCode;
        // onDemandPrice.Location = onDemandPrice.Location.Equals("") ? null : onDemandPrice.Location;
        // onDemandPrice.LocationType = onDemandPrice.LocationType.Equals("") ? null : onDemandPrice.LocationType;
        // onDemandPrice.InstanceType = onDemandPrice.InstanceType.Equals("") ? null : onDemandPrice.InstanceType;
        // onDemandPrice.CurrentGeneration = onDemandPrice.CurrentGeneration.Equals("") ? null : onDemandPrice.CurrentGeneration;
        // onDemandPrice.InstanceFamily = onDemandPrice.InstanceFamily.Equals("") ? null : onDemandPrice.InstanceFamily;
        // onDemandPrice.vCPU = onDemandPrice.vCPU.Equals("") ? null : onDemandPrice.vCPU;
        // onDemandPrice.PhysicalProcessor = onDemandPrice.PhysicalProcessor.Equals("") ? null : onDemandPrice.PhysicalProcessor;
        // onDemandPrice.ClockSpeed = onDemandPrice.ClockSpeed.Equals("") ? null : onDemandPrice.ClockSpeed;
        // onDemandPrice.Memory = onDemandPrice.Memory.Equals("") ? null : onDemandPrice.Memory;
        // onDemandPrice.Storage = onDemandPrice.Storage.Equals("") ? null : onDemandPrice.Storage;
        // onDemandPrice.NetworkPerformance = onDemandPrice.NetworkPerformance.Equals("") ? null : onDemandPrice.NetworkPerformance;
        // onDemandPrice.ProcessorArchitecture = onDemandPrice.ProcessorArchitecture.Equals("") ? null : onDemandPrice.ProcessorArchitecture;
        // onDemandPrice.Tenancy = onDemandPrice.Tenancy.Equals("") ? null : onDemandPrice.Tenancy;
        // onDemandPrice.OperatingSystem = onDemandPrice.OperatingSystem.Equals("") ? null : onDemandPrice.OperatingSystem;
        // onDemandPrice.LicenseModel = onDemandPrice.LicenseModel.Equals("") ? null : onDemandPrice.LicenseModel;
        // onDemandPrice.GPU = onDemandPrice.GPU.Equals("") ? null : onDemandPrice.GPU;
        // onDemandPrice.GpuMemory = onDemandPrice.GpuMemory.Equals("") ? null : onDemandPrice.GpuMemory;
        // onDemandPrice.instanceSKU = onDemandPrice.instanceSKU.Equals("") ? null : onDemandPrice.instanceSKU;
        // onDemandPrice.MarketOption = onDemandPrice.MarketOption.Equals("") ? null : onDemandPrice.MarketOption;
        // onDemandPrice.NormalizationSizeFactor = onDemandPrice.NormalizationSizeFactor.Equals("") ? null : onDemandPrice.NormalizationSizeFactor;
        // onDemandPrice.PhysicalCores = onDemandPrice.PhysicalCores.Equals("") ? null : onDemandPrice.PhysicalCores;
        // onDemandPrice.ProcessorFeatures = onDemandPrice.ProcessorFeatures.Equals("") ? null : onDemandPrice.ProcessorFeatures;
        // onDemandPrice.RegionCode = onDemandPrice.RegionCode.Equals("") ? null : onDemandPrice.RegionCode;
        // onDemandPrice.serviceName = onDemandPrice.serviceName.Equals("") ? null : onDemandPrice.serviceName;
        return onDemandPrice;
    }

    public static async Task BulkCopy(NpgsqlBinaryImporter pgPricingBulkCopier, DateTimeOffset createdAt, OnDemandPrice onDemandPrice, CancellationToken cancellationToken)
    {
        try
        {
            await pgPricingBulkCopier.StartRowAsync(cancellationToken);
            await pgPricingBulkCopier.WriteAsync(createdAt.ToUniversalTime(), NpgsqlDbType.TimestampTz,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.SKU, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvFilesId, NpgsqlDbType.Bigint,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvRowsId, NpgsqlDbType.Bigint,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferTermCode, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.RateCode, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.TermType, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.PriceDescription, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.EffectiveDate, NpgsqlDbType.Date, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.StartingRange, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.EndingRange, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Unit, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.PricePerUnit, NpgsqlDbType.Money, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Currency, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.RelatedTo, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.LeaseContractLength, NpgsqlDbType.Text,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.PurchaseOption, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferingClass, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProductFamily, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceCode, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Location, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.LocationType, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceType, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.CurrentGeneration, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceFamily, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.vCPU, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalProcessor, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.ClockSpeed, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Memory, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Storage, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.NetworkPerformance, NpgsqlDbType.Text,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorArchitecture, NpgsqlDbType.Text,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.Tenancy, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.OperatingSystem, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.LicenseModel, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.GPU, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.GpuMemory, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.instanceSKU, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.MarketOption, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.NormalizationSizeFactor, NpgsqlDbType.Text,
                cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalCores, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorFeatures, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.RegionCode, NpgsqlDbType.Text, cancellationToken);
            await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceName, NpgsqlDbType.Text, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ondemanprice bulk copy error");
            Log.Error(ex, "ondemanprice bulk copy error: {OnDemandPriceRecord}", onDemandPrice);
            Log.Error(ex.InnerException, "ondemanprice bulk copy error");
        }
    }
    public static void BulkCopy(NpgsqlBinaryImporter pgPricingBulkCopier, DateTimeOffset createdAt, OnDemandPrice onDemandPrice)
    {
        try
        {
            pgPricingBulkCopier.StartRow();
            pgPricingBulkCopier.Write(createdAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
            pgPricingBulkCopier.Write(onDemandPrice.SKU, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.OnDemandCsvFilesId, NpgsqlDbType.Bigint);
            pgPricingBulkCopier.Write(onDemandPrice.OnDemandCsvRowsId, NpgsqlDbType.Bigint);
            pgPricingBulkCopier.Write(onDemandPrice.OfferTermCode, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.RateCode, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.TermType, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.PriceDescription, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.EffectiveDate, NpgsqlDbType.Date);
            pgPricingBulkCopier.Write(onDemandPrice.StartingRange, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.EndingRange, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.Unit, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.PricePerUnit, NpgsqlDbType.Money);
            pgPricingBulkCopier.Write(onDemandPrice.Currency, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.RelatedTo, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.LeaseContractLength, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.PurchaseOption, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.OfferingClass, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.ProductFamily, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.serviceCode, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.Location, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.LocationType, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.InstanceType, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.CurrentGeneration, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.InstanceFamily, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.vCPU, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.PhysicalProcessor, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.ClockSpeed, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.Memory, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.Storage, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.NetworkPerformance, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.ProcessorArchitecture, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.Tenancy, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.OperatingSystem, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.LicenseModel, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.GPU, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.GpuMemory, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.instanceSKU, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.MarketOption, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.NormalizationSizeFactor, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.PhysicalCores, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.ProcessorFeatures, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.RegionCode, NpgsqlDbType.Text);
            pgPricingBulkCopier.Write(onDemandPrice.serviceName, NpgsqlDbType.Text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ondemanprice bulk copy error");
            Log.Error(ex.InnerException, "ondemanprice bulk copy error");
        }
    }
}