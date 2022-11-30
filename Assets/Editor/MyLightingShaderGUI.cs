using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class MyLightingShaderGUI : ShaderGUI
{
    enum RenderingMode
    {
        Opaque, Cutout, Fade, Transparent
    }

    enum SmoothnessSource
    {
        Uniform, Albedo, Metallic
    }

    struct RenderingSettings
    {
        public RenderQueue queue;
        public string renderType;
        public BlendMode srcBlend, dstBlend;
        public bool zWrite;

        static public RenderingSettings[] modes =
        {
            new RenderingSettings()
            {
                queue = RenderQueue.Geometry,
                renderType = "",
                srcBlend = BlendMode.One,
                dstBlend = BlendMode.Zero,
                zWrite = true
            },

            new RenderingSettings()
            {
                queue = RenderQueue.AlphaTest,
                renderType = "TransparentCutout",
                srcBlend = BlendMode.One,
                dstBlend = BlendMode.Zero,
                zWrite = true
            },

            new RenderingSettings()
            {
                queue = RenderQueue.Transparent,
                renderType = "Transparent",
                srcBlend = BlendMode.SrcAlpha,
                dstBlend = BlendMode.OneMinusSrcAlpha,
                zWrite = false
            },

            new RenderingSettings()
            {
                queue = RenderQueue.Transparent,
                renderType = "Transparent",
                srcBlend = BlendMode.One,
                dstBlend = BlendMode.OneMinusSrcAlpha,
                zWrite = false
            }
        };
    }

    static private GUIContent staticLabel = new GUIContent();

    private Material target;
    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;

    private bool shouldShowAlphaCutoff;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        target = materialEditor.target as Material;
        this.materialEditor = materialEditor;
        this.properties = properties;

        CreateRenderingMode();
        CreateMain();
        CreateSecondary();
    }

    static private GUIContent CreateLabel(string text, string tooltip = null)
    {
        staticLabel.text = text;
        staticLabel.tooltip = tooltip;

        return staticLabel;
    }

    static private GUIContent CreateLabel(MaterialProperty property, string tooltip = null)
    {
        staticLabel.text = property.displayName;
        staticLabel.tooltip = tooltip;

        return staticLabel;
    }

    private MaterialProperty FindProperty(string name)
    {
        return FindProperty(name, properties);
    }

    private void SetKeyword(string keyword, bool state)
    {
        if(state)
        {
            foreach(Material m in materialEditor.targets)
            {
                target.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach(Material m in materialEditor.targets)
            {
                target.DisableKeyword(keyword);
            }
        }
    }

    private bool IsKeywordEnabled(string keyword)
    {
        return target.IsKeywordEnabled(keyword);
    }

    private void RecordAction(string label)
    {
        materialEditor.RegisterPropertyChangeUndo(label);
    }

    private void CreateSemiTransparentShadows()
    {
        EditorGUI.BeginChangeCheck();
        bool semitransparentShadows = EditorGUILayout.Toggle(CreateLabel("Semitransp. Shadows", "Semitransparent Shadows"), IsKeywordEnabled("_SEMITRANSPARENT_SHADOWS"));


        if(EditorGUI.EndChangeCheck())
        {
            SetKeyword("_SEMITRANSPARENT_SHADOWS", semitransparentShadows);
        }

        if(!semitransparentShadows)
        {
            shouldShowAlphaCutoff = true;
        }
    }

    private void CreateRenderingMode()
    {
        RenderingMode mode = RenderingMode.Opaque;
        shouldShowAlphaCutoff = false;
        
        if(IsKeywordEnabled("_RENDERING_CUTOUT"))
        {
            mode = RenderingMode.Cutout;
            shouldShowAlphaCutoff = true;
        }
        else if(IsKeywordEnabled("_RENDERING_FADE"))
        {
            mode = RenderingMode.Fade;
        }
        else if(IsKeywordEnabled("_RENDERING_TRANSPARENT"))
        {
            mode = RenderingMode.Transparent;
        }

        EditorGUI.BeginChangeCheck();
        mode = (RenderingMode)EditorGUILayout.EnumPopup(CreateLabel("Rendering Mode"), mode);

        if(EditorGUI.EndChangeCheck())
        {
            RecordAction("Rendering Mode");
            SetKeyword("_RENDERING_CUTOUT", mode == RenderingMode.Cutout);
            SetKeyword("_RENDERING_FADE", mode == RenderingMode.Fade);
            SetKeyword("_RENDERING_TRANSPARENT", mode == RenderingMode.Transparent);

            RenderingSettings settings = RenderingSettings.modes[(int)mode];
            foreach(Material m in materialEditor.targets)
            {
                m.renderQueue = (int)settings.queue;
                m.SetOverrideTag("RenderType", settings.renderType);
                m.SetInt("_SrcBlend", (int)settings.srcBlend);
                m.SetInt("_DstBlend", (int)settings.dstBlend);
                m.SetInt("_ZWrite", settings.zWrite ? 1 : 0);
            }
        }

        if(mode == RenderingMode.Fade || mode == RenderingMode.Transparent)
        {
            CreateSemiTransparentShadows();
        }
    }

    private void CreateMain()
    {
        GUILayout.Label("Main Maps", EditorStyles.boldLabel);

        MaterialProperty mainTex = FindProperty("_MainTex");
        materialEditor.TexturePropertySingleLine(CreateLabel(mainTex, "Albedo (RGB)"), mainTex, FindProperty("_Tint"));

        if(shouldShowAlphaCutoff)
        {
            CreateAlphaCutoff();
        }

        CreateMetallic();
        CreateSmoothness();
        CreateNormals();
        CreateOcclusion();
        CreateEmission();
        CreateDetailMask();

        materialEditor.TextureScaleOffsetProperty(mainTex);
    }

    private void CreateSecondary()
    {
        GUILayout.Label("Secondary Maps", EditorStyles.boldLabel);

        MaterialProperty detailTex = FindProperty("_DetailTex");
        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(detailTex, "Detail (RGB)"), detailTex);

        if(EditorGUI.EndChangeCheck())
        {
            SetKeyword("_DETAIL_ALBEDO_MAP", detailTex.textureValue);
        }

        CreateSecondaryNormals();

        materialEditor.TextureScaleOffsetProperty(detailTex);
    }

    private void CreateAlphaCutoff()
    {
        MaterialProperty slider = FindProperty("_AlphaCutoff");

        EditorGUI.indentLevel += 2;

        materialEditor.ShaderProperty(slider, CreateLabel(slider));

        EditorGUI.indentLevel -= 2;
    }

    private void CreateNormals()
    {
        MaterialProperty map = FindProperty("_NormalMap");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(map, "Normal (RGB)"), map, tex ? FindProperty("_BumpScale") : null);

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_NORMAL_MAP", map.textureValue);
        }
    }

    private void CreateSecondaryNormals()
    {
        MaterialProperty map = FindProperty("_DetailNormalMap");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(map, "Detail Normal (RGB)"), map, tex ? FindProperty("_DetailBumpScale") : null);

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_DETAIL_NORMAL_MAP", map.textureValue);
        }
    }

    private void CreateMetallic()
    {
        MaterialProperty map = FindProperty("_MetallicMap");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(map, "Metallic (R)"), map, tex ? null : FindProperty("_Metallic"));

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_METALLIC_MAP", map.textureValue);
        }
    }

    private void CreateSmoothness()
    {
        SmoothnessSource source = SmoothnessSource.Uniform;
        
        if(IsKeywordEnabled("_SMOOTHNESS_ALBEDO"))
        {
            source = SmoothnessSource.Albedo;
        }
        else if(IsKeywordEnabled("_SMOOTHNESS_METALLIC"))
        {
            source = SmoothnessSource.Metallic;
        }

        EditorGUI.indentLevel += 2;

        MaterialProperty slider = FindProperty("_Smoothness");
        materialEditor.ShaderProperty(slider, CreateLabel(slider));

        EditorGUI.indentLevel += 1;

        EditorGUI.BeginChangeCheck();
        source = (SmoothnessSource)EditorGUILayout.EnumPopup(CreateLabel("Source"), source);
        
        if(EditorGUI.EndChangeCheck())
        {
            RecordAction("Smoothness Source");
            SetKeyword("_SMOOTHNESS_ALBEDO", source == SmoothnessSource.Albedo);
            SetKeyword("_SMOOTHNESS_METALLIC", source == SmoothnessSource.Metallic);
        }

        EditorGUI.indentLevel -= 3;
    }

    private void CreateOcclusion()
    {
        MaterialProperty map = FindProperty("_OcclusionMap");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(map, "Occlusion (G)"), map, tex ? FindProperty("_OcclusionStrength") : null);

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_OCCLUSION_MAP", map.textureValue);
        }
    }

    private void CreateEmission()
    {
        MaterialProperty map = FindProperty("_EmissionMap");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertyWithHDRColor(CreateLabel(map, "Emission (RGB)"), map, FindProperty("_Emission"), false);

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_EMISSION_MAP", map.textureValue);
        }
    }

    private void CreateDetailMask()
    {
        MaterialProperty map = FindProperty("_DetailMask");
        Texture tex = map.textureValue;

        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(CreateLabel(map, "Detail Mask (A)"), map);

        if(EditorGUI.EndChangeCheck() && tex != map.textureValue)
        {
            SetKeyword("_DETAIL_MASK", map.textureValue);
        }
    }
}
