using System;
using System.Collections.Generic;
using BasicStats;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.Legacy.utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace ubco.ovilab.HPUI.Core
{
    public class HPUIMultiFingerCanvas : MonoBehaviour
    {
        public UnityEvent<HPUIGestureEventArgs, HPUICanvasEventArgs> OnCanvasInteractions = new();
        public Dictionary<Vector2Int, Collider> coordsToCollider = new();
        public float X_size => x_size;
        public float Y_size => y_size;
        public int MeshXResolution => meshXResolution;
        public int MeshYResolution => meshYResolution;

        public Vector2 MinBounds => minBounds;
        public Vector2 MaxBounds => maxBounds;

        public HPUIMeshContinuousInteractable[,] HPUICanvasComponents => HPUIInteractables.GetCells();
        [SerializeField] private HPUIInteractable2DArray HPUIInteractables = new();
        
        [Tooltip("In Percent")]
        [SerializeField] private Vector2Int boundaryBuffer;

        [Header("One Euro Params")]
        private OneEuroFilter<Vector2> posFilter;
        [Header("SWD One Euro filter settings")]
        [Tooltip("Filter min cutoff for position filter")]
        [SerializeField] private bool applyPosFilter = true;
        [SerializeField] private float posFilterMinCutoff = 1f;
        [Tooltip("Beta value for position filter")]
        [SerializeField] private float posFilterBeta = 50;

        [Header("Debug Info")]
        [SerializeField] private List<Vector2> currentGesturePoints = new();
        [SerializeField] private List<Vector2> bufferPoints = new();

        [SerializeField, HideInInspector] private float x_size;
        [SerializeField, HideInInspector] private float y_size;
        [SerializeField, HideInInspector] private int meshXResolution;
        [SerializeField, HideInInspector] private int meshYResolution;
        [SerializeField, HideInInspector] private Vector2 minBounds;
        [SerializeField, HideInInspector] private Vector2 maxBounds;

        private readonly Quaternion VectorCorrection = Quaternion.Euler(0, 0, 90f);
        private int notValidPoints = 0;
        private bool hasGestureStarted = false;
        [SerializeField] private HPUICanvasState canvasState = HPUICanvasState.INVALID;
        private void OnEnable()
        {
            for (int i = 0; i < HPUIInteractables.GridSize.x; i++)
            {
                for (int j = 0; j < HPUIInteractables.GridSize.y; j++)
                {
                    HPUIInteractables.GetCell(i,j).GestureEvent.AddListener(HandleGesture);
                }
            }
            posFilter = new(90, posFilterMinCutoff, posFilterBeta);
        }

        private void OnDisable()
        {
            for (int i = 0; i < HPUIInteractables.GridSize.x; i++)
            {
                for (int j = 0; j < HPUIInteractables.GridSize.y; j++)
                {
                    HPUIInteractables.GetCell(i,j).GestureEvent.AddListener(HandleGesture);
                }
            }
            posFilter = new(90, posFilterMinCutoff, posFilterBeta);
        }

        private void Awake()
        {
            RestitchInteractables();
        }
        private void RestitchInteractables()
        {
            int maxX = HPUIInteractables.GridSize.x, maxY = HPUIInteractables.GridSize.y;
            
            for (int i = 0; i < maxX; i++)
            {
                x_size += HPUIInteractables.GetCell(i,0).X_size;
                meshXResolution += HPUIInteractables.GetCell(i,0).MeshXResolution;
            }

            for (int j = 0; j < maxY; j++)
            {
                y_size += HPUIInteractables.GetCell(0,j).Y_size;
                meshYResolution += HPUIInteractables.GetCell(0,j).MeshYResolution;
            }

            for (int i = 0; i < maxX; i++)
            {
                for (int j = 0; j < maxY; j++)
                {
                    foreach ((Vector2Int key, Collider value) in HPUIInteractables.GetCell(i,j).ContinuousCollidersManager.RawCoordsToCollider)
                    {
                        //The older coordinates are between (0,0) to (MeshXRes, MeshYRes) for each interactable
                        //Refitting it (0,0) and (MeshXRes * MaxX, MeshYRes * MaxY) 
                        Vector2Int coordinate = new(key.x + i * HPUIInteractables.GetCell(i, j).MeshXResolution, 
                                                    key.y + j * HPUIInteractables.GetCell(i, j).MeshYResolution);
                        coordsToCollider[coordinate] = value;
                        Debug.Log(coordinate);
                    }
                }
            }

            minBounds = Vector2.zero;
            maxBounds = new(HPUIInteractables.GridSize.x, HPUIInteractables.GridSize.y);
        }
        private void HandleGesture(HPUIGestureEventArgs eventArgs)
        {
            HPUICanvasEventArgs canvasArgs;
            HPUIMeshContinuousInteractable interactable = eventArgs.CurrentTrackingInteractable as HPUIMeshContinuousInteractable;
            Vector2 inputPosition = eventArgs.CurrentTrackingInteractablePoint;
            if (!ProcessTouchPoints(inputPosition, interactable, out Vector2 processedPosition))
            {
                return;
            }

            switch (eventArgs.State)
            {
                case HPUIGestureState.Started or HPUIGestureState.Updated:
                {
                    if (!hasGestureStarted)
                    {
                        bufferPoints.Add(processedPosition);
                        if (IsInBufferBoundary(processedPosition, interactable))
                        {
                            notValidPoints++;
                            if (notValidPoints == bufferPoints.Count)
                            {
                                canvasState = HPUICanvasState.NotStarted;
                                canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints);
                                OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                                break;
                            }
                        }
                        else
                        {
                            notValidPoints = 0;
                            hasGestureStarted = true;
                            canvasState = HPUICanvasState.Started;
                            currentGesturePoints.Add(processedPosition);
                            canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints);
                            OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                            bufferPoints.Clear();
                            break;
                        }
                    }

                    canvasState = HPUICanvasState.Processing;
                    currentGesturePoints.Add(processedPosition);
                    canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints);
                    OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                    break;
                }
                case HPUIGestureState.Stopped:
                {
                    canvasState = HPUICanvasState.Completed;
                    if (currentGesturePoints.Count < 5 || eventArgs.CumulativeDistance < 0.005f)
                    {
                        canvasState = HPUICanvasState.Cancelled;
                        canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints);
                        OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                        ResetGesture();
                        break;
                    }
                    canvasArgs.GesturePositions = Stats.RemoveOutliers(currentGesturePoints);
                    Vector2Int binnedDirection = CalculateDirection(canvasArgs.GesturePositions,2,2, out Vector2 rawDirection);
                    canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints,binnedDirection, rawDirection);
                    OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                    ResetGesture();
                    break;
                }
                case HPUIGestureState.Canceled:
                {
                    canvasState = HPUICanvasState.Cancelled;
                    canvasArgs = new HPUICanvasEventArgs(canvasState, currentGesturePoints);
                    OnCanvasInteractions?.Invoke(eventArgs, canvasArgs);
                    ResetGesture();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ResetGesture()
        {
            canvasState = HPUICanvasState.INVALID;
            currentGesturePoints.Clear();
            posFilter = new(90, posFilterMinCutoff, posFilterBeta);
            hasGestureStarted = false;
        }

        public bool ProcessTouchPoints(Vector2 touchPoint, HPUIMeshContinuousInteractable canvasComponent, out Vector2 processedPosition)
        {
            Vector2 processedTouchPos = new((touchPoint.x + canvasComponent.OffsetX) / canvasComponent.X_size,
                                            (touchPoint.y + canvasComponent.OffsetY) / canvasComponent.Y_size);

            Vector2Int? canvasID = HPUIInteractables.GetID(canvasComponent);
            if (canvasID != null)
            {
                processedTouchPos.x = Mathf.Clamp(canvasID.Value.x + processedTouchPos.x, minBounds.x, maxBounds.x);
                processedTouchPos.y = Mathf.Clamp(canvasID.Value.y + processedTouchPos.y, minBounds.y, maxBounds.y);
                processedPosition = applyPosFilter ? posFilter.Filter(processedTouchPos) : processedTouchPos;
                return true;
            }

            Debug.LogWarning($"Unregistered Canvas: {canvasComponent.transform.name}");
            processedPosition = Vector2.zero;
            return false;
        }

        private bool IsInBufferBoundary(Vector2 point, HPUIMeshContinuousInteractable currentInteractable)
        {
            Vector2Int? canvasID = HPUIInteractables.GetID(currentInteractable);
            Debug.Assert(canvasID!=null, $"Canvas not found: {currentInteractable.transform.name}");
            
            Vector2 canvasMinBounds = (Vector2) canvasID;
            Vector2 canvasBoundaryEnd = canvasMinBounds + Vector2.one;
            Vector2 bufferArea = new Vector2(boundaryBuffer.x/100f, boundaryBuffer.y/100f);
            bool isNearHorizontalBoundary = point.x <= (canvasMinBounds.x + bufferArea.x) ||
                                            point.x >= (canvasBoundaryEnd.x - bufferArea.x);

            bool isNearVerticalBoundary = point.y <= canvasMinBounds.y + bufferArea.y ||
                                          point.y >= canvasBoundaryEnd.y - bufferArea.y;

            return isNearHorizontalBoundary || isNearVerticalBoundary;
        }

        private Vector2Int CalculateDirection(List<Vector2> points, int rollBackFrames, int forwardFrames, out Vector2 rawDirection)
        {
            Vector2 endPoint = new Vector2(points[^rollBackFrames].x, points[^rollBackFrames].y);
            Vector2 startPoint = new Vector2(points[forwardFrames].x, points[forwardFrames].y);
            rawDirection =  VectorCorrection * (endPoint - startPoint);
            Vector2Int outputVector = Mathf.Abs(rawDirection.x) > Mathf.Abs(rawDirection.y)
                ? Vector2Int.RoundToInt(Vector2.right) * (int)Mathf.Sign(rawDirection.x)
                : Vector2Int.RoundToInt(Vector2.up) * (int)Mathf.Sign(rawDirection.y);
            return outputVector;
        }

    }

    public struct HPUICanvasEventArgs
    {
        public Vector2Int? SwipeStartRegion;
        public Vector2Int? CurrentSwipeRegion;
        public Vector2Int? SwipeEndRegion;
        public Vector2? RawDirection;
        public Vector2Int? BinnedDirection;
        public HPUICanvasState State;
        public List<Vector2> GesturePositions;

        public HPUICanvasEventArgs(HPUICanvasState state,List<Vector2> gesturePositions, Vector2Int? binnedDirection = null, Vector2? rawDirection = null,  Vector2Int? swipeStartRegion = null, Vector2Int? swipeEndRegion = null, Vector2Int? currentSwipeRegion = null)
        {
            State = state;
            GesturePositions = gesturePositions;
            BinnedDirection = binnedDirection;
            RawDirection = rawDirection;
            SwipeStartRegion = swipeStartRegion;
            CurrentSwipeRegion = currentSwipeRegion;
            SwipeEndRegion = swipeEndRegion;
        }
    }

    public enum HPUICanvasState
    {
        INVALID = -1,
        NotStarted = 0,
        Started = 1,
        Processing = 2,
        Cancelled = 3,
        Completed = 4
    }

    public static class HPUICanvasComponentUtils
    {
        public static Vector2Int CalculateColliderIndex(Vector2 coords, HPUIMultiFingerCanvas targetCanvas)
        {
            int xVal = Mathf.FloorToInt(coords.x / targetCanvas.MaxBounds.x * targetCanvas.MeshXResolution );
            int yVal = Mathf.FloorToInt(coords.y / targetCanvas.MaxBounds.y *  targetCanvas.MeshYResolution );

            if (xVal < 0 || yVal < 0 || xVal > targetCanvas.MeshXResolution - 1 || yVal > targetCanvas.MeshYResolution - 1)
            {
                Debug.LogWarning("This Should not happen");
            }
            xVal = Mathf.Clamp(xVal, 0, targetCanvas.MeshXResolution - 1);
            yVal = Mathf.Clamp(yVal, 0, targetCanvas.MeshYResolution - 1);

            return new Vector2Int(xVal, yVal);
        }


    }
}



