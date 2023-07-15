using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace aws_restapi.services;

public class AwsParams
{
    public static IEnumerable<string> GetGpuInstances()
    {
        var instanceTypesText = File.ReadAllText("params/aws-params.json");
        
        var instanceTypesJson = JObject.Parse(instanceTypesText);
        var gpuInstanceArray = instanceTypesJson["gpuInstances"]
            .Select(j => j.ToString());
        return gpuInstanceArray;
    }
}