using System;
using Vintagestory.API.Common;

namespace OldMansEnhancedEdition.Features;

public interface IFeature 
{
    public EnumAppSide Side { get; }
    public bool Initialize(ICoreAPI api);
    
    public void Teardown();
}