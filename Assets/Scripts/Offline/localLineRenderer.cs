using UnityEngine;
using System.Collections.Generic;

public class localLineRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    public Material lineMaterial;
    public float lineWidth = 0.1f;
    public Color lineColor = Color.white;

    [Header("Controls")]
    [Space(10)]
    [TextArea(2, 3)]
    public string instructions = "Left Click/Touch to draw lines\nRight Click to clear all lines";

    private List<LineRenderer> allLines = new List<LineRenderer>();
    private LineRenderer currentLine;
    private List<Vector3> currentPoints = new List<Vector3>();
    private Camera mainCamera;
    private bool isDrawing = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Right click to clear all lines
        if (Input.GetMouseButtonDown(1))
        {
            ClearAllLines();
            return;
        }

        // PC: Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            StartNewLine();
            AddPointToCurrentLine(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            AddPointToCurrentLine(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            FinishCurrentLine();
        }

        // Mobile: Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                StartNewLine();
                AddPointToCurrentLine(touch.position);
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isDrawing)
            {
                AddPointToCurrentLine(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                FinishCurrentLine();
            }
        }
    }

    void StartNewLine()
    {
        isDrawing = true;
        currentPoints.Clear();
        
        // Create new LineRenderer for this line
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
    }

    void AddPointToCurrentLine(Vector2 screenPos)
    {
        if (!isDrawing || currentLine == null) return;
        
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        
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
}
