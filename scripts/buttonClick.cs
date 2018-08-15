using UnityEngine;
using System.Collections;

public class buttonClick : MonoBehaviour
{
    protected bool switchFlag = true;
    public GameObject moder;
    private Color[] moderColorAry;
    private Material[] moderAry;
    // Use this for initialization
    public enum RenderingMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent,
    }

    public static void SetMaterialRenderingMode(Material material, RenderingMode renderingMode)
    {
        switch (renderingMode)
        {
            case RenderingMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case RenderingMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case RenderingMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case RenderingMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }
    void Start()
    {
        switchFlag = false;
        if (moder != null)
        {
            Material[] mr = moder.GetComponent<MeshRenderer>().materials;
            moderColorAry = new Color[mr.Length];
            moderAry = new Material[mr.Length];
            for(int index =0; index < mr.Length; ++index)
            {
                moderColorAry[index] = mr[index].color;
                moderAry[index] = mr[index];
            }
            
        }
    }

    void OnClick()
    {
        if (this.switchFlag)
        {
            this.GetComponent<UISprite>().spriteName = "off";
            this.GetComponent<UIButton>().normalSprite = "off";
            this.switchFlag = false;
            if(moder != null)
            {
                for(int index = 0; index < moderAry.Length; ++index)
                {
                    moderAry[index].color = moderColorAry[index];
                    SetMaterialRenderingMode(moderAry[index], RenderingMode.Opaque);
                }
               
            }
      
        }
        else
        {
            this.GetComponent<UISprite>().spriteName = "on";
            this.GetComponent<UIButton>().normalSprite = "on";
            this.switchFlag = true;
            if (moder != null)
            {
                for (int index = 0; index < moderAry.Length; ++index)
                {
                    moderAry[index].color = new Color(moderColorAry[index].r, moderColorAry[index].g, moderColorAry[index].b,0.5f);
                    SetMaterialRenderingMode(moderAry[index], RenderingMode.Transparent);
                }
            }
        }
    }
}