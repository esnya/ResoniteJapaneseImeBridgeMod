using MozcInput.Bridge.Engine;
using MozcInput.Protocol;

namespace MozcInput.Bridge;

internal static class Program
{
    private static int Main()
    {
        var engine = BridgeEngineFactory.Create();

        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            var response = HandleLine(engine, line);
            Console.WriteLine(MozcBridgeSerializer.SerializeResponse(response));
            Console.Out.Flush();

            if (response.Handled && line.Contains("shutdown", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
        }

        return 0;
    }

    private static MozcBridgeResponse HandleLine(IBridgeEngine engine, string line)
    {
        try
        {
            var request = MozcBridgeSerializer.DeserializeRequest(line);
            if (request is null)
            {
                return MozcBridgeResponse.Failure(0, "Invalid request JSON.");
            }

            return engine.Handle(request);
        }
        catch (InvalidOperationException ex)
        {
            return MozcBridgeResponse.Failure(0, ex.Message);
        }
    }
}
