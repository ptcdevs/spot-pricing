using System.Text.Json.Nodes;

namespace aws_restapi.services;

public class AwsParams
{
    public static List<string?>? GetGpuInstances()
    {
        var instanceTypesText = File.ReadAllText("params/aws-params.json");
        var instanceTypesJson = JsonNode.Parse(instanceTypesText);
        var instanceTypes = instanceTypesJson == null
                ? new List<string?>()
                : instanceTypesJson
                    .AsArray()
                    .Select(node => node?.ToString())
                    .Where(instanceType => instanceType != null)
                    .ToList();

        return instanceTypes;
    }
}