using System;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Editor.Scripts.Manager
{
    public static class ModularManager
    {
        private static GameObject _selectedObject;
        private static VRCExpressionsMenu _expressionsMenu;
        private static VRCExpressionsMenu _closetMenu;
        private static AnimatorController _animatorController;

        public static void ToggleObject(string targetPath)
        {
            // 선택된 아바타 프리팹 로드
            _selectedObject = PrefabUtility.LoadPrefabContents(targetPath);
            Debug.Log($"{EdenStudioInitializer.SelectedItem.path} is selected");
            GameObject avatarObject = FindAvatarDescriptor(PrefabUtility.LoadPrefabContents(EdenStudioInitializer.SelectedItem.path));
            
            if (_selectedObject && avatarObject)
            {
                GameObject clothes = CheckAlreadyExists(avatarObject);
                if (!clothes)
                {
                    GameObject instance = Object.Instantiate(_selectedObject, avatarObject.transform);
                    instance.name = _selectedObject.name; // 인스턴스 이름을 원본 프리팹 이름과 동일하게 설정

                    // MenuCommand를 생성하여 SetupOutfit 메서드에 전달

                    Selection.activeObject = instance;
                    MenuCommand menuCommand = new MenuCommand(instance);

                    // 리플렉션을 사용하여 내부 메서드 호출
                    InvokeInternalSetupOutfit(menuCommand);
                    Selection.activeObject = null;

                    CreateAnimation(instance);
                    AddComponents(instance);
                }
                else
                {
                    Object.DestroyImmediate(clothes);
                }

                PrefabUtility.SaveAsPrefabAsset(avatarObject, EdenStudioInitializer.SelectedItem.path);
            }
            else if (!_selectedObject)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject", "OK");
            }
            else if (!avatarObject)
            {
                EditorUtility.DisplayDialog("Error", "Please make avatar instance in level", "OK");
            }
        }

        private static GameObject CheckAlreadyExists(GameObject avatarObject)
        {
            Transform foundTransform = avatarObject.transform.Find(_selectedObject.name);
            return foundTransform ? foundTransform.gameObject : null;
        }

        private static void InvokeInternalSetupOutfit(MenuCommand menuCommand)
        {
            // 어셈블리 로드
            Assembly assembly =
                Assembly.LoadFrom("Library\\ScriptAssemblies\\nadena.dev.modular-avatar.core.editor.dll");
            // EasySetupOutfit 타입 가져오기
            var type = assembly.GetType("nadena.dev.modular_avatar.core.editor.EasySetupOutfit");

            // SetupOutfit 메서드 가져오기
            MethodInfo methodInfo = type.GetMethod("SetupOutfit", BindingFlags.Static | BindingFlags.NonPublic);
            if (methodInfo == null)
            {
                Debug.LogError("메서드를 찾을 수 없습니다: SetupOutfit");
                return;
            }

            // 메서드 호출
            methodInfo.Invoke(null, new object[] { menuCommand });
            Console.ReadKey();
        }

        private static GameObject FindAvatarDescriptor(GameObject targetObject)
        {
            VRC_AvatarDescriptor[] avatars = targetObject.GetComponentsInChildren<VRC_AvatarDescriptor>();
            if (avatars.Length > 0)
            {
                return avatars[0].gameObject;
            }

            return null;
        }

        private static void CreateAnimation(GameObject targetObject)
        {
            AnimationClip toggleOnClip = new AnimationClip();
            AnimationClip toggleOffClip = new AnimationClip();

            SkinnedMeshRenderer[] renderers = targetObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                string animPath = renderer.transform.name;

                AddAnimationCurve(toggleOnClip, animPath, true);
                AddAnimationCurve(toggleOffClip, animPath, false);
            }

            string folderPath = "Assets/EasyCloset";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "EasyCloset");
            }

            folderPath += $"/{targetObject.name}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/EasyCloset", targetObject.name);
            }

            AssetDatabase.CreateAsset(toggleOnClip, $"{folderPath}/{targetObject.name}_ToggleOn.anim");
            AssetDatabase.CreateAsset(toggleOffClip, $"{folderPath}/{targetObject.name}_ToggleOff.anim");

            _animatorController =
                AnimatorController.CreateAnimatorControllerAtPath(
                    $"{folderPath}/{targetObject.name}_ToggleAnimatorController.controller");

            _animatorController.AddParameter($"{targetObject.name}Toggle", AnimatorControllerParameterType.Bool);

            AnimatorState toggleOnState = _animatorController.AddMotion(toggleOnClip);
            AnimatorState toggleOffState = _animatorController.AddMotion(toggleOffClip);

            _animatorController.layers[0].stateMachine.defaultState = toggleOnState;

            AnimatorStateMachine stateMachine = _animatorController.layers[0].stateMachine;
            AnimatorStateTransition toOffTransition = toggleOnState.AddTransition(toggleOffState);
            toOffTransition.AddCondition(AnimatorConditionMode.IfNot, 0, $"{targetObject.name}Toggle");
            toOffTransition.hasExitTime = false;

            AnimatorStateTransition toOnTransition = toggleOffState.AddTransition(toggleOnState);
            toOnTransition.AddCondition(AnimatorConditionMode.If, 0, $"{targetObject.name}Toggle");
            toOnTransition.hasExitTime = false;

            Animator animator = targetObject.GetComponent<Animator>();
            if (!animator)
            {
                animator = targetObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = _animatorController;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddAnimationCurve(AnimationClip clip, string path, bool isActive)
        {
            // AnimationCurve를 사용하여 키프레임을 설정
            AnimationCurve curve = AnimationCurve.Constant(0, 0, isActive ? 1f : 0f);
            clip.SetCurve(path, typeof(GameObject), "m_IsActive", curve);
        }

        private static void AddComponents(GameObject targetObject)
        {
            // 1. Menu 생성
            CreateVRCExpressionsMenu(targetObject);

            // 2. MA Menu Installer 컴포넌트 추가
            ModularAvatarMenuInstaller installer = targetObject.GetComponent<ModularAvatarMenuInstaller>();
            if (!installer)
            {
                installer = targetObject.AddComponent<ModularAvatarMenuInstaller>();
            }
            else
            {
                //Menu Installer가 있으면 이미 해당 오브젝트는 진행했다 가정
                return;
            }

            // rootMenu를 찾아서 installTargetMenu로 설정
            VRCAvatarDescriptor avatarDescriptor = targetObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatarDescriptor)
            {
                installer.installTargetMenu = avatarDescriptor.expressionsMenu;
            }

            VRCExpressionsMenu rootMenu = installer.installTargetMenu;
            if (!findClosetMenuControl(rootMenu))
            {
                var control = new VRCExpressionsMenu.Control
                {
                    name = "Closet",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = _closetMenu
                };
                rootMenu.controls.Add(control);
            }

            installer.installTargetMenu = _closetMenu;
            installer.menuToAppend = _expressionsMenu;
            installer.menuToAppend.controls[0].parameter.name = _animatorController.parameters[0].name;

            // 3. MA Parameters 컴포넌트 추가
            ModularAvatarParameters parameters = targetObject.GetComponent<ModularAvatarParameters>();
            if (!parameters)
            {
                parameters = targetObject.AddComponent<ModularAvatarParameters>();
            }

            // 4. bool 타입의 파라미터 생성 -> animation cotroller에 등록된 파라미터
            parameters.parameters.Add(new ParameterConfig
            {
                internalParameter = false,
                nameOrPrefix = _animatorController.parameters[0].name,
                isPrefix = false,
                remapTo = "",
                syncType = ParameterSyncType.Bool,
                saved = true,
            });


            // 5. MA Merge Animator 컴포넌트 추가
            ModularAvatarMergeAnimator mergeAnimator = targetObject.GetComponent<ModularAvatarMergeAnimator>();
            if (!mergeAnimator)
            {
                mergeAnimator = targetObject.AddComponent<ModularAvatarMergeAnimator>();
            }

            // 기본 설정 추가
            mergeAnimator.animator = targetObject.GetComponent<Animator>().runtimeAnimatorController;
            mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            mergeAnimator.deleteAttachedAnimator = true;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
            mergeAnimator.matchAvatarWriteDefaults = false;
            mergeAnimator.relativePathRoot = new AvatarObjectReference();
            mergeAnimator.layerPriority = 0;
        }

        static bool findClosetMenuControl(VRCExpressionsMenu rootMenu)
        {
            if (!rootMenu) return false;
            foreach (var control in rootMenu.controls)
            {
                if (control.name == "Closet" && control.subMenu == _closetMenu)
                    return true;
                else if (control.name == "Closet")
                {
                    rootMenu.controls.Remove(control);
                }
            }

            return false;
        }

        private static void CreateVRCExpressionsMenu(GameObject targetObject)
        {
            //Assets/MakeToggle 확인
            string folderPath = "Assets/EasyCloset";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "EasyCloset");
            }

            //Assets/MakeToggle/ClosetMenu.asset 확인
            string closetPath = folderPath + "/ClosetMenu.asset";
            _closetMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(closetPath);
            if (!_closetMenu)
            {
                _closetMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AssetDatabase.CreateAsset(_closetMenu, closetPath);
                AssetDatabase.SaveAssets();
            }

            //Assets/MakeToggle/selectObject.name 폴더 확인
            folderPath += $"/{targetObject.name}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("EasyCloset", targetObject.name);
            }

            //Asset/MakeToggle/selectObject.name/NewExpressionMenu.asset 확인
            string path = folderPath + "/NewExpressionsMenu.asset";
            _expressionsMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
            if (_expressionsMenu)
            {
                return;
            }

            _expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            var control = new VRCExpressionsMenu.Control
            {
                name = targetObject.name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter
                {
                    name = targetObject.name + "Toggle"
                }
            };
            _expressionsMenu.controls.Add(control);

            AssetDatabase.CreateAsset(_expressionsMenu, path);
            AssetDatabase.SaveAssets();
        }

        public static bool HasObject(string itemPath)
        {
            // 아이템 경로로 프리팹 로드
            GameObject item = PrefabUtility.LoadPrefabContents(itemPath);
            var avatarObject = EdenStudioInitializer.SelectedItem ? FindAvatarDescriptor(PrefabUtility.LoadPrefabContents(EdenStudioInitializer.SelectedItem.path)) : null;
            
            // 아이템이 로드되지 않았다면 false 반환
            if (!item || !avatarObject)
            {
                return false;
            }
            Debug.Log($"{EdenStudioInitializer.SelectedItem.path} is selected");
            
            // 아이템이 로드되었다면 아바타 디스크립터 찾기
            var existingItem = avatarObject.transform.Find(item.name);
            
            // 아바타 디스크립터가 없다면 false 반환
            return existingItem;
        }

        public static void ToggleCostume(bool active)
        {
            // 선택된 아바타 프리팹 로드
            GameObject avatarObject = FindAvatarDescriptor(PrefabUtility.LoadPrefabContents(EdenStudioInitializer.SelectedItem.path));
            Debug.Log($"{EdenStudioInitializer.SelectedItem.path} is selected");
            
            var preset = PresetManager.LoadOrCreateAvatarPreset(EdenStudioInitializer.SelectedItem.modelName);

            // 프리셋에 정의된 의상들 활성화/비활성화
            SkinnedMeshRenderer[] renderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (preset.costumeNames == null) continue;
                if (System.Array.Exists(preset.costumeNames, name => renderer.name.Contains(name)))
                {
                    renderer.gameObject.SetActive(active);
                }
            }

            // 블렌드쉐이프 설정
            foreach (var renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh != null && preset.blendShapes != null)
                {
                    foreach (var blendShape in preset.blendShapes)
                    {
                        int blendShapeIndex = mesh.GetBlendShapeIndex(blendShape.blendShapeName);
                        if (blendShapeIndex >= 0)
                        {
                            renderer.SetBlendShapeWeight(blendShapeIndex, active ? blendShape.value : 0);
                        }
                    }
                }
            }

            // 변경된 내용 프리팹에 저장
            PrefabUtility.SaveAsPrefabAsset(avatarObject, EdenStudioInitializer.SelectedItem.path);
        }
    }
}