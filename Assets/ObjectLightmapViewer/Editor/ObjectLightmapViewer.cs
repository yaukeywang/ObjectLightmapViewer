/**
 * The display of object uv area in lightmap class.
 *
 * @filename  ObjectLightmapViewer.cs
 * @copyright Copyright (c) 2015 Yaukey/yaukeywang/WangYaoqi (yaukeywang@gmail.com) all rights reserved.
 * @license   The MIT License (MIT)
 * @author    Yaukey
 * @date      2016-04-09
 */

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

// The display of object uv area in lightmap class.
public class ObjectLightmapViewer : EditorWindow
{
    // Used styles.
    private static class Styles
    {
        public static readonly GUIStyle LabelStyle = EditorStyles.wordWrappedMiniLabel;
        public static readonly GUIStyle ToolbarStyle = "preToolbar";
        public static readonly GUIStyle ToolbarTitleStyle = "preToolbar";
        public static readonly GUIStyle ButtonStyle = "LargeButton";
        public static readonly GUIStyle background = "preBackground";
    }

    // The constant for size.
    private static readonly float MAX_LIGHTMAP_SIZE = 256.0f;
    private static readonly float LIGHTMAP_LEFT_MARGIN = 5.0f;
    private static readonly float LIGHTMAP_RIGHT_MARGIN = 50.0f;
    private static readonly float MIN_WINDOW_SIZE = MAX_LIGHTMAP_SIZE + LIGHTMAP_LEFT_MARGIN + LIGHTMAP_RIGHT_MARGIN;
    private static readonly float SINGLE_LINGLE_GAP = 3.0f;

    // The scroll position.
    private Vector2 m_vScrollPosition = Vector2.zero;
    private Vector2 m_vScrollLightmap = Vector2.zero;

    // The separator line color.
    private Color m_clrSeparator = Color.black;

    // The uv rect frame color.
    private Color m_clrUVArea = Color.green;

    // Create priview lightmap uv things.
    private PreviewRenderUtility m_cPreviewUtility = null;
    private Material m_cMaterial = null;
    private Material m_cWireMaterial = null;
    private Mesh m_cFullQuadMesh = null;
    private Vector2 m_vPreviewDir = new Vector2(-120, 20);
    private float m_fPreviewSize = 0.0f;

    // Get reflection things.
    private MethodInfo m_miPrResizeHandle = null;
    private System.Object m_cPreviewResizerInstance = null;

    // The init method.
    [MenuItem("Tools/Object Lightmap Viewer")]
    private static void Init()
    {
        EditorWindow cWnd = GetWindow(typeof(ObjectLightmapViewer));
        cWnd.position = new Rect(100.0f, 100.0f, 500.0f, 600.0f);
        cWnd.minSize = new Vector2(MIN_WINDOW_SIZE, MIN_WINDOW_SIZE);
        cWnd.Show();
    }

    // The on gui event.
    private void OnGUI()
    {
        // Validate all the basic things.
        ValidateInitialize();

        // Draw lightmap overrall preview.
        Rect rcUVArea = position;
        Rect rcWindowArea = position;
        ProcessLightmapArea(true, out rcUVArea, out rcWindowArea);

        // Draw preview header.
        EditorGUILayout.BeginHorizontal(Styles.ToolbarStyle, GUILayout.Height(17));
        {
            GUILayout.FlexibleSpace();
            GUI.Label(GUILayoutUtility.GetLastRect(), "Preview", Styles.ToolbarTitleStyle);
        }

        EditorGUILayout.EndHorizontal();

        // Draw object uv area preview in lightmap.
        ProcessPreviewObjectUVInLightmap();
    }

    // Enable event.
    void OnEnable()
    {
        // Init internal settings.
        InitPreview();
        InitializeReflection();

        // Setup delegate.
        Selection.selectionChanged += OnSelectionChanged;
        Repaint();
    }

    // Disable event.
    void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    // The event for selection changed in scene or hierarchy window.
    private void OnSelectionChanged()
    {
        Rect rcUVArea = position;
        Rect rcWindowArea = position;
        if (ProcessLightmapArea(false, out rcUVArea, out rcWindowArea))
        {
            rcWindowArea.yMax -= m_fPreviewSize;
            if ((rcUVArea.yMax < rcWindowArea.y) || (rcUVArea.y > rcWindowArea.yMax))
            {
                m_vScrollPosition.y = rcUVArea.y - 1.0f;
            }
        }

        Repaint();
    }

    // Init preview data.
    private void InitPreview()
    {
        m_cPreviewUtility = new PreviewRenderUtility();
        m_cPreviewUtility.camera.fieldOfView = 30.0f;
        m_cPreviewUtility.camera.orthographic = true;
        m_cMaterial = CreatePreivewDefaultMaterial();
        m_cWireMaterial = CreatePreivewWireframeMaterial();
        m_cFullQuadMesh = CreatePreivewFullQuadMesh();
    }

    private void ValidateInitialize()
    {
        // Add all things that need validate here, changing scene may cause some assets need to be created again, e.g. Mesh.
        if (null == m_cFullQuadMesh)
        {
            m_cFullQuadMesh = CreatePreivewFullQuadMesh();
        }
    }

    // Create default material for preview.
    private Material CreatePreivewDefaultMaterial()
    {
        var cShader = Shader.Find("Hidden/ObjectLightmapViewer/Unlit/FullQuad");
        if (null == cShader)
        {
            Debug.LogWarning("ObjectLightmapViewer.CreatePreivewDefaultMaterial: Could not find preview default shader!");
            return null;
        }

        var cMat = new Material(cShader);
        cMat.hideFlags = HideFlags.HideAndDontSave;
        return cMat;
    }

    // Create wireframe material for preview.
    private Material CreatePreivewWireframeMaterial()
    {
        var cShader = Shader.Find("Hidden/ObjectLightmapViewer/Unlit/VisualizeUV");
        if (null == cShader)
        {
            Debug.LogWarning("ObjectLightmapViewer.CreatePreivewWireframeMaterial: Could not find preview wireframe shader!");
            return null;
        }

        var cMat = new Material(cShader);
        cMat.hideFlags = HideFlags.HideAndDontSave;
        cMat.SetColor("_Color", Color.blue);
        return cMat;
    }

    // Create a full quad mesh to show lightmap uv.
    private Mesh CreatePreivewFullQuadMesh()
    {
        Mesh cMesh = new Mesh();
        cMesh.name = "FullQuadMesh";

        Vector3[] aVertices = new Vector3[4];
        aVertices[0] = new Vector3(0.0f, 0.0f, 1.0f);
        aVertices[1] = new Vector3(0.0f, 1.0f, 1.0f);
        aVertices[2] = new Vector3(1.0f, 0.0f, 1.0f);
        aVertices[3] = new Vector3(1.0f, 1.0f, 1.0f);

        int[] aTriangles = new int[6];
        aTriangles[0] = 0;
        aTriangles[1] = 1;
        aTriangles[2] = 3;
        aTriangles[3] = 0;
        aTriangles[4] = 3;
        aTriangles[5] = 2;

        Vector2[] aUvs = new Vector2[4];
        aUvs[0] = new Vector2(0.0f, 0.0f);
        aUvs[1] = new Vector2(0.0f, 1.0f);
        aUvs[2] = new Vector2(1.0f, 0.0f);
        aUvs[3] = new Vector2(1.0f, 1.0f);

        cMesh.vertices = aVertices;
        cMesh.triangles = aTriangles;
        cMesh.uv = aUvs;

        return cMesh;
    }

    // Init reflection and get internal types we need.
    private void InitializeReflection()
    {
        // Get all loaded assemblies.
        Assembly[] aAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        Assembly cEditorAssembly = null;
        foreach (Assembly cAssembly in aAssemblies)
        {
            AssemblyName cAssemblyName = cAssembly.GetName();
            if ("UnityEditor" == cAssemblyName.Name)
            {
                cEditorAssembly = cAssembly;
                break;
            }
        }

        // Load assembly by path.
        if (null == cEditorAssembly)
        {
            Debug.LogError("Can not get assembly: UnityEditor.UI.dll");
            return;
        }

        // Get resizer type.
        Type cPreviewResizerType = cEditorAssembly.GetType("UnityEditor.PreviewResizer");

        // Get init method.
        MethodInfo miPrInit = cPreviewResizerType.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);

        // Get resize handler method.
        m_miPrResizeHandle = cPreviewResizerType.GetMethod("ResizeHandle", new Type[] { typeof(Rect), typeof(float), typeof(float), typeof(float) });

        // Get constructor and invoke.
        ConstructorInfo ctor = cPreviewResizerType.GetConstructor(new Type[] { });
        m_cPreviewResizerInstance = ctor.Invoke(null);

        // Invoke init method.
        miPrInit.Invoke(m_cPreviewResizerInstance, new object[] { "ObjectUVInLightmapPreview" });
    }

    // Perform a preview render.
    private void DoRenderObjectInLightmapPreview(Mesh cObjectMesh, Renderer cObjectRenderer)
    {
        if ((null == cObjectMesh) || (null == cObjectRenderer))
        {
            return;
        }

        LightmapData cLightmapData = LightmapSettings.lightmaps[cObjectRenderer.lightmapIndex];
        Texture2D cLightmapTex = (null != cLightmapData.lightmapColor) ? cLightmapData.lightmapColor : cLightmapData.lightmapDir;

        // Force texture filter to point.
        FilterMode eTextureFilter = cLightmapTex.filterMode;
        cLightmapTex.filterMode = FilterMode.Point;

        m_cMaterial.mainTexture = cLightmapTex;
        m_cMaterial.mainTextureOffset = new Vector2(cObjectRenderer.lightmapScaleOffset.z, cObjectRenderer.lightmapScaleOffset.w);
        m_cMaterial.mainTextureScale = new Vector2(cObjectRenderer.lightmapScaleOffset.x, cObjectRenderer.lightmapScaleOffset.y);

        // Draw preview.
        RenderMeshPreview(m_cFullQuadMesh, cObjectMesh, m_cPreviewUtility, m_cMaterial, m_cWireMaterial, m_vPreviewDir, -1);

        // Recover texture filter.
        cLightmapTex.filterMode = eTextureFilter;
    }

    // Render a mesh in preview with default and wireframe.
    private void RenderMeshPreview(
        Mesh cMesh,
        Mesh cMeshWire,
        PreviewRenderUtility cPreviewUtility,
        Material cLitMaterial,
        Material cWireMaterial,
        Vector2 vDirection,
        int nMeshSubset // -1 for whole mesh.
        )
    {
        if ((null == cMesh) || (null == cMeshWire) || (null == cPreviewUtility))
        {
            return;
        }

        // Setup preview camera common settings.
        cPreviewUtility.lights[0].intensity = 1.4f;
        cPreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
        cPreviewUtility.lights[1].intensity = 1.4f;
        cPreviewUtility.ambientColor = new Color(0.1f, 0.1f, 0.1f, 0.0f);

        // Render mesh with default material.
        Bounds cBounds = cMesh.bounds;
        float fHalfSize = cBounds.extents.magnitude;
        float fDistance = 4.0f * fHalfSize;

        cPreviewUtility.camera.transform.position = -Vector3.forward * fDistance;
        cPreviewUtility.camera.transform.rotation = Quaternion.identity;
        cPreviewUtility.camera.nearClipPlane = fDistance - fHalfSize * 1.1f;
        cPreviewUtility.camera.farClipPlane = fDistance + fHalfSize * 1.1f;
        
        RenderMeshPreviewSkipCameraAndLighting(cMesh, cBounds, cPreviewUtility, cLitMaterial, null, vDirection, nMeshSubset);

        // Render mesh with wireframe material.
        cBounds = cMeshWire.bounds;
        fHalfSize = cBounds.extents.magnitude;
        fDistance = 4.0f * fHalfSize;

        cPreviewUtility.camera.transform.position = -Vector3.forward * fDistance;
        cPreviewUtility.camera.transform.rotation = Quaternion.identity;
        cPreviewUtility.camera.nearClipPlane = fDistance - fHalfSize * 1.1f;
        cPreviewUtility.camera.farClipPlane = fDistance + fHalfSize * 1.1f;

        RenderMeshPreviewSkipCameraAndLightingWireframe(cMeshWire, cBounds, cPreviewUtility, cWireMaterial, null, vDirection, nMeshSubset);
    }

    // Render a mesh in preview with default material.
    private void RenderMeshPreviewSkipCameraAndLighting(
        Mesh cMesh,
        Bounds cBounds,
        PreviewRenderUtility cPreviewUtility,
        Material cLitMaterial,
        MaterialPropertyBlock cCustomProperties,
        Vector2 vDirection,
        int nMeshSubset // -1 for whole mesh.
        )
    {
        if ((null == cMesh) || (null == cPreviewUtility))
        {
            return;
        }

        Quaternion cRot = Quaternion.Euler(vDirection.y, 0, 0) * Quaternion.Euler(0, vDirection.x, 0);
        Vector3 vPos = cRot * (-cBounds.center);

        bool bOldFog = RenderSettings.fog;
        Unsupported.SetRenderSettingsUseFogNoDirty(false);

        int submeshes = cMesh.subMeshCount;
        if (null != cLitMaterial)
        {
            cPreviewUtility.camera.clearFlags = CameraClearFlags.Nothing;
            if ((nMeshSubset < 0) || (nMeshSubset >= submeshes))
            {
                for (int i = 0; i < submeshes; ++i)
                {
                    cPreviewUtility.DrawMesh(cMesh, vPos, cRot, cLitMaterial, i, cCustomProperties);
                }
            }
            else
            {
                cPreviewUtility.DrawMesh(cMesh, vPos, cRot, cLitMaterial, nMeshSubset, cCustomProperties);
            }

            cPreviewUtility.Render();
        }

        Unsupported.SetRenderSettingsUseFogNoDirty(bOldFog);
    }

    // Render a mesh in preview with wireframe material.
    private void RenderMeshPreviewSkipCameraAndLightingWireframe(
        Mesh cMeshWire,
        Bounds cBounds,
        PreviewRenderUtility cPreviewUtility,
        Material cWireMaterial,
        MaterialPropertyBlock cCustomProperties,
        Vector2 vDirection,
        int nMeshSubset // -1 for whole mesh.
        )
    {
        if ((null == cMeshWire) || (null == cPreviewUtility))
        {
            return;
        }

        Quaternion cRot = Quaternion.Euler(vDirection.y, 0, 0) * Quaternion.Euler(0, vDirection.x, 0);
        Vector3 vPos = cRot * (-cBounds.center);

        bool bOldFog = RenderSettings.fog;
        Unsupported.SetRenderSettingsUseFogNoDirty(false);

        int submeshes = cMeshWire.subMeshCount;
        if (null != cWireMaterial)
        {
            cPreviewUtility.camera.clearFlags = CameraClearFlags.Nothing;
            GL.wireframe = true;

            if ((nMeshSubset < 0) || (nMeshSubset >= submeshes))
            {
                for (int i = 0; i < submeshes; ++i)
                {
                    cPreviewUtility.DrawMesh(cMeshWire, vPos, cRot, cWireMaterial, i, cCustomProperties);
                }
            }
            else
            {
                cPreviewUtility.DrawMesh(cMeshWire, vPos, cRot, cWireMaterial, nMeshSubset, cCustomProperties);
            }

            cPreviewUtility.Render();
            GL.wireframe = false;
        }

        Unsupported.SetRenderSettingsUseFogNoDirty(bOldFog);
    }

    /**
     * To process the lightmap and the uv area calculation and display.
     * 
     * @param bool bOnGUI - Used in OnGUI or not, set false avoid to use the gui functions.
     * @param out Rect rcUVArea - Get the result of the selected object's lightmap uv area in the editor window.
     * @param out Rect rcWindowArea - Get the editor window size from the current scrolled position.
     * @return bool - If an object is selected and have a valid lightmap info, then return true, otherwise return false.
     */
    private bool ProcessLightmapArea(bool bOnGUI, out Rect rcUVArea, out Rect rcWindowArea)
    {
        // Init the result.
        rcUVArea = position;
        rcWindowArea = position;
        bool bResult = false;

        // The get rect plus an OnGUI param.
        Action<bool, float, float> GUILayoutUtility_GetRect = (bool bGUI, float fWidth, float fHeight) => 
        {
            if (bGUI)
            {
                GUILayoutUtility.GetRect(fWidth, fHeight);
            }
        };

        // The get aspect rect plus an OnGUI param.
        Func<bool, float, GUILayoutOption[], Rect> GUILayoutUtility_GetAspectRect = (bool bGUI, float fAspect, GUILayoutOption[] aOption) =>
        {
            Rect rcRes = new Rect();
            if (bGUI)
            {
                rcRes = GUILayoutUtility.GetAspectRect(fAspect, aOption);
            }

            return rcRes;
        };

        // The label field plus an OnGUI param.
        Action<bool, Rect, string> EditorGUI_LabelField = (bool bGUI, Rect rcArea, string strLabel) =>
        {
            if (bGUI)
            {
                EditorGUI.LabelField(rcArea, strLabel);
            }
        };

        // The draw rect plus an OnGUI param.
        Action<bool, Rect, Color> EditorGUI_DrawRect = (bool bGUI, Rect rcArea, Color clrArea) =>
        {
            if (bGUI)
            {
                EditorGUI.DrawRect(rcArea, clrArea);
            }
        };

        // The draw preview texture plus an OnGUI param.
        Action<bool, Rect, Texture2D> EditorGUI_DrawPreviewTexture = (bool bGUI, Rect rcArea, Texture2D cTex) => 
        {
            if (bGUI)
            {
                EditorGUI.DrawPreviewTexture(rcArea, cTex, null, ScaleMode.ScaleToFit);
            }
        };

        // The draw rect frame plus an OnGUI param.
        Action<bool, Rect, Color> EditorGUI_DrawRectFrame = (bool bGUI, Rect rcArea, Color clrArea) => 
        {
            if (bGUI)
            {
                DrawRectFrame(rcArea, clrArea);
            }
        };

        // Set a scroll view.
        if (bOnGUI)
        {
            m_vScrollPosition = EditorGUILayout.BeginScrollView(m_vScrollPosition);
        }

        // Get current active scene.
        UnityEngine.SceneManagement.Scene cCurScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if ((cCurScene.IsValid()) && !string.IsNullOrEmpty(cCurScene.name))
        {
            // Get scene name.
            string strActiveSceneName = cCurScene.name;

            // Get lightmap data.
            int nLightmapCount = LightmapSettings.lightmaps.Length;
            if (nLightmapCount >= 0)
            {
                // There is at least one lightmap, calc the base width and height.
                float fSingleLineHeight = EditorGUIUtility.singleLineHeight;
                float fWindowWidth = position.width;
                float fWindowHeight = position.height;
                float fLightmapSize = Mathf.Max(fWindowWidth - LIGHTMAP_LEFT_MARGIN - LIGHTMAP_RIGHT_MARGIN, MAX_LIGHTMAP_SIZE);

                // calculate the area to draw lightmap.
                Rect rcLmName = new Rect(LIGHTMAP_LEFT_MARGIN, SINGLE_LINGLE_GAP, fLightmapSize, fSingleLineHeight);
                Rect rcLmTips = new Rect(LIGHTMAP_LEFT_MARGIN, rcLmName.yMax, fLightmapSize, fSingleLineHeight);
                Rect rcSeparator = new Rect(LIGHTMAP_LEFT_MARGIN, rcLmTips.yMax + SINGLE_LINGLE_GAP, fLightmapSize, 1.0f);
                Rect rcLightmapNameBegin = new Rect(LIGHTMAP_LEFT_MARGIN, rcSeparator.yMax + SINGLE_LINGLE_GAP, fLightmapSize, fSingleLineHeight);
                Rect rcLightmapBegin = new Rect(LIGHTMAP_LEFT_MARGIN, rcLightmapNameBegin.yMax, fLightmapSize, fLightmapSize);
                Rect rcWindowSize = new Rect(0.0f, 0.0f, fWindowWidth, fWindowHeight);

                // Set the whole area layout size.
                GUILayoutUtility_GetRect(bOnGUI, fLightmapSize, rcLightmapNameBegin.y);

                // Draw current scene name, help tips and separator.
                EditorGUI_LabelField(bOnGUI, rcLmName, "Scene Name: " + strActiveSceneName);
                EditorGUI_LabelField(bOnGUI, rcLmTips, "Select object in scene to show area in lightmap.");
                EditorGUI_DrawRect(bOnGUI, rcSeparator, m_clrSeparator);

                // Iterate each lightmap, only draw lightmap-far.
                Rect rcLightmapName = rcLightmapNameBegin;
                Rect rcLightmap = rcLightmapBegin;
                Rect[] aLightmapAreas = new Rect[nLightmapCount];
                for (int i = 0; i < nLightmapCount; i++)
                {
                    // Show lightmap.
                    LightmapData cLd = LightmapSettings.lightmaps[i];
                    if (null != cLd)
                    {
                        // Get a valid lightmap but lightmap far is preferrd.
                        Texture2D cLightmap = (null != cLd.lightmapColor) ? cLd.lightmapColor : cLd.lightmapDir;
                        if (null != cLightmap)
                        {
                            // Get lightmap draw area.
                            GUILayoutOption[] aLdAreaOptions = new GUILayoutOption[]
                            {
                                GUILayout.MaxWidth(rcLightmapBegin.width),
                                GUILayout.MaxHeight(rcLightmapBegin.height)
                            };

                            // Draw lightmap name.
                            GUILayoutUtility_GetRect(bOnGUI, rcLightmapName.width, rcLightmapName.height);
                            EditorGUI_LabelField(bOnGUI, rcLightmapName, "Lightmap-" + i);

                            // Draw lightmap.
                            Rect rcLightmapCur = GUILayoutUtility_GetAspectRect(bOnGUI, (float)cLightmap.width / (float)cLightmap.height, aLdAreaOptions);
                            rcLightmapCur.x += LIGHTMAP_LEFT_MARGIN;
                            EditorGUI_DrawPreviewTexture(bOnGUI, rcLightmapCur, cLightmap);

                            // Add a line gap area.
                            GUILayoutUtility_GetRect(bOnGUI, rcLightmap.width, SINGLE_LINGLE_GAP);

                            // Get current area for the index.
                            aLightmapAreas[i] = rcLightmap;

                            // Recalculate the area for next lightmap.
                            rcLightmapName.y = rcLightmap.yMax + SINGLE_LINGLE_GAP;
                            rcLightmap.y = rcLightmapName.yMax;
                        }
                    }
                }

                // Not select any renderer in scene.
                if (Selection.transforms.Length > 0)
                {
                    // Show area on the light map.
                    Renderer cRr = Selection.transforms[0].gameObject.GetComponent<Renderer>();
                    if ((null != cRr) && (-1 != cRr.lightmapIndex))
                    {
                        // Caution the v is from bottom to the top.
                        Rect rcCurLightmap = aLightmapAreas[cRr.lightmapIndex];
                        Rect rcUV = new Rect(cRr.lightmapScaleOffset.z, 1.0f - (cRr.lightmapScaleOffset.w + cRr.lightmapScaleOffset.y), cRr.lightmapScaleOffset.x, cRr.lightmapScaleOffset.y);
                        Rect rcUVSize = new Rect(rcCurLightmap.x + rcUV.x * fLightmapSize, rcCurLightmap.y + rcUV.y * fLightmapSize, rcUV.width * fLightmapSize, rcUV.height * fLightmapSize);

                        // Check the boundary not overflow the lightmap area.
                        if (rcUVSize.x < rcCurLightmap.x)
                        {
                            rcUVSize.width -= rcCurLightmap.x - rcUVSize.x;
                            rcUVSize.x = rcCurLightmap.x;
                        }

                        if (rcUVSize.y < rcCurLightmap.y)
                        {
                            rcUVSize.height -= rcCurLightmap.y - rcUVSize.y;
                            rcUVSize.y = rcCurLightmap.y;
                        }

                        if (rcUVSize.xMax > rcCurLightmap.xMax)
                        {
                            rcUVSize.width -= rcUVSize.xMax - rcCurLightmap.xMax;
                        }

                        if (rcUVSize.yMax > rcCurLightmap.yMax)
                        {
                            rcUVSize.height -= rcUVSize.yMax - rcCurLightmap.yMax;
                        }

                        // Draw the uv rect frame.
                        EditorGUI_DrawRectFrame(bOnGUI, rcUVSize, m_clrUVArea);

                        // Set the out result.
                        rcUVArea = rcUVSize;
                        rcWindowArea = new Rect(0.0f, m_vScrollPosition.y, rcWindowSize.width, rcWindowSize.height);
                        bResult = true;
                    }
                }

                if (bOnGUI)
                {
                    EditorGUILayout.Space();
                }
            }
        }

        if (bOnGUI)
        {
            EditorGUILayout.EndScrollView();
        }

        return bResult;
    }

    // Process the object uv in lightmap to preview.
    private void ProcessPreviewObjectUVInLightmap()
    {
        if (Selection.transforms.Length <= 0)
        {
            return;
        }

        MeshFilter cMf = Selection.transforms[0].gameObject.GetComponent<MeshFilter>();
        if (null == cMf)
        {
            return;
        }

        Mesh cMesh = cMf.sharedMesh;
        if (null == cMesh)
        {
            return;
        }

        Renderer cRenderer = cMf.gameObject.GetComponent<Renderer>();
        if ((null == cRenderer) || (-1 == cRenderer.lightmapIndex) || (cRenderer.lightmapIndex >= LightmapSettings.lightmaps.Length))
        {
            return;
        }

        // Get preview size.
        m_fPreviewSize = (float)m_miPrResizeHandle.Invoke(m_cPreviewResizerInstance, new object[] { position, 100, 250, 17 });
        //Rect rcPreviewRect = new Rect(0.0f, position.height - m_fPreviewSize, position.width, m_fPreviewSize);

        if (m_fPreviewSize <= 0.0f)
        {
            return;
        }

        if (LightmapSettings.lightmaps.Length <= 0)
        {
            return;
        }

        // Get lightmap max size.
        float fLightmapSize = Mathf.Max(position.width - LIGHTMAP_LEFT_MARGIN - LIGHTMAP_RIGHT_MARGIN, MAX_LIGHTMAP_SIZE);
        Texture2D cLightmap = LightmapSettings.lightmaps[0].lightmapColor;
        GUILayoutOption[] aLdAreaOptions = new GUILayoutOption[] { GUILayout.MaxWidth(fLightmapSize), GUILayout.MaxHeight(fLightmapSize) };

        // Draw object uv area in lightmap.
        m_vScrollLightmap = EditorGUILayout.BeginScrollView(m_vScrollLightmap, GUILayout.Height(m_fPreviewSize));
        {
            // Draw label uv info.
            Vector4 vObjectLmArea = cRenderer.lightmapScaleOffset;
            EditorGUILayout.LabelField(string.Format("Object: {0}; Tiling XY: ({1}, {2}); Offset XY: ({3}, {4}).", cMf.gameObject.name, vObjectLmArea.x, vObjectLmArea.y, vObjectLmArea.z, vObjectLmArea.w));
            Rect rcLabel = GUILayoutUtility.GetLastRect();
            EditorGUILayout.Space();
            rcLabel.yMax += GUILayoutUtility.GetLastRect().height;

            // Calculate object uv area size in lightmap.
            Rect rcLightmapDraw = GUILayoutUtility.GetAspectRect((float)cLightmap.width / (float)cLightmap.height, aLdAreaOptions);
            rcLightmapDraw.x += LIGHTMAP_LEFT_MARGIN;
            Rect rcDrawPreview = new Rect(rcLightmapDraw.x, rcLabel.height, rcLightmapDraw.width, rcLightmapDraw.height);

            // Perform preview render.
            m_cPreviewUtility.BeginPreview(rcDrawPreview, Styles.background);
            DoRenderObjectInLightmapPreview(cMesh, cRenderer);
            m_cPreviewUtility.EndAndDrawPreview(rcDrawPreview);
        }

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
    }

    /**
     * Draw a rect frame for a given area and color.
     * 
     * @param Rect rcArea - The rect frame area.
     * @param Color clrArea - The rect frame color.
     * @return void.
     */
    private void DrawRectFrame(Rect rcArea, Color clrArea)
    {
        // Calculate the border line.
        Rect rcTop = new Rect(rcArea.x, rcArea.y, rcArea.width, 1.0f);
        Rect rcBottom = new Rect(rcArea.x, rcArea.yMax - 1.0f, rcArea.width, 1.0f);
        Rect rcLeft = new Rect(rcArea.x, rcArea.y, 1.0f, rcArea.height);
        Rect rcRight = new Rect(rcArea.xMax - 1.0f, rcArea.y, 1.0f, rcArea.height);

        // Draw lines.
        EditorGUI.DrawRect(rcTop, clrArea);
        EditorGUI.DrawRect(rcBottom, clrArea);
        EditorGUI.DrawRect(rcLeft, clrArea);
        EditorGUI.DrawRect(rcRight, clrArea);
    }
}
