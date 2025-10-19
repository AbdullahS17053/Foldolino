using UnityEngine;
using System.Collections.Generic;

public class localLineRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    public Material lineMaterial;
    public float lineWidth = 0.1f;
    public Color lineColor = Color.white;

    public float drawDistance = 10f;

    [Header("UI Bounds")]
    public RectTransform drawingArea;
    public bool canDraw = false;
    public bool isLocalPlay = false;
    public bool hasDrawn = false;

    [Header("Controls")]
    [Space(10)]
    [TextArea(2, 3)]
    public string instructions = "Left Click/Touch to draw lines\nRight Click to clear all lines";

    public List<LineRenderer> allLines = new List<LineRenderer>();
    private Stack<LineRenderer> undoStack = new Stack<LineRenderer>();
    private Stack<LineRenderer> redoStack = new Stack<LineRenderer>();
    private LineRenderer currentLine;
    private List<Vector3> currentPoints = new List<Vector3>();
    private Camera mainCamera;
    private bool isDrawing = false;
    private int activeFingerId = -1;

    void Start()
    {
        mainCamera = Camera.main;
        if (!isLocalPlay)
            drawingArea = UIManager.Instance.drawingArea;
    }
    private void OnEnable()
    {
        if (isLocalPlay)
            LocalDrawManager.instance.playerSelectionPopup.SetActive(true);
    }
    bool IsInsideDrawingArea(Vector2 screenPos)
    {
        if (drawingArea == null)
            return true;
        if (!canDraw)
            return false;

        Canvas canvas = drawingArea.GetComponentInParent<Canvas>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(drawingArea, screenPos, cam);
    }

    void Update()
    {
        // PC: Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            if (IsInsideDrawingArea(Input.mousePosition))
            {
                StartNewLine();
                AddPointToCurrentLine(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            AddPointToCurrentLine(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            FinishCurrentLine();
        }

        // Undo with CTRL + Z (or just Z)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UndoLine();
        }

        // Redo with CTRL + Y (or just Y)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RedoLine();
        }

        // Mobile: Touch input
        if (Input.touchCount > 0)
        {
            Touch? activeTouch = null;

            if (activeFingerId != -1)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).fingerId == activeFingerId)
                    {
                        activeTouch = Input.GetTouch(i);
                        break;
                    }
                }

                if (!activeTouch.HasValue)
                {
                    FinishCurrentLine();
                    activeFingerId = -1;
                    return;
                }
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (IsInsideDrawingArea(t.position))
                        {
                            activeFingerId = t.fingerId;
                            activeTouch = t;
                            StartNewLine();
                            AddPointToCurrentLine(t.position);
                            break;
                        }
                    }
                }
            }

            if (activeTouch.HasValue)
            {
                Touch touch = activeTouch.Value;

                if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isDrawing)
                {
                    AddPointToCurrentLine(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    FinishCurrentLine();
                    activeFingerId = -1;
                }
            }
        }
        else
        {
            if (activeFingerId != -1)
            {
                FinishCurrentLine();
                activeFingerId = -1;
            }
        }
    }

    void StartNewLine()
    {
        isDrawing = true;
        hasDrawn = true;
        currentPoints.Clear();

        GameObject lineObj = new GameObject("DrawnLine_" + allLines.Count);
        lineObj.transform.SetParent(transform);

        currentLine = lineObj.AddComponent<LineRenderer>();
        currentLine.material = lineMaterial;
        currentLine.startColor = lineColor;
        currentLine.endColor = lineColor;
        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;
        currentLine.positionCount = 0;
        currentLine.useWorldSpace = true;


        allLines.Add(currentLine);
        // clear redo history when starting a new line
        redoStack.Clear();
    }

    void AddPointToCurrentLine(Vector2 screenPos)
    {
        if (!isDrawing || currentLine == null) return;

        if (!IsInsideDrawingArea(screenPos))
        {
            return;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, drawDistance));

        // Only add if far enough from last point
        if (currentPoints.Count == 0 || Vector3.Distance(currentPoints[currentPoints.Count - 1], worldPos) > 0.05f)
        {
            currentPoints.Add(worldPos);
            currentLine.positionCount = currentPoints.Count;
            currentLine.SetPositions(currentPoints.ToArray());
        }
    }

    void FinishCurrentLine()
    {
        isDrawing = false;

        // Remove line if it has too few points
        if (currentPoints.Count < 2 && currentLine != null)
        {
            allLines.Remove(currentLine);
            DestroyImmediate(currentLine.gameObject);
        }
        else if (currentLine != null)
        {
            undoStack.Push(currentLine);
        }

        currentLine = null;
        currentPoints.Clear();
    }

    public void ClearAllLines()
    {
        foreach (LineRenderer line in allLines)
        {
            if (line != null)
                DestroyImmediate(line.gameObject);
        }

        allLines.Clear();
        undoStack.Clear();
        redoStack.Clear();

        if (isDrawing)
        {
            isDrawing = false;
            currentLine = null;
            currentPoints.Clear();
        }
    }

    // Public method that can be called from UI buttons
    public void ClearAllLinesButton()
    {
        ClearAllLines();
    }

    // Undo last line
    public void UndoLine()
    {
        if (undoStack.Count > 0)
        {
            LineRenderer lastLine = undoStack.Pop();
            if (lastLine != null)
            {
                allLines.Remove(lastLine);
                lastLine.gameObject.SetActive(false); // hide instead of destroy
                redoStack.Push(lastLine);
            }
        }
    }

    // Redo last undone line
    public void RedoLine()
    {
        if (redoStack.Count > 0)
        {
            LineRenderer redoLine = redoStack.Pop();
            if (redoLine != null)
            {
                redoLine.gameObject.SetActive(true);
                allLines.Add(redoLine);
                undoStack.Push(redoLine);
            }
        }
    }
    public void RemoveSaves()
    {
        redoStack.Clear();
        undoStack.Clear();
    }

    public List<Vector3[]> GetAllLines()
    {
        List<Vector3[]> lines = new List<Vector3[]>();
        foreach (LineRenderer line in allLines)
        {
            if (line == null) continue;
            Vector3[] points = new Vector3[line.positionCount];
            line.GetPositions(points);
            lines.Add(points);
        }
        return lines;
    }

    // Called when we receive points from the server
    public void RebuildFromPoints(Vector3[] points, Color? colorOverride = null)
    {
        if (points == null || points.Length < 2) return;

        //ClearAllLines();

        GameObject lineObj = new GameObject("RedrawnLine_" + allLines.Count);
        lineObj.transform.SetParent(transform);

        LineRenderer newLine = lineObj.AddComponent<LineRenderer>();
        newLine.material = lineMaterial;

        Color c = colorOverride ?? lineColor;
        newLine.startColor = c;
        newLine.endColor = c;

        newLine.startWidth = lineWidth;
        newLine.endWidth = lineWidth;
        newLine.useWorldSpace = true;
        newLine.positionCount = points.Length;
        newLine.SetPositions(points);

        allLines.Add(newLine);
    }
}
