using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace OldMansEnhancedEdition.Utils;

public class Notifier
{
    private ICoreAPI _api;
    private long _ingameErrorCallbackId;

    public Notifier(ICoreAPI api)
    {
        _api = api;
    }
    public void SendPlayerWarning(string code, string message)
    {
        if (_api.Side == EnumAppSide.Client)
        {
            ICoreClientAPI? client = _api as ICoreClientAPI;
            if (client == null) return;
            _ingameErrorCallbackId = client!.World.RegisterCallbackUnique((worldAccessor, _, _) =>
            {
                (_api as ICoreClientAPI)?.TriggerIngameError(this, code, Lang.Get(message));
                worldAccessor.UnregisterCallback(_ingameErrorCallbackId);
            }, client.World.Player.Entity.Pos.AsBlockPos, 100);

        }
    }
}