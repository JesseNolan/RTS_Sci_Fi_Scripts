using UnityEngine;
using System.Collections;

public class meshProject : MonoBehaviour {


    public Vector3[] vertices;

    public int xSize;
    public int zSize;

    public float offset;

    int terrainLayerMask = 1 << 10;

    Terrain terrain;

    Mesh mesh;
	
	// Update is called once per frame
    void Start()
    {
        terrain = TerrainSystem.terrain;
        mesh = meshGenerate(xSize, zSize);

        Vector3 localPos = gameObject.transform.localPosition;

        localPos.x -= xSize / 2;
        localPos.z -= zSize / 2;

        gameObject.transform.localPosition = localPos;
    }

    void Update()
    {
        //terrain = TerrainGenerator.theTerrain;
        mapMeshToTerrain(mesh, xSize, zSize, terrain.terrainData);

        Vector3 pos = gameObject.transform.position;

        pos.y = offset;

        gameObject.transform.position = pos;

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }


    private void mapMeshToTerrain(Mesh mesh, int cols, int rows, TerrainData data)
    {
        Vector3[] verts = mesh.vertices;

        Vector3 terrainSize = data.size;

        rows = rows * 2;
        cols = cols * 2;

        Vector3 gridObjPos = this.transform.position;

        //Vector3 terrainPos = ...  // assume to be at 0,0

        float origX = gridObjPos.x / terrainSize.x;
        float origZ = gridObjPos.z / terrainSize.z;

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= cols; x ++)
            {
                int i = x + y * (cols + 1);        

                Vector3 localPos = verts[i];
                Vector3 worldPos = transform.TransformPoint(localPos);

                //get the terrain height at that point
                // set the mesh height at that point
               
                float meshSpacing = 0.5f;
                float xPos = (float)x * meshSpacing / terrainSize.x;
                float yPos = (float)y * meshSpacing / terrainSize.z;

                float height = data.GetInterpolatedHeight(xPos + origX, yPos + origZ);         

                verts[i].y = height;
            }
        }
        mesh.vertices = verts;
    }


    private Mesh meshGenerate(int xSize, int zSize)
    {
        Mesh dst = new Mesh();

        Vector3[] vertices = new Vector3[(xSize*2 + 1) * (zSize*2 + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        for (int i = 0, z = 0; z <= zSize*2; z++)
        {
            for (int x = 0; x <= xSize*2; x++, i++)
            {
                vertices[i] = new Vector3((float)x / 2, 0, (float)z / 2);
                uv[i] = new Vector2((float)x / (xSize * 2), (float)z / (zSize * 2));
                tangents[i] = tangent;
            }
        }


        dst.vertices = vertices;
        dst.uv = uv;
        dst.tangents = tangents;

        int[] triangles = new int[xSize * 2 * zSize * 2 * 6];
        for (int ti = 0, vi = 0, y = 0; y < zSize * 2; y++, vi++)
        {
            for (int x = 0; x < xSize * 2; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize * 2 + 1;
                triangles[ti + 5] = vi + xSize * 2 + 2;
            }
        }
        dst.triangles = triangles;
        dst.RecalculateNormals();

        return dst;
    }


    //private Mesh meshGenerate(int xSize, int zSize, int resolution)
    //{
    //    Mesh dst = new Mesh();

    //    float boxWidth = (float)xSize / resolution;

    //    Vector3[] vertices = new Vector3[(xSize + 1) * (zSize + 1)];
    //    Vector2[] uv = new Vector2[vertices.Length];
    //    Vector4[] tangents = new Vector4[vertices.Length];
    //    Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

    //    for (int i = 0, z = 0; z <= this.zSize; z++)
    //    {
    //        for (int x = 0; x <= xSize; x++, i++)
    //        {
    //            vertices[i] = new Vector3(x, 0, z);
    //            uv[i] = new Vector2((float)x / xSize, (float)z / zSize);
    //            tangents[i] = tangent;
    //        }
    //    }
    //    dst.vertices = vertices;
    //    dst.uv = uv;
    //    dst.tangents = tangents;

    //    int[] triangles = new int[xSize * zSize * 6];
    //    for (int ti = 0, vi = 0, y = 0; y < zSize; y++, vi++)
    //    {
    //        for (int x = 0; x < xSize; x++, ti += 6, vi++)
    //        {
    //            triangles[ti] = vi;
    //            triangles[ti + 3] = triangles[ti + 2] = vi + 1;
    //            triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
    //            triangles[ti + 5] = vi + xSize + 2;
    //        }
    //    }
    //    dst.triangles = triangles;
    //    dst.RecalculateNormals();

    //    return dst;
    //}


    //void Update ()
    //   {

    //       vertices = obj.GetComponent<MeshFilter>().sharedMesh.vertices;

    //       if (vertices == null)
    //       {
    //           Debug.Log("vertices is empty");
    //       } else
    //       {

    //           for (int i = 0; i < vertices.Length; i++)
    //           {
    //               vertices[i] = new Vector3(0, Random.Range(-1.0f, 1.0f));

    //           }


    //           obj.GetComponent<MeshFilter>().sharedMesh.vertices = vertices;


    //       }




    //}
}
