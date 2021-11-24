#ifndef POI_DECAL
#define POI_DECAL
#if defined(PROP_DECALMASK) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_DecalMask);
#endif
#if defined(PROP_DECALTEXTURE) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_DecalTexture);
#else
    float2 _DecalTextureUV;
#endif
float4 _DecalColor;
fixed _DecalTiled;
float _DecalBlendType;
half _DecalRotation;
half2 _DecalScale;
half2 _DecalPosition;
half _DecalRotationSpeed;
float _DecalEmissionStrength;
float _DecalBlendAlpha;
float _DecalHueShiftEnabled;
float _DecalHueShift;
float _DecalHueShiftSpeed;
half _AudioLinkDecal0ScaleBand;
float4 _AudioLinkDecal0Scale;
half _AudioLinkDecal0RotationBand;
float2 _AudioLinkDecal0Rotation;
half _AudioLinkDecal0AlphaBand;
float2 _AudioLinkDecal0Alpha;
half _AudioLinkDecal0EmissionBand;
float2 _AudioLinkDecal0Emission;
half _AudioLinkDecal1ScaleBand;
float4 _AudioLinkDecal1Scale;
half _AudioLinkDecal1RotationBand;
float2 _AudioLinkDecal1Rotation;
half _AudioLinkDecal1AlphaBand;
float2 _AudioLinkDecal1Alpha;
half _AudioLinkDecal1EmissionBand;
float2 _AudioLinkDecal1Emission;
half _AudioLinkDecal2ScaleBand;
float4 _AudioLinkDecal2Scale;
half _AudioLinkDecal2RotationBand;
float2 _AudioLinkDecal2Rotation;
half _AudioLinkDecal2AlphaBand;
float2 _AudioLinkDecal2Alpha;
half _AudioLinkDecal2EmissionBand;
float2 _AudioLinkDecal2Emission;
half _AudioLinkDecal3ScaleBand;
float4 _AudioLinkDecal3Scale;
half _AudioLinkDecal3RotationBand;
float2 _AudioLinkDecal3Rotation;
half _AudioLinkDecal3AlphaBand;
float2 _AudioLinkDecal3Alpha;
half _AudioLinkDecal3EmissionBand;
float2 _AudioLinkDecal3Emission;
float _Decal0Depth;
float _Decal1Depth;
float _Decal2Depth;
float _Decal3Depth;
float2 calcParallax(float height)
{
    return((height * - 1) + 1) * (poiCam.decalTangentViewDir.xy / poiCam.decalTangentViewDir.z);
}
float2 decalUV(float uvNumber, float2 position, half rotation, half rotationSpeed, half2 scale, float depth)
{
    float2 uv = poiMesh.uv[uvNumber] + calcParallax(depth + 1);
    float2 decalCenter = position;
    float theta = radians(rotation + _Time.z * rotationSpeed);
    float cs = cos(theta);
    float sn = sin(theta);
    uv = float2((uv.x - decalCenter.x) * cs - (uv.y - decalCenter.y) * sn + decalCenter.x, (uv.x - decalCenter.x) * sn + (uv.y - decalCenter.y) * cs + decalCenter.y);
    uv = remap(uv, float2(0, 0) - scale / 2 + position, scale / 2 + position, float2(0, 0), float2(1, 1));
    return uv;
}
inline float3 decalHueShift(float enabled, float3 color, float shift, float shiftSpeed)
{
    
    if (enabled)
    {
        color = hueShift(color, shift + _Time.x * shiftSpeed);
    }
    return color;
}
inline float applyTilingClipping(float enabled, float2 uv)
{
    float ret = 1;
    
    if (!enabled)
    {
        if (uv.x > 1 || uv.y > 1 || uv.x < 0 || uv.y < 0)
        {
            ret = 0;
        }
    }
    return ret;
}
void applyDecals(inout float4 albedo, inout float3 decalEmission)
{
    #if defined(PROP_DECALMASK) || !defined(OPTIMIZER_ENABLED)
        float4 decalMask = POI2D_SAMPLER_PAN(_DecalMask, _MainTex, poiMesh.uv[float(0)], float4(0,0,0,0));
    #else
        float4 decalMask = 1;
    #endif
    float4 decalColor = 1;
    float2 uv = 0;
    float2 decalScale = float2(1, 1);
    float decalRotation = 0;
    decalScale = float4(1,1,0,0);
    #if defined(PROP_DECALTEXTURE) || !defined(OPTIMIZER_ENABLED)
        #ifdef POI_AUDIOLINK
            
            if (poiMods.audioLinkTextureExists)
            {
                decalScale += lerp(float4(0,0,0,0).xy, float4(0,0,0,0).zw, poiMods.audioLink[float(0)]);
                decalRotation += lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[float(0)]);
            }
        #endif
        uv = decalUV(float(0), float4(0.5,0.5,0,0), float(0) + decalRotation, float(0), decalScale, float(0));
        decalColor = POI2D_SAMPLER_PAN(_DecalTexture, _MainTex, uv, float4(0,0,0,0)) * float4(1,1,1,1);
    #else
        uv = decalUV(float(0), float4(0.5,0.5,0,0), float(0) + decalRotation, float(0), decalScale, float(0));
        decalColor = float4(1,1,1,1);
    #endif
    decalColor.rgb = decalHueShift(float(0), decalColor.rgb, float(0), float(0));
    decalColor.a *= applyTilingClipping(float(0), uv) * decalMask.r;
    float audioLinkDecalAlpha0 = 0;
    #ifdef POI_AUDIOLINK
        audioLinkDecalAlpha0 = lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[float(0)]) * poiMods.audioLinkTextureExists;
    #endif
    albedo.rgb = lerp(albedo.rgb, customBlend(albedo.rgb, decalColor.rgb, float(0)), decalColor.a * saturate(_DecalBlendAlpha + audioLinkDecalAlpha0));
    float audioLinkDecalEmission0 = 0;
    #ifdef POI_AUDIOLINK
        audioLinkDecalEmission0 = lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[float(0)]) * poiMods.audioLinkTextureExists;
    #endif
    decalEmission += decalColor.rgb * decalColor.a * max(float(0) + audioLinkDecalEmission0, 0);
    albedo = saturate(albedo);
}
#endif
