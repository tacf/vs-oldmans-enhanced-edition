using System.Drawing;
using System.Linq;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace OldMansEnhancedEdition.Features.UI.Widgets;

#nullable disable
public class HungerBuffCounter : HudElement
{
    private long _listenerId;
    private float _desiredTarget;
    private float _currentValue;
    private float _maxDelay;
    private GuiElementStatbar _statbar;
    public HungerBuffCounter(ICoreClientAPI capi) : base(capi)
    {
        CreateGui();
    }

    public override double DrawOrder => 0.06;
    public override float ZSize => 175f; // just to ensure it's behind the standard GUI Elements default: 150
    public override bool ShouldReceiveMouseEvents() => false;
    public override bool ShouldReceiveKeyboardEvents() => false;

    private void CreateGui()
    {
        const float statsBarParentWidth = 850f;
        const float statsBarWidth = 349f;
    
        ElementBounds statsBarBounds = new ElementBounds()
        {
            Alignment = EnumDialogArea.CenterBottom,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = statsBarParentWidth,
            fixedHeight = 100
        }.WithFixedAlignmentOffset(-0.2, 5.0);
        
        ElementBounds hungerBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightTop, statsBarWidth)
            .WithFixedAlignmentOffset(-0.2, 0)
            .WithFixedHeight(10);
    
        ElementBounds hungerBarParentBounds = statsBarBounds.FlatCopy().FixedGrow(0.0, 20.0);
    
        GuiComposer composer = capi.Gui.CreateCompo("hungercooldown-statbar", hungerBarParentBounds);
    
        _statbar = new GuiElementStatbar(composer.Api, hungerBarBounds, GuiStyle.FoodBarColor, true, false);
        _statbar.ShowValueOnHover = false;
        _statbar.SetLineInterval(100f);
        _statbar.SetValue(100f);
    
        SingleComposer = composer
            .BeginChildElements(statsBarBounds)
            .AddInteractiveElement(_statbar, "hunger-cd-bar")
            .EndChildElements()
            .Compose();
    }
    
    public void UpdateBuffCounter(float timeLeft)
    {
        //Logger.Debug("Updating buff counter - " + timeLeft + " hunger rate blended: " + capi.World.Player.Entity.Stats.GetBlended("hungerrate"));
        
         if (_maxDelay <= 0 && timeLeft >= 0)
        {
            _maxDelay = timeLeft ;
            _currentValue = 100f;
            _desiredTarget = 100f;
        }
        else
        {
            _desiredTarget = (timeLeft * 100 / _maxDelay);
            _currentValue = float.Clamp(float.Lerp(_desiredTarget, _currentValue, 0.5f), 0, 100.0f);
        }
         
        // Logger.Debug($"Desired {_desiredTarget}, Current {_currentValue}, Max {_maxDelay}");
        
        _statbar.SetValue(_currentValue);
        
        if (_currentValue <= 1.0f && _listenerId == 0 && IsOpened())
        {
            _listenerId = capi.Event.RegisterCallback((f =>
            {
                TryClose();
                _listenerId = 0;
                _maxDelay = 0;
                _statbar?.SetValue(100f);
            }), 500);
        }
        else if (timeLeft > 0 && !IsOpened())
        {
            TryOpen();
        }
    }

    private ElementBounds GetSaturationBarBounds()
    {
        return capi.OpenedGuis.OfType<HudStatbar>().First().Composers.First().Value.GetElement("saturationstatbar").Bounds;
    }
}