using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OldMansEnhancedEdition.Features.UI.Widgets;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

#nullable disable

namespace OldMansEnhancedEdition.Features.UI;

public class HungerCooldownBuff(ICoreClientAPI capi) : IFeature
{
    private IClientPlayer _player;
    private HungerBuffCounter _hudElement;
    private long _playerAwaitListenerId;
    private long _playerHugenCheckListenerId;

    public EnumAppSide Side => EnumAppSide.Universal;


    public bool Initialize()
    {
        _playerAwaitListenerId = capi.Event.RegisterGameTickListener(CheckPlayerReady, 200);
        Logger.Debug("Waiting for player to be ready to initialize Hunger CD buff Indicator");
        return true;
    }

    public void Teardown()
    {
        capi.Event.UnregisterGameTickListener(_playerAwaitListenerId);
        capi.Event.UnregisterGameTickListener(_playerHugenCheckListenerId);
    }

    private void CheckPlayerReady(float dt)
    {
        if (capi.PlayerReadyFired)
        {
            _player = capi.World.Player;
            _hudElement = new HungerBuffCounter(capi);
            _playerHugenCheckListenerId = capi.Event.RegisterGameTickListener(CheckHungerBuff, 1000);
            capi.Event.UnregisterGameTickListener(_playerAwaitListenerId);
        }
    }

    
    private void CheckHungerBuff(float timePassed)
    {
        float remainingTime = GetRemainingHungerCooldown(_player);
        if (remainingTime >=0)
            _hudElement.UpdateBuffCounter(remainingTime);
    }

   private float GetRemainingHungerCooldown(IPlayer player)
   {
       ITreeAttribute hungerAttrb = player.Entity.WatchedAttributes.GetAttribute("hunger") as ITreeAttribute;
       if (hungerAttrb == null) return 0;
       IEnumerator<KeyValuePair<string, IAttribute>> enumerator = hungerAttrb.GetEnumerator();
       List<float> delay = new ();
       delay.Add(0);
       while (enumerator.MoveNext())
       {
           if (enumerator.Current.Key.StartsWith("saturationlossdelay"))
           {
               float satDelayLossValue = (float)enumerator.Current.Value.GetValue();
               delay.Add(satDelayLossValue > 0 ? satDelayLossValue : 0);
               if (satDelayLossValue > 0)
               {
                   Logger.Debug($"{enumerator.Current.Key}: {enumerator.Current.Value}");
               }
           }
       }
       enumerator.Dispose();

       return (delay.Count > 0 ? delay.Max() : 0);
   }
}