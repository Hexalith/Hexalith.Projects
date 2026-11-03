using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
string[] schemas = ["{\"é\":{\"z\":1,\"a\":2},\"a\":[1,null,true],\"😀\":{\"\\uE000\":\"b\",\"😀\":\"a\"},\"z\":0}", "{\"a\":[1,null,true],\"z\":0,\"é\":{\"a\":2,\"z\":1},\"😀\":{\"😀\":\"a\",\"\\uE000\":\"b\"}}", "{\"z\":0,\"a\":1,\"a\":2}", "{\"a\":1,\"a\":2,\"z\":0}", "{\"a\":2,\"a\":1,\"z\":0}"];
foreach(string schema in schemas) {
 using var input=JsonDocument.Parse(schema);using var output=JsonDocument.Parse("{}");
 var operation=new RouteOperationContract("codec-order","v1","public","agent-eligible",input.RootElement,output.RootElement,"","query","sample","GetSample");
 Console.WriteLine(RouteCatalogCodec.ComputeOperationContractDigest(operation));
}
