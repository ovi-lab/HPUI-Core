using Array2DEditor;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

[System.Serializable]
public class HPUIInteractable2DArray : Array2D<HPUIMeshContinuousInteractable>
{
    [SerializeField] private CellRowContinuousInteractable[] cells = new CellRowContinuousInteractable[Consts.defaultGridSize];
    
    protected override CellRow<HPUIMeshContinuousInteractable> GetCellRow(int idx)
    {
        return cells[cells.Length - idx - 1];
    }
}

[System.Serializable]
public class CellRowContinuousInteractable : CellRow<HPUIMeshContinuousInteractable> { }


