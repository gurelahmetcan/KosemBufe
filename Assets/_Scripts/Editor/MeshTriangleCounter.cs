using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Bufe
{
    /// <summary>
    /// Sahnedeki tum mesh'leri triangle sayisina gore siralayip Console'a doker.
    /// Menu: Tools > Perf > Log Scene Triangles
    /// </summary>
    public static class MeshTriangleCounter
    {
        private class Entry
        {
            public string path;
            public string mesh;
            public int triangles;
            public int instanceCount;   // ayni mesh'ten kac tane var
        }

        [MenuItem("Tools/Perf/Log Scene Triangles")]
        public static void LogSceneTriangles()
        {
            var perObject = new List<Entry>();
            var perMesh = new Dictionary<Mesh, Entry>();
            long total = 0;

            // Sadece render edilen (aktif ve enabled) mesh'leri say
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var renderer = mf.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled) continue;
                if (!mf.gameObject.activeInHierarchy) continue;

                int tris = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    tris += (int)(mesh.GetIndexCount(i) / 3);

                total += tris;

                perObject.Add(new Entry
                {
                    path = GetPath(mf.transform),
                    mesh = mesh.name,
                    triangles = tris
                });

                if (!perMesh.TryGetValue(mesh, out var e))
                {
                    e = new Entry { mesh = mesh.name, triangles = tris, instanceCount = 0 };
                    perMesh[mesh] = e;
                }
                e.instanceCount++;
            }

            // SkinnedMeshRenderer'lari da dahil et (karakter/animasyonlu mesh)
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || !smr.enabled || !smr.gameObject.activeInHierarchy) continue;

                int tris = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    tris += (int)(mesh.GetIndexCount(i) / 3);

                total += tris;
                perObject.Add(new Entry
                {
                    path = GetPath(smr.transform),
                    mesh = mesh.name + " (skinned)",
                    triangles = tris
                });
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== SCENE TRIANGLE REPORT ===  Toplam (aktif): {total:N0} tris");
            sb.AppendLine();

            sb.AppendLine("--- EN AGIR 25 OBJE (tek instance triangle sayisi) ---");
            foreach (var e in perObject.OrderByDescending(x => x.triangles).Take(25))
                sb.AppendLine($"{e.triangles,10:N0}  |  {e.mesh}  |  {e.path}");

            sb.AppendLine();
            sb.AppendLine("--- MESH BAZINDA TOPLAM MALIYET (triangle x instance) ---");
            foreach (var e in perMesh.Values
                         .OrderByDescending(x => (long)x.triangles * x.instanceCount)
                         .Take(25))
            {
                long meshTotal = (long)e.triangles * e.instanceCount;
                sb.AppendLine($"{meshTotal,12:N0}  |  {e.mesh}  ({e.instanceCount}x @ {e.triangles:N0} tris)");
            }

            Debug.Log(sb.ToString());
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
    }
}
