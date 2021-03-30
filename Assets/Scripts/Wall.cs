using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Wall : MonoBehaviour
{
    Mesh mesh;
    public List<FixedPoint> fixedPoints = new List<FixedPoint>();
    List<Vector2> points = new List<Vector2>();
    List<Vector2> pointsHome = new List<Vector2>();
    void Start()
    {
        for (int i = 0; i < fixedPoints.Count; i++)
        {
            FixedPoint start = fixedPoints[i];
            FixedPoint end;
            if (i + 1 == fixedPoints.Count)
            {
                end = fixedPoints[0];
            }
            else
            {
                end = fixedPoints[i + 1];
            }
            Vector2 startNormal = Quaternion.AngleAxis(90, Vector3.back) * (start.handle - start.location);
            Vector2 diff = end.location - start.location;
            float dist = diff.magnitude;
            Vector2 circleDirection = Vector3.Project(diff, startNormal).normalized;
            float travelDist;
            float totalAngle = 0;
            if (circleDirection.SqrMagnitude() == 0)
            {
                travelDist = Vector3.Distance(start.location, end.location);
            }
            else
            {
                float centerAngle = Vector2.SignedAngle(circleDirection, diff);
                float r = (dist / 2) / Mathf.Cos(Mathf.Deg2Rad * centerAngle);
                Vector2 circleCenter = start.location + circleDirection * r;
                // Debug.DrawLine(start.location, circleCenter, Color.blue, 1000000);
                // Debug.DrawLine(start.location, start.location + circleDirection, Color.yellow, 1000000);
                // Debug.DrawLine(start.location, start.location + startNormal, Color.green, 1000000);
                // Debug.DrawLine(start.location, start.location + diff, Color.red, 1000000);
                // for (int a = 0; a < 360; a++)
                // {
                //     Debug.DrawLine((Vector3)circleCenter + Quaternion.AngleAxis(a, Vector3.back) * Vector2.left * r, (Vector3)circleCenter + Quaternion.AngleAxis(a - 1, Vector3.back) * Vector2.left * r, Color.cyan, 10000000);
                // }
                totalAngle = GetAngle(-circleDirection, end.location - circleCenter, Vector2.Dot(circleDirection, startNormal) < 0);
                travelDist = r * 3.1415f * 2 * Mathf.Abs(totalAngle) / 360;
            }
            int dots = (int)(travelDist * 5);
            float step = travelDist / dots;
            float aStep = totalAngle / dots;
            Vector2 currentDir = (start.handle - start.location).normalized;
            Vector2 basePos = start.location;
            for (int n = 0; n < dots; n++)
            {
                points.Add(basePos);
                basePos += currentDir * step;
                currentDir = Quaternion.AngleAxis(aStep, Vector3.back) * currentDir;
            }
        }
        pointsHome = new List<Vector2>(points);
        GetComponent<PolygonCollider2D>().points = points.ToArray();
        GetComponent<LineRenderer>().positionCount = points.Count;
        GetComponent<LineRenderer>().SetPositions(To3D(points.ToArray()));
    }

    void FixedUpdate(){
        for (int i = 0; i < points.Count; i++){
            points[i] = Vector2.Lerp(points[i], pointsHome[i], Time.deltaTime * 5);
        }
        GetComponent<PolygonCollider2D>().points = points.ToArray();
        GetComponent<LineRenderer>().SetPositions(To3D(points.ToArray()));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        ContactPoint2D cont = collision.GetContact(0);
        for (int i = 0; i < points.Count; i++)
        {
            points[i] += Mathf.Max(0, 10 - Vector2.Distance(points[i], cont.point)) / 50 * cont.normal * Vector3.Project(collision.rigidbody.velocity, cont.normal).magnitude;
        }
    }

    public static float GetAngle(Vector2 v1, Vector2 v2, bool flip)
    {
        float signedAngle = Vector2.SignedAngle(v1, v2) * (flip ? 1 : -1);
        print(signedAngle);
        if (signedAngle < 0)
        {
            signedAngle += 360;
        }
        return signedAngle * (flip ? -1 : 1);
    }

    Vector3[] To3D(Vector2[] v2d)
    {
        Vector3[] outArr = new Vector3[v2d.Length];
        for (int i = 0; i < v2d.Length; i++)
        {
            outArr[i] = v2d[i];
        }
        return outArr;
    }
}

[System.Serializable]
public struct FixedPoint
{
    public Vector2 location;
    public Vector2 handle;
    public bool beforeLocked;
    public bool afterLocked;
}

[CustomEditor(typeof(Wall)), CanEditMultipleObjects]
public class WallEditor : Editor
{
    int fc;
    void OnSceneGUI()
    {
        if (Application.isPlaying)
        {
            return;
        }
        Wall wall = (Wall)target;
        for (int i = 0; i < wall.fixedPoints.Count; i++)
        {
            FixedPoint f = wall.fixedPoints[i];
            Vector2 oldloc = f.location;
            f.location = Handles.PositionHandle(f.location, Quaternion.identity);
            f.handle = Handles.PositionHandle(f.handle, Quaternion.identity);
            f.handle += f.location - oldloc;
            if (Handles.Button(f.location + Vector2.left * .5f, Quaternion.identity, .1f, .1f, Handles.RectangleHandleCap))
            {
                f.afterLocked ^= true;
            }
            if (Handles.Button(f.location + Vector2.right * .5f, Quaternion.identity, .1f, .1f, Handles.RectangleHandleCap))
            {
                f.beforeLocked ^= true;
            }
            f.handle = f.location + (f.handle - f.location).normalized;
            Handles.DrawLine((2 * f.location - f.handle), f.handle);
            if (f.afterLocked)
            {
                Handles.DrawSolidDisc(f.handle, Vector3.back, .1f);
            }
            if (f.beforeLocked)
            {
                Handles.DrawSolidDisc((2 * f.location - f.handle), Vector3.back, .1f);
            }
            wall.fixedPoints[i] = f;
        }
        fc++;
        if (fc % 30 == 0)
        {
            for (int i = 1; i < wall.fixedPoints.Count; i++)
            {
                FixedPoint start = wall.fixedPoints[i];
                FixedPoint end;
                if (i + 1 == wall.fixedPoints.Count)
                {
                    end = wall.fixedPoints[0];
                }
                else
                {
                    end = wall.fixedPoints[i + 1];
                }
                Vector2 startNormal = Quaternion.AngleAxis(90, Vector3.back) * (start.handle - start.location);
                Vector2 diff = end.location - start.location;
                float dist = diff.magnitude;
                Vector2 circleDirection = Vector3.Project(diff, startNormal).normalized;
                if (circleDirection.SqrMagnitude() == 0)
                {
                    end.handle = end.location + start.handle - start.location;
                }
                else
                {
                    float centerAngle = Vector2.SignedAngle(circleDirection, diff);
                    float r = (dist / 2) / Mathf.Cos(Mathf.Deg2Rad * centerAngle);
                    Vector2 circleCenter = start.location + circleDirection * r;
                    float totalAngle = Wall.GetAngle(-circleDirection, end.location - circleCenter, Vector2.Dot(circleDirection, startNormal) < 0);
                    end.handle = (Vector3)end.location + Quaternion.AngleAxis(totalAngle, Vector3.back) * (start.handle - start.location);
                }
                if (i + 1 == wall.fixedPoints.Count)
                {
                    wall.fixedPoints[0] = end;
                }
                else
                {
                    wall.fixedPoints[i + 1] = end;
                }
            }
        }
        for (int i = 0; i < wall.fixedPoints.Count - 1; i++)
        {
            Handles.color = Color.red;
            Handles.DrawLine(wall.fixedPoints[i].location, wall.fixedPoints[i + 1].location);
        }
    }
}
// [PointHandle] public Vector2 start, end;
// public bool flip;
// public float depth = 1;
// void Start()
// {
//     MeshFilter mf = GetComponent<MeshFilter>();
//     mesh = mf.mesh;
//     Vector3[] verts = CreateLine(start, end, depth)
//     mesh.vertices = verts;

//     List<int> tris = new List<int>();
//     bool s = true;
//     int[] liveIndices = new int[] { 0, 1, verts.Length - 1 };
//     tris.AddRange(liveIndices);
//     while (liveIndices[1] != liveIndices[2])
//     {
//         if (s ^= true)
//         {
//             liveIndices[0] = liveIndices[1];
//             liveIndices[1]++;
//         }
//         else
//         {
//             liveIndices[0] = liveIndices[2];
//             liveIndices[2]--;
//         }
//         tris.AddRange(liveIndices);
//     }
//     mesh.triangles = tris.ToArray();
// }

// // Update is called once per frame
// void Update()
// {

// }

// Vector3[] CreateLine()
// {
//     float dpu = 5;
//     int dots = (int)(Vector3.Distance(start, end) * dpu);
//     Vector3 outward = Quaternion.AngleAxis(90, Vector3.up) * (end - start);

//     Vector3[] line = new Vector3[dots * 2];
//     for (int i = 0; i < dots; i++)
//     {
//         line[i] = Vector3.MoveTowards(start, end, dpu * i);
//         line[line.Length - 1 - i] = Vector3.MoveTowards(start, end, dpu * i) + outward * depth;
//     }
//     return line;
// }



