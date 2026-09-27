using UnityEngine;

// Asset references the runtime atmosphere needs. Lives in Resources so the bootstrap can load it in any scene
// (and so the sprites/materials are guaranteed to ship in the WebGL build).
public class JamVisualsConfig : ScriptableObject
{
    public Sprite background;
    public Sprite chainLinkA;
    public Sprite chainLinkB;
    public Sprite crash;
    public Material litSprite;
    public Material unlitSprite;
}
