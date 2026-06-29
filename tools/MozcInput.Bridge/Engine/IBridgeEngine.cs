using MozcInput.Protocol;

namespace MozcInput.Bridge.Engine;

internal interface IBridgeEngine
{
    MozcBridgeResponse Handle(MozcBridgeRequest request);
}
