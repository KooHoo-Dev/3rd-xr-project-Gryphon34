using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnluckSoftware
{
    public class FraggedController : MonoBehaviour
    {
        [Header("Fragments")]
        [Tooltip("Can frags fall off, set to false for floors and walls where the frags only are suppose to rotate")]
        public bool fragEnabled = true;
        [Tooltip("Maximum force put on fracture fragments when fragged")]
        public int forceMax = 250;
        [Tooltip("Minimun force put on fracture fragments when fragged")]
        public int forceMin = 50;
        [Tooltip("Scales fracture fragments after fragged")]
        public float fragOffScale = 1.0f;
        [Tooltip("Rotates randomly when hit (random degrees * fracture.hitPoints(0-1))")]
        public int rotateOnHit = 10;
        [Tooltip("Each fracture fragments mass")]
        public float fragMass = 0.01f;
        [Tooltip("Amount decreased from fracture fragment hitPoints(1 =100%) on mouse over or *collide magnitude")]
        public float hitPointDecrease = .2f;
        [Tooltip("When using Box Colliders they sometimes overlap when activated and makes fragments fly far far away, this limitation will fix that")]
        public float limitFragmentSpeed = 25.0f;
        [Tooltip("Destroys the entire object on any amount of damage.")]
        public bool fragAllOnDamage;


        [Header("Collisions")]
        [Tooltip("//Fracture fragments on collision magnitude (0 disabled, 5 good, 25 max)")]
        public int collidefragMagnitude = 0;
        [Tooltip("	//Colliders in theese layers that can fragment object")]
        public LayerMask collideMask;

        [Header("Particles")]
        [Tooltip("")]
        public Vector2 fragEmitMinMax = new Vector2(2.0f, 4.0f);


        [Header("Connections")]
        [Tooltip("Everything not connected to the bottom fragments frags off")]
        public int stickyTop;
        [Tooltip("Everything not connected to the top fragments frags off")]
        public int stickyBottom;
        [Tooltip("How many fragments needs to be connected before they break appart")]
        public int connectedFragments = 3;
        [Tooltip("Size of sphere connecting fragments")]
        public float connectOverlapSphere = .5f;
        [Tooltip("Should contain layer containing fragments")]
        public LayerMask stickyMask = (LayerMask)1;

        [Header("After Fragment")]
        [Tooltip("Once hitting a gameobject of this type the collider will be disabled.")]
        public LayerMask disableMask;
        [Tooltip("Disables fracture fragments after fragged, 0 never disable. (seconds)")]
        public float disableDelay = 0.0f;
        [Tooltip("Combine frags to a single mesh.")]
        public bool combineFrags = true;
        [Tooltip("Combines all fragments to one mesh after last fragged fragment+delay [seconds/10] (performance+++) (negative/zero=don't merge)")]
        public int combineMeshesDelay = 3;

        public Transform startMesh;             //Original mesh used when not fragmented
        public ParticleSystem fragParticles;    //This will play on every frag
        public ParticleSystem dustParticles;    //This will play on every hit
        [HideInInspector]
        public int reCounter = 1;               //Counter for recombining fragments
        [HideInInspector]
        public GameObject combinedFrags;        //Stores combined mesh
        [HideInInspector]
        public MeshFilter[] meshFilters;
        [HideInInspector]
        public FraggedChild[] childFrags;       //Cached child components for performance
        public Transform fragments;

        // Cached references for performance (avoids repeated GetComponent calls)
        private Renderer startMeshRenderer;
        private bool hasFrags;

        [Header("Change Materials")]
        [Tooltip("")]
        public Material[] fragMaterials;

        [Header("Sounds")]
        [Tooltip("Sound plays on impact, scales with magnitude")]
        public AudioClip impactSound;
        [Tooltip("")]
        public float maxPitch = 1.0f;
        [Tooltip("")]
        public float minPitch = 1.0f;
        [Tooltip("")]
        public float volume = 1f;
        [Tooltip("")]
        public float minVelocity = 1f;


        /// RESET FRAGMENTED OBJECT
        public void ResetFrags()
        {
            if (startMeshRenderer != null) startMeshRenderer.enabled = true;
            if (fragParticles != null) fragParticles.Clear();
            for (int i = 0; i < childFrags.Length; i++)
            {
                childFrags[i].resetMe();
            }
            if (combinedFrags != null)
            {
                Destroy(combinedFrags);
                combinedFrags = null;
            }
            if (startMeshRenderer != null)
            {
                reCounter = 0;
                InvokeRepeating(nameof(reCombine), 1.0f, 1.0f);
            }
        }

        public void Start()
        {
            fragments = transform.Find("Fragments");
            if (fragments == null)
            {
                Debug.LogError("[Fragmented Objects] Could not find 'Fragments' child transform. Make sure the prefab structure is intact.");
                return;
            }

            Transform particleFragment = transform.Find("Particles Fragment");
            if (particleFragment != null)
            {
                fragParticles = particleFragment.GetComponent<ParticleSystem>();
                if (fragParticles != null) fragParticles.Stop();
            }

            Transform particleDust = transform.Find("Particles Dust");
            if (particleDust != null)
            {
                dustParticles = particleDust.GetComponent<ParticleSystem>();
                if (dustParticles != null) dustParticles.Stop();
            }

            startMesh = transform.Find("Original Mesh");
            if (startMesh != null) startMeshRenderer = startMesh.GetComponent<Renderer>();

            // Cache all child components once for performance (avoids GetComponent/GetChild in every FixedUpdate)
            int childCount = fragments.childCount;
            meshFilters = fragments.GetComponentsInChildren<MeshFilter>(true);
            childFrags = new FraggedChild[childCount];
            for (int i = 0; i < childCount; i++)
            {
                childFrags[i] = fragments.GetChild(i).GetComponent<FraggedChild>();
            }
            hasFrags = childCount > 0;

            FindSticky();
            if (startMesh == null) CombineFrags();
            InvokeRepeating(nameof(reCombine), 1.0f, 1.0f);
            ChangeMaterials();
        }

        public void ChangeMaterials()
        {
            if (fragMaterials == null || fragMaterials.Length == 0) return;
            Material[] mats = fragMaterials;
            for (int i = 0; i < childFrags.Length; i++)
            {
                Renderer r = childFrags[i].GetComponent<Renderer>();
                if (r != null) r.sharedMaterials = mats;
            }
        }

        public void FixedUpdate()
        {
            if (limitFragmentSpeed > 0 && hasFrags && combinedFrags == null && startMeshRenderer != null && !startMeshRenderer.enabled)
            {
                for (int i = childFrags.Length - 1; i >= 0; i--)
                {
                    childFrags[i].SpeedCheck();
                }
            }
        }

        public int Compare(float first, float second)
        {
            return second.CompareTo(first);
        }

        public void FragAll()
        {
            if (!hasFrags) return;
            if (startMeshRenderer != null) startMeshRenderer.enabled = false;
            EnableRenderers();
            ReleaseFrags(false);
            reCounter = 0;
            for (int i = childFrags.Length - 1; i >= 0; i--)
            {
                childFrags[i].fragMe(200.0f);
            }
        }

        public void checkConnections()
        {
            if (stickyTop > 0 || stickyBottom > 0)
            {
                FraggedChild frag = null;
                for (int i = stickyTop; i < meshFilters.Length; i++)
                {
                    frag = meshFilters[i].GetComponent<FraggedChild>();
                    frag.connected = false;
                }
                for (int j = stickyTop; j < meshFilters.Length; j++)
                {
                    frag.checkConnections();
                    frag = meshFilters[j].GetComponent<FraggedChild>();
                }
                for (int u = meshFilters.Length - stickyBottom - 1; u >= stickyTop; u--)
                {
                    frag = meshFilters[u].GetComponent<FraggedChild>();
                    if (!frag.fragged)
                    {
                        frag.checkConnections();
                        if (!frag.connected) frag.fragMe(2.0f);
                    }
                }
            }
        }

        public void FindSticky()
        {
            if (stickyTop > 0 || stickyBottom > 0)
            {
                meshFilters = meshFilters.OrderByDescending(x => x.transform.position.y).ToArray();
                for (int j = 0; j < stickyTop; j++)
                {
                    FraggedChild g = meshFilters[j].GetComponent<FraggedChild>();
                    g.stickyFrag = true;
                }
                for (int i = meshFilters.Length - stickyBottom; i < meshFilters.Length; i++)
                {
                    FraggedChild k = meshFilters[i].GetComponent<FraggedChild>();
                    k.stickyFrag = true;
                }
            }
        }

        public void EnableRenderers()
        {
            for (int i = 0; i < childFrags.Length; i++)
            {
                Renderer r = childFrags[i].GetComponent<Renderer>();
                if (r != null) r.enabled = true;
            }
        }

        //// COMBINE FRAGMENTS TO A SINGLE MESH
        public void CombineFrags()
        {
            if ((combinedFrags == null) && (startMeshRenderer == null || !startMeshRenderer.enabled))
            {
                combinedFrags = new GameObject();
                combinedFrags.name = "Combined Fragments";
                combinedFrags.gameObject.AddComponent<MeshFilter>();
                combinedFrags.gameObject.AddComponent<MeshRenderer>();
                if (meshFilters.Length == 0)
                {
                    meshFilters = fragments.GetComponentsInChildren<MeshFilter>(true);
                }
                List<Material> materials = new List<Material>();
                List<List<CombineInstance>> combineInstanceArrays = new List<List<CombineInstance>>();
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    MeshFilter[] meshFilterss = meshFilters[i].GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter meshFilter in meshFilterss)
                    {
                        MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                        if (meshRenderer == null) continue;
                        FraggedChild c = meshFilter.transform.GetComponent<FraggedChild>();
                        if ((c != null) && c.fragged == false || (c != null) && combineFrags)
                        {
                            c.fragged = false;
                            c.hitPoints = 1.0f;
                            Renderer r = meshFilters[i].transform.gameObject.GetComponent<Renderer>();
                            if (r != null) r.enabled = false;
                            Rigidbody rb = meshFilters[i].GetComponent<Rigidbody>();
                            if (rb != null) rb.isKinematic = true;
                            for (int o = 0; o < meshFilter.sharedMesh.subMeshCount; o++)
                            {
                                Material mat = meshRenderer.sharedMaterials[o];
                                if (mat == null) continue;
                                int materialArrayIndex = -1;
                                for (int mi = 0; mi < materials.Count; mi++)
                                {
                                    if (materials[mi].name == mat.name)
                                    {
                                        materialArrayIndex = mi;
                                        break;
                                    }
                                }
                                if (materialArrayIndex == -1)
                                {
                                    materials.Add(mat);
                                    combineInstanceArrays.Add(new List<CombineInstance>());
                                    materialArrayIndex = materials.Count - 1;
                                }
                                CombineInstance combineInstance = new CombineInstance();
                                combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                                combineInstance.subMeshIndex = o;
                                combineInstance.mesh = meshFilter.sharedMesh;
                                combineInstanceArrays[materialArrayIndex].Add(combineInstance);
                            }
                        }
                    }
                }
                Mesh[] meshes = new Mesh[materials.Count];
                CombineInstance[] combineInstances = new CombineInstance[materials.Count];
                for (int m = 0; m < materials.Count; m++)
                {
                    CombineInstance[] combineInstanceArray = combineInstanceArrays[m].ToArray();
                    meshes[m] = new Mesh();
                    meshes[m].CombineMeshes(combineInstanceArray, true, true);
                    combineInstances[m] = new CombineInstance();
                    combineInstances[m].mesh = meshes[m];
                    combineInstances[m].subMeshIndex = 0;
                }
                combinedFrags.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                combinedFrags.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combineInstances, false, false);
                for (int mi = 0; mi < meshes.Length; mi++)
                {
                    meshes[mi].Clear();
                    DestroyImmediate(meshes[mi]);
                }
                MeshRenderer meshRendererCombine = combinedFrags.GetComponent<MeshRenderer>();
                if (meshRendererCombine == null) meshRendererCombine = gameObject.AddComponent<MeshRenderer>();
                meshRendererCombine.materials = materials.ToArray();
                if (Application.isEditor && !Application.isPlaying && combinedFrags.transform.parent != transform) combinedFrags.transform.parent = transform;
            }
        }

        public void ReleaseFrags(bool editor)
        {
            if (combinedFrags != null)
            {
                for (int i = 0; i < childFrags.Length; i++)
                {
                    Renderer r = childFrags[i].GetComponent<Renderer>();
                    if (r != null) r.enabled = true;
                }
                Destroy(combinedFrags);
                combinedFrags = null;
            }
        }

        public void reCombine()
        {
            if (startMeshRenderer == null || !startMeshRenderer.enabled)
            {
                if (combineMeshesDelay >= 0 && combinedFrags == null && reCounter > combineMeshesDelay)
                {
                    CombineFrags();
                    CancelInvoke(nameof(reCombine));
                }
                else if (reCounter <= combineMeshesDelay)
                {
                    reCounter++;
                }
            }
        }
    }
}