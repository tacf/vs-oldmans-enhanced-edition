namespace OldMansEnhancedEdition.Utils;

class ModConfig
{
    public static ModConfig Instance { get; set; } = new ModConfig();

    /// <summary>
    /// Prevent dead crops from drop
    /// </summary>
    public bool DeadCropsDontDropSeeds { get; set; } = true;
    
    /// <summary>
    /// Show Interaction Progress Overlay.
    /// </summary>
    public bool InteractionProgresOverlay { get; set; } = true;
}