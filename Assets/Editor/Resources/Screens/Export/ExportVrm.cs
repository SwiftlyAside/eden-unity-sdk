using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Resources.Components;
using Editor.Scripts;
using Editor.Scripts.Manager;
using Editor.Scripts.Struct;
using lilToon;
using UniGLTF;
using UniGLTF.MeshUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UniVRM10;
using VrmLib;
using VRMShaders;
using Object = UnityEngine.Object;
using ScrollView = UnityEngine.UIElements.ScrollView;

namespace Editor.Resources.Screens.Export
{
    public class ExportVrm : EditorWindow
    {
        private class TempDisposable : IDisposable
        {
            private List<Object> _disposables = new();

            public void Add(Object obj)
            {
                _disposables.Add(obj);
            }

            public void Dispose()
            {
                foreach (var obj in _disposables)
                {
                    DestroyImmediate(obj);
                }

                _disposables.Clear();
            }
        }

        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;
        private static VisualElement _container;
        private static Button _exportButton;
        private static Button _backButton;
        private static Button _exportVrmButton;
        private static TextField _nameField;
        private static TextField _authorField;
        private static TextField _versionField;
        private static Preview _preview;

        private static Button _happyButton;
        private static Button _angryButton;
        private static Button _sadButton;
        private static Button _relaxedButton;
        private static Button _surprisedButton;

        private static VisualElement _expressionPanel;
        private static Button _expressionCloseButton;
        private static Label _expressionLabel;
        private static ScrollView _expressionScrollView;
        private static List<string> _blendShapeKeys = new();

        // 쉐이프키 이름과 skinnedMeshRenderer의 blendShapeIndex를 매핑
        private static Dictionary<string, BlendShapeData> _blendShapeDataMap = new();

        public static void Show(VisualElement root, Action onBackClicked)
        {
            _container = root;
            // clear the container
            _container.Clear();
            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Screens/Export/ExportVrm");
            visualTree.CloneTree(_container);
            _exportButton = _container.Q<Button>("exportButton");
            _exportButton.clicked += ExportItem;
            _backButton = _container.Q<Button>("backButton");
            _backButton.clicked += onBackClicked;
            _preview = new Preview(_container.Q("preview"), EdenStudioInitializer.SelectedItem?.path ?? "");
            _preview.ShowContent();

            _nameField = _container.Q<TextField>("nameField");
            _authorField = _container.Q<TextField>("authorField");
            _versionField = _container.Q<TextField>("versionField");

            _happyButton = _container.Q<Button>("happyButton");
            _angryButton = _container.Q<Button>("angryButton");
            _sadButton = _container.Q<Button>("sadButton");
            _relaxedButton = _container.Q<Button>("relaxedButton");
            _surprisedButton = _container.Q<Button>("surprisedButton");

            _expressionPanel = _container.Q("expressionPanel");
            _expressionLabel = _container.Q<Label>("expressionLabel");
            _expressionScrollView = _container.Q<ScrollView>("expressionScroll");
            _expressionCloseButton = _container.Q<Button>("expressionCloseButton");

            EdenStudioInitializer.OnSelectedItemChanged += OnSelectedItemChanged;

            if (EdenStudioInitializer.SelectedItem != null)
            {
                OnSelectedItemChanged(EdenStudioInitializer.SelectedItem);
            }

            _happyButton.clicked += () => OpenExpressionPanel("happy");
            _angryButton.clicked += () => OpenExpressionPanel("angry");
            _sadButton.clicked += () => OpenExpressionPanel("sad");
            _relaxedButton.clicked += () => OpenExpressionPanel("relaxed");
            _surprisedButton.clicked += () => OpenExpressionPanel("surprised");
            _expressionCloseButton.clicked += () => _expressionPanel.style.display = DisplayStyle.None;
        }

        private static void OpenExpressionPanel(string expression)
        {
            _expressionLabel.text = expression;
            _expressionScrollView.Clear();
            var selectedItem = EdenStudioInitializer.SelectedItem;
            var preset = PresetManager.LoadOrCreateVrm10MorphTargetPreset(selectedItem.modelName);

            // 이미 선택된 blendShapeKey들을 가져옴
            var selectedBlendShapeKeys =  new List<BlendShapeData>();
            var savedBlendShapeKeys = preset.Get(expression);
            var currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(selectedItem.path);
            var currentSkinnedMeshRenderers = currentPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();

            // 현재 아바타에서 찾아서 가져옴
            foreach (var saved in savedBlendShapeKeys)
            {
                var found = currentSkinnedMeshRenderers.FirstOrDefault(v => v.sharedMesh.name == saved.skinnedMeshRendererPath);
                if (found != null)
                {
                    var blendShapeData = new BlendShapeData(found, saved.index, saved.shapeKeyName);

                    // 이미 선택된 blendShapeKey에 없을 경우 추가
                    if (!selectedBlendShapeKeys.Contains(blendShapeData))
                    {
                        selectedBlendShapeKeys.Add(blendShapeData);
                    }
                }
            }

            selectedItem.SelectedBlendShapes[expression] = selectedBlendShapeKeys;

            var availableBlendShapeKeys = _blendShapeKeys.Except(selectedBlendShapeKeys.Select(b => b.shapeKeyName)).ToList();
            availableBlendShapeKeys.Insert(0, "쉐이프키를 선택해주세요");

            foreach (var selectedBlendShape in selectedBlendShapeKeys)
            {
                var blendShapeItem = new BlendShapeItem(availableBlendShapeKeys, (oldKey, key) =>
                {
                    OnShapeKeySelected(expression, oldKey, key);
                });
                // 목록안에서의 인덱스
                var blendShapeName = selectedBlendShape.skinnedMeshRenderer.sharedMesh.name + "_" + selectedBlendShape.shapeKeyName;
                blendShapeItem.SetSelectedIndex(availableBlendShapeKeys.IndexOf(blendShapeName));
                _expressionScrollView.Add(blendShapeItem);
            }

            // 누르면 새로운 blendShapeItem을 scrollview에 추가하는 버튼
            var addButton = new Button(() =>
            {
                var newBlendShapeItem = new BlendShapeItem(availableBlendShapeKeys, (oldKey, key) =>
                {
                    OnShapeKeySelected(expression, oldKey, key);
                });
                _expressionScrollView.Add(newBlendShapeItem);
            })
            {
                text = "추가"
            };
            _expressionScrollView.Add(addButton);
            _expressionPanel.style.display = DisplayStyle.Flex;
        }

        private static void OnShapeKeySelected(string expression, string oldKey, string key)
        {
            var selectedItem = EdenStudioInitializer.SelectedItem;
            if (oldKey != null && oldKey != "쉐이프키를 선택해주세요")
            {
                // remove old key
                var oldBlendShapeData = _blendShapeDataMap[oldKey];
                selectedItem.SelectedBlendShapes[expression].Remove(oldBlendShapeData);
            }

            if (key != null && key != "쉐이프키를 선택해주세요")
            {
                // add new key
                var blendShapeData = _blendShapeDataMap[key];
                if (!selectedItem.SelectedBlendShapes.ContainsKey(expression))
                {
                    selectedItem.SelectedBlendShapes[expression] = new List<BlendShapeData>();
                }
                selectedItem.SelectedBlendShapes[expression].Add(blendShapeData);
            }
            
            // 저장
            // var presetPath = Path.Combine("Assets/Eden/Presets", selectedItem.modelName + ".asset");
            var preset = PresetManager.LoadOrCreateVrm10MorphTargetPreset(selectedItem.modelName);
            var expressionData = selectedItem.SelectedBlendShapes[expression].Select(data =>
                new SerializableBlendShapeData(data.skinnedMeshRenderer.sharedMesh.name, data.index, data.shapeKeyName)).ToList();
            preset.Set(expression, expressionData);
            PresetManager.SaveVrm10MorphTargetPreset(preset);

            foreach (var blendShapeData in selectedItem.SelectedBlendShapes[expression])
            {
                Debug.Log($"{blendShapeData.skinnedMeshRenderer.name} {blendShapeData.shapeKeyName} {blendShapeData.index}");
            }

            // 갱신된 블렌드 쉐이프 키 목록으로 UI 업데이트
            OpenExpressionPanel(expression);
        }


        private static void OnSelectedItemChanged(ItemInfo item)
        {
            _blendShapeDataMap.Clear();
            _preview.RefreshPreview(item.path);
            // load prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.path);
            // load blend shape data
            var skinMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var skinMeshRenderer in skinMeshRenderers)
            {
                for (var i = 0; i < skinMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    // Mesh name + BlendShape name
                    var blendShapeName = skinMeshRenderer.sharedMesh.name + "_" + skinMeshRenderer.sharedMesh.GetBlendShapeName(i);
                    _blendShapeDataMap[blendShapeName] = new BlendShapeData(skinMeshRenderer, i, skinMeshRenderer.sharedMesh.GetBlendShapeName(i));
                }
            }
            _blendShapeKeys = _blendShapeDataMap.Keys.ToList();
        }

        private static void ChangeMaterials(GameObject prefab, string savePath)
        {
            //1. prefab 하위 오브젝트들을 활성화시킨다.
            List<GameObject> list = new List<GameObject>();
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(prefab.transform);

            while (stack.Count > 0)
            {
                Transform current = stack.Pop();

                foreach (Transform child in current)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(true);
                        list.Add(child.gameObject);
                    }

                    stack.Push(child);
                }
            }

            var setActiveFalseList = list.ToArray();


            //2.prefab의 skinmeshrenderer을 모아 이중 for each문으로 각각의 material을 가져온다.
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<Material> referenceMaterials = new List<Material>();

            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                foreach (var sMaterial in skinnedMesh.sharedMaterials)
                {
                    //3. lilToon인지 확인하고, 맞다면 referenceMaterial 라스트애 추가한다.
                    if (sMaterial != null && sMaterial.shader.name.Contains("lilToon"))
                    {
                        if (!referenceMaterials.Contains(sMaterial))
                        {
                            referenceMaterials.Add(sMaterial);
                        }
                    }
                }
            }

            //4.referneceMaterials를 순환하면 Mtoon으로 재질 변환한다. 변환한 material들은 Dictionary 형식으로 저장한다.
            var convertedMaterials = new Dictionary<Material, Material>();
            foreach (var material in referenceMaterials)
            {
                var path = Path.Combine(savePath,
                    material.name + ".mat");
                try
                {
                    lilMaterialBaker.CreateMToonMaterial(material, path);
                }
                catch (Exception e)
                {
                    //Debug.Log(material.name + "Exception : " + e);
                }

                Material m = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
                if (!m)
                {
                    Debug.Log("no Materials : " + path);
                }

                convertedMaterials.Add(material, m);
            }


            //다시 skinMesh단위로 순환하며 Dictionary의 meterial들을 map에서 찾아 교체한다.
            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                var materials = skinnedMesh.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader.name.Contains("lilToon"))
                    {
                        if (convertedMaterials.ContainsKey(materials[i]))
                        {
                            materials[i] = convertedMaterials[materials[i]];
                        }
                    }
                }

                skinnedMesh.sharedMaterials = materials;
            }

            //오브잭트들을 다시 비활성화한다.
            setActiveFalseList.ToList().ForEach(obj => obj.SetActive(false));
        }

        private static void ExportItem()
        {
            // Export the selected item
            var prefabPath = EdenStudioInitializer.SelectedItem.path;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var savePath = "Assets/Eden/" + EdenStudioInitializer.SelectedItem.modelName + ".vrm";

            Debug.Log("Exporting " + prefab.name + " to VRM format...");

            // (VRM)을 붙인 이름으로 프리팹 복제
            var prefabClone = Instantiate(prefab);
            prefabClone.name += "(VRM)";

            // 저장
            PrefabUtility.SaveAsPrefabAsset(prefabClone, "Assets/Eden/" + prefabClone.name + ".prefab");

            // 이 프리팹을 사용
            prefab = prefabClone;

            //prefab의 material들의 셰이더를 Mtoon으로 변환
            ChangeMaterials(prefab, "Assets/Eden/");

            using (var tempDisposable = new TempDisposable())
            using (var arrayManager = new NativeArrayManager())
            {
                var selectedItem = EdenStudioInitializer.SelectedItem;
                var vrmMeta = new VRM10ObjectMeta
                {
                    Name = _nameField.value,
                    Version = _versionField.value,
                    Authors = new List<string> { _authorField.value },
                };

                var copy = Instantiate(prefab);
                tempDisposable.Add(copy);
                prefab = copy;
                
                var preset = PresetManager.LoadOrCreateVrm10MorphTargetPreset(selectedItem.modelName);
                
                // 선택된 blendShapeKey들을 가져옴
                var selectedBlendShapes = new Dictionary<string, List<BlendShapeData>>();
                foreach (var expression in new[] { "happy", "angry", "sad", "relaxed", "surprised" })
                {
                    var savedBlendShapeKeys = preset.Get(expression);
                    var currentSkinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

                    // 현재 아바타에서 찾아서 가져옴
                    var selectedBlendShapeKeys = new List<BlendShapeData>();
                    foreach (var saved in savedBlendShapeKeys)
                    {
                        var found = currentSkinnedMeshRenderers.FirstOrDefault(v => v.sharedMesh.name == saved.skinnedMeshRendererPath);
                        if (found != null)
                        {
                            var blendShapeData = new BlendShapeData(found, saved.index, saved.shapeKeyName);

                            // 이미 선택된 blendShapeKey에 없을 경우 추가
                            if (!selectedBlendShapeKeys.Contains(blendShapeData))
                            {
                                selectedBlendShapeKeys.Add(blendShapeData);
                            }
                        }
                    }

                    selectedBlendShapes[expression] = selectedBlendShapeKeys;
                }

                ConvertManager.Convert(
                    savePath,
                    prefab,
                    vrmMeta,
                    selectedBlendShapes
                );

                // freeze mesh
                // 왠지 몰라도 노멀라이즈하면 모델이 깨짐. 그래서 일단 주석처리. TODO: 노멀라이즈 수정
                var newMeshMap = BoneNormalizer.NormalizeHierarchyFreezeMesh(prefab);
                BoneNormalizer.Replace(prefab, newMeshMap, true, true);
                var converter = new ModelExporter();
                var model = converter.Export(arrayManager, prefab);

                model.ConvertCoordinate(Coordinates.Vrm1);

                //셰이더를 Mtoon에서 Mtoon10으로 변환 및 _ShadTexture 속성을 임시 저장 및 _ShadeTex 속성으로 복구
                foreach (var VARIABLE in model.Materials)
                {
                    var material = VARIABLE as Material;
                    if (material == null) continue;

                    var ShadeTexture = material.GetTexture("_ShadeTexture");

                    // ShadeTexture가 없는 경우 (일반적으로 lilToon이 아닌 경우)는 MainTex를 사용
                    if (ShadeTexture == null)
                    {
                        ShadeTexture = material.GetTexture("_MainTex");
                    }

                    // Change the shader
                    material.shader = Shader.Find("VRM10/MToon10");

                    if (material.HasProperty("_ShadeTex"))
                    {
                        material.SetTexture("_ShadeTex", ShadeTexture);
                    }
                }

                var gltfExportSettings = new GltfExportSettings();
                var exporter = new Vrm10Exporter(new EditorTextureSerializer(), gltfExportSettings);
                var option = new ExportArgs();
                exporter.Export(prefab, model, converter, option);
                var bytes = exporter.Storage.ToGlbBytes();
                File.WriteAllBytes(savePath, bytes);
            }

            EditorUtility.DisplayDialog("Exported", "The item has been exported to VRM format.", "OK");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
        }
    }
}