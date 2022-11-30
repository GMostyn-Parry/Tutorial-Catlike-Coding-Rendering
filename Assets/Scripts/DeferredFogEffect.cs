using UnityEngine;
using System;

[ExecuteInEditMode]
public class DeferredFogEffect : MonoBehaviour
{
    public Shader deferredFog;

    [NonSerialized] private Material fogMaterial;
    [NonSerialized] private Camera deferredCamera;
    [NonSerialized] private Vector3[] frustrumCorners;
    [NonSerialized] private Vector4[] vectorArray;

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(fogMaterial == null)
        {
            fogMaterial = new Material(deferredFog);

            deferredCamera = GetComponent<Camera>();
            frustrumCorners = new Vector3[4];

            vectorArray = new Vector4[4];
        }

        deferredCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), deferredCamera.farClipPlane, deferredCamera.stereoActiveEye, frustrumCorners);

        vectorArray[0] = frustrumCorners[0];
        vectorArray[1] = frustrumCorners[3];
        vectorArray[2] = frustrumCorners[1];
        vectorArray[3] = frustrumCorners[2];
        fogMaterial.SetVectorArray("_FrustrumCorners", vectorArray);

        Graphics.Blit(source, destination, fogMaterial);
    }
}
