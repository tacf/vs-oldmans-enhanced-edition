using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace OldMansEnhancedEdition.Features.Mechanics;

#nullable disable
public class CTRLStopOnLadders : IFeature
{
    public EnumAppSide Side => EnumAppSide.Universal;
    private ICoreClientAPI _api;


    public CTRLStopOnLadders(ICoreAPI api)
    {
        _api = (api as ICoreClientAPI)!;
    }
    
    public bool Initialize()
    {
        _api.Input.InWorldAction += CTRLHoldsPlayerOnLadders;
        return true;
    }
    
    
    private void CTRLHoldsPlayerOnLadders(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        EntityControls pControls = _api.World.Player.Entity.Controls;
        if ((action is EnumEntityAction.Sneak) && pControls.CtrlKey)
        {
            if (pControls.RightMouseDown || pControls.LeftMouseDown)
            {
                handled = EnumHandling.PreventDefault;
            }
            else
            {
                handled = EnumHandling.PreventSubsequent;
            }
        }
    }
    
    public void Teardown()
    {
        _api.Input.InWorldAction -= CTRLHoldsPlayerOnLadders;
    }
}