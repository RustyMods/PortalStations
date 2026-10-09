using System.Collections.Generic;
using HarmonyLib;
using PieceManager;
using UnityEngine;

namespace PortalStations;

public class PieceEffectSetter
{
    private static List<PieceEffectSetter> setters = [];
    
    public GameObject prefab;
    public Piece piece;
    public WearNTear wnt;
    public string ClonePortalSFXFrom;

    public PieceEffectSetter(BuildPiece bp)
    {
        prefab = bp.Prefab;
        piece = prefab.GetComponent<Piece>();
        wnt = prefab.GetComponent<WearNTear>();
        setters.Add(this);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetSceneAwakePatch
    {
        private static void Postfix()
        {
            for (int i = 0; i < setters.Count; ++i)
            {
                var setter = setters[i];
                setter.Load();
            }   
        }
    }
    public void Load()
    {
        AddPortalSFX();
    }
    
    private void AddPortalSFX()
    {
        if (ZNetScene.instance && ZNetScene.instance.GetPrefab(ClonePortalSFXFrom) is { } original && Utils.FindChild(original.transform, "SFX") is {} SFX && SFX.TryGetComponent(out AudioSource source) 
            && Utils.FindChild(prefab.transform, "SFX") is {} newSFX)
        {
            AudioSource newSource = newSFX.gameObject.AddComponent<AudioSource>();
            newSource.clip = source.clip;
            newSource.outputAudioMixerGroup = source.outputAudioMixerGroup;
            newSource.mute = source.mute;
            newSource.bypassEffects = source.bypassEffects;
            newSource.bypassReverbZones = source.bypassReverbZones;
            newSource.playOnAwake = source.playOnAwake;
            newSource.loop = source.loop;
            newSource.priority = source.priority;
            newSource.volume = PortalStationsPlugin.PortalVolume;
            newSource.pitch = source.pitch;
            newSource.panStereo = source.panStereo;
            newSource.spatialBlend = source.spatialBlend;
            newSource.reverbZoneMix = source.reverbZoneMix;
            newSource.dopplerLevel = source.dopplerLevel;
            newSource.spread = source.spread;
            newSource.rolloffMode = source.rolloffMode;
            newSource.minDistance = source.minDistance;
            newSource.maxDistance = source.maxDistance;
        }
    }
}

public static class Ext
{
    public static PieceEffectSetter GetPieceEffectSetter(this BuildPiece bp) => new PieceEffectSetter(bp);
}