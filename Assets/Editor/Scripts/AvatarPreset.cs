using System.Collections.Generic;
using UnityEngine;

namespace Editor.Scripts
{
    [CreateAssetMenu(fileName = "AvatarPreset", menuName = "AvatarPreset", order = 0)]
    public class AvatarPreset: ScriptableObject
    {
        public string[] costumeNames;
        public List<BlendShapeData> blendShapes; // 블렌드쉐이프 데이터 목록

        [System.Serializable]
        public class BlendShapeData
        {
            public string blendShapeName; // 블렌드쉐이프 이름
            public float value; // 블렌드쉐이프 값
        }
    }
}