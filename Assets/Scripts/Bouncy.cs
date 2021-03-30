using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using mattatz.Triangulation2DSystem;
public class Bouncy : MonoBehaviour
{
    List<Point> points = new List<Point>();
    public Texture2D shape;
    public float scale = .2f;
    public Player player;
    void Start()
    {
        Color[] pixels = shape.GetPixels();
        bool[,] needsVert = new bool[shape.width, shape.height];
        for (int x = 0; x < shape.width; x++)
        {
            for (int y = 0; y < shape.height; y++)
            {
                if (pixels[x + y * shape.width].Equals(Color.black))
                {
                    points.Add(new Point(new Vector2(x * scale, y * scale)));
                }
            }
        }
        Point p = points[0];
        while (p != null)
        {
            float min = float.MaxValue;
            Point closest = null;
            foreach (Point o in points)
            {
                float dist;
                if (p != o && p.before != o && o.before == null && (dist = Vector2.Distance(p.home, o.home)) < min)
                {
                    min = dist;
                    closest = o;
                }
            }
            if (closest != null)
            {
                p.after = closest;
                closest.before = p;
            }
            p = closest;
        }
        points = points.OrderBy(po => {
            return Vector2.SignedAngle(Vector2.up, po.home - new Vector2(shape.width, shape.height) * scale / 2);
        }).ToList();
    }

    void CreateMesh()
    {
        Vector2[] vertices2D = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            vertices2D[i] = points[i].home + points[i].offset;
        }
        Triangulator tr = new Triangulator(vertices2D);
        int[] indices = tr.Triangulate();
 
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[vertices2D.Length];
        for (int i=0; i<vertices.Length; i++) {
            vertices[i] = new Vector3(vertices2D[i].x, vertices2D[i].y, 0);
        }
 
        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;
    }

    void Update(){
                CreateMesh();

    }

    void OnDrawGizmos()
    {
        foreach (Point p in points)
        {
            Gizmos.DrawLine(p.home + p.offset, p.after.home + p.after.offset);
            // Gizmos.color = Color.black;
            // Gizmos.DrawSphere(p.home, .02f);
            // Gizmos.color = Color.red;
            // Gizmos.DrawLine(p.home,p.before.home);
        }
        foreach (Point p in points)
        {
            Gizmos.DrawSphere(p.home + p.offset, .02f);
        }

    }

    void FixedUpdate()
    {
        foreach (Point p in points)
        {
            Vector2 pushDir = p.home + p.offset - (Vector2)player.transform.position;

            if (pushDir.sqrMagnitude <= player.radius * player.radius)
            {
                // Debug.DrawRay(player.transform.position, pushDir.normalized, Color.red);
                // Debug.DrawRay(player.transform.position, player.velocity.normalized, Color.blue);
                if (Vector2.Dot(pushDir, player.velocity) >= 0)
                {
                    p.Push(Vector3.Project(player.velocity, pushDir));
                }
                if (Vector2.Dot(pushDir, p.vel) < 0)
                {
                    player.velocity += (Vector2)Vector3.Project(p.vel, pushDir) - (Vector2)Vector3.Project(player.velocity, pushDir);
                    //player.velocity += (Vector2)Vector3.Project(p.vel, pushDir) * .5f;

                }
                else
                {
                    player.velocity -= (Vector2)Vector3.Project(player.velocity, pushDir) * .2f; //end constant is how thick gel
                }
                Vector2 jump = pushDir.normalized * -1 * (player.radius - pushDir.magnitude);
                player.transform.position += (Vector3)jump;
            }
            p.Update();
        }
    }

    class Point
    {
        public Vector2 home, offset;
        public Vector2 vel;
        public Point before, after;
        float bDist, aDist;
        static float springiness = 90, damping = 10, connectionSpringiness = 90, connectionStiffness = .2f;

        public Point(Vector2 _home)
        {
            home = _home;
        }

        public void Update()
        {
            if (bDist == 0)
            {
                bDist = Vector2.Distance(home, before.home);
                aDist = Vector2.Distance(home, after.home);
            }

            Vector2 acceleration = Spring(springiness, damping, offset, vel); //sprint towards home
            acceleration += Spring(connectionSpringiness, damping, (after.home + after.offset - offset - home) * (aDist - Vector2.Distance(home + offset, after.home + after.offset)), vel - after.vel);
            acceleration += Spring(connectionSpringiness, damping, (before.home + before.offset - offset - home) * (bDist - Vector2.Distance(home + offset, before.home + before.offset)), vel - before.vel);
            vel += acceleration * Time.fixedDeltaTime;
            offset += vel * Time.fixedDeltaTime;

            before.vel += vel * Time.fixedDeltaTime * connectionStiffness * 2;
            after.vel += vel * Time.fixedDeltaTime * connectionStiffness * 2;
        }

        Vector2 Spring(float springConstant, float damping, Vector2 offset, Vector2 relativeVel)
        {
            return -springConstant * offset + -damping * relativeVel;
        }

        public void Push(Vector2 p)
        {
            vel += p;
            if (p.sqrMagnitude > .01f)
            {
                before.Push(p * connectionStiffness);
                after.Push(p * connectionStiffness);
            }
        }
    }
}


