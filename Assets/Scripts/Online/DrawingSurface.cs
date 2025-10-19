using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawingSurface : MonoBehaviour
{
    public int OwnerIndex;
    public localLineRenderer lineDrawer;

    public MultiLineData GetDrawnLinesData()
    {
        var lines = lineDrawer.allLines;
        List<Vector3> allPoints = new List<Vector3>();
        int[] lineLengths = new int[lines.Count];
        uint[] lineColors = new uint[lines.Count];

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            Vector3[] points = new Vector3[line.positionCount];
            line.GetPositions(points);
            lineLengths[i] = points.Length;
            allPoints.AddRange(points);

            lineColors[i] = ColorToUInt(line.startColor);
        }

        return new MultiLineData
        {
            Points = allPoints.ToArray(),
            LineLengths = lineLengths,
            LineColors = lineColors
        };
    }

    private uint ColorToUInt(Color c)
    {
        Color32 c32 = c; // converts to byte RGBA
        return (uint)(c32.a << 24 | c32.r << 16 | c32.g << 8 | c32.b);
    }

    public void Redraw(Vector3[][] lines, uint[] colors)
    {

        lineDrawer.ClearAllLines();
        StartCoroutine(DelayDraw(lines, colors));
    }

    IEnumerator DelayDraw(Vector3[][] lines, uint[] colors)
    {
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < lines.Length; i++)
        {
            Color c = (colors != null && i < colors.Length) ? UIntToColor(colors[i]) : lineDrawer.lineColor;
            lineDrawer.RebuildFromPoints(lines[i], c);
        }

    }

    private Color UIntToColor(uint value)
    {
        byte a = (byte)((value >> 24) & 0xFF);
        byte r = (byte)((value >> 16) & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)(value & 0xFF);
        return new Color32(r, g, b, a);
    }
}
