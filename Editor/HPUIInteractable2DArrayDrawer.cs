using Array2DEditor;
using ubco.ovilab.HPUI.Interaction;
using UnityEditor;
using UnityEngine;

namespace ubco.ovilab.HPUI
{
    [CustomPropertyDrawer(typeof(HPUIInteractable2DArray))]
    public class HPUIInteractable2DArrayDrawer : Array2DMonoBehaviorDrawer<HPUIMeshContinuousInteractable> {}
    
    public abstract class Array2DMonoBehaviorDrawer<T> : Array2DDrawer where T : MonoBehaviour
    {
        protected override Vector2Int GetDefaultCellSizeValue() => new Vector2Int(64, 64);

        protected override object GetDefaultCellValue() => null;

        protected override object GetCellValue(SerializedProperty cell) => cell.objectReferenceValue;

        protected override void SetValue(SerializedProperty cell, object obj)
        {
            cell.objectReferenceValue = (T)obj;
        }
    }
}
