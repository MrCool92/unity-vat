using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VAT
{
    [CreateAssetMenu]
    public class VatDataList : ScriptableObject
    {
        public List<VatData> entries = new List<VatData>();

        [MenuItem("CONTEXT/VatDataList/Populate")]
        private static void Populate(MenuCommand command)
        {
            var vatDataAssetList = command.context as VatDataList;
            if (vatDataAssetList == null)
            {
                return;
            }

            vatDataAssetList.entries = new List<VatData>();
            var guids = AssetDatabase.FindAssets($"t:{nameof(VatData)}");
            for (int i = 0; i < guids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<VatData>(path);
                vatDataAssetList.entries.Add(asset);
                asset.vatId = (ushort)i;
                EditorUtility.SetDirty(asset);
            }

            EditorUtility.SetDirty(vatDataAssetList);
            AssetDatabase.Refresh();
        }
    }
}