using System.Collections.Generic;
using UnityEngine;

namespace PokemonArena
{
    [ExecuteAlways]
    public class PokemonArenaBuilder : MonoBehaviour
    {
        [Header("Arena Dimensions")]
        public float fieldWidth = 18f;        // Battlefield turf width (X)
        public float fieldLength = 32f;       // Battlefield turf length (Z)
        public float apronMargin = 4f;        // Paved walkway around the pitch
        public float fenceHeight = 2.2f;      // Perimeter fence height
        public float postSpacing = 2.8f;      // Spacing between fence posts
        public float picketSpacing = 0.35f;   // Spacing between vertical fence slats

        [Header("Lighting & Features")]
        public bool buildFloodlights = true;
        public float floodlightHeight = 10.5f;
        public float floodlightIntensity = 3000f;
        public bool buildTrainerPodiums = true;
        public bool buildEntranceGates = true;
        public bool buildCornerLanterns = true;
        public bool placeSampleCharacter = true;

        [Header("Spawn Points (Auto-Generated)")]
        public Transform spawnTrainer1;
        public Transform spawnTrainer2;
        public Transform spawnPokemon1;
        public Transform spawnPokemon2;

        public void ClearArena()
        {
            // Remove existing children
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            foreach (var child in children)
            {
                if (Application.isEditor && !Application.isPlaying)
                {
                    DestroyImmediate(child);
                }
                else
                {
                    Destroy(child);
                }
            }
        }

        public void BuildArena()
        {
            ClearArena();

            // 1. Ensure textures and materials exist
            ArenaTextureGenerator.GenerateAllTextures();

            Material courtMat = ArenaMaterialManager.GetCourtMaterial();
            Material pavementMat = ArenaMaterialManager.GetPavementMaterial();
            Material woodMat = ArenaMaterialManager.GetFenceWoodMaterial();
            Material trimMat = ArenaMaterialManager.GetFenceTrimMaterial();
            Material metalMat = ArenaMaterialManager.GetDarkMetalMaterial();
            Material goldMat = ArenaMaterialManager.GetGoldTrimMaterial();
            Material redMat = ArenaMaterialManager.GetTrainerRedMaterial();
            Material blueMat = ArenaMaterialManager.GetTrainerBlueMaterial();
            Material lightLensMat = ArenaMaterialManager.GetEmissiveLightMaterial();
            Material lanternMat = ArenaMaterialManager.GetCornerLanternMaterial();
            Material outerGroundMat = ArenaMaterialManager.GetOuterGroundMaterial();

            // 2. Create Root Groups
            GameObject groundGroup = CreateGroup("Ground");
            GameObject fencesGroup = CreateGroup("PerimeterFences");
            GameObject podiumsGroup = CreateGroup("TrainerPodiums");
            GameObject lightingGroup = CreateGroup("StadiumLighting");
            GameObject spawnsGroup = CreateGroup("SpawnPoints");

            float halfFieldW = fieldWidth * 0.5f;
            float halfFieldL = fieldLength * 0.5f;
            float arenaWidth = fieldWidth + apronMargin * 2f;
            float arenaLength = fieldLength + apronMargin * 2f + 4f; // extra space for trainer platforms
            float halfArenaW = arenaWidth * 0.5f;
            float halfArenaL = arenaLength * 0.5f;

            // ----------------------------------------------------
            // 3. GROUND CONSTRUCTION
            // ----------------------------------------------------
            // Outer concourse/grass
            GameObject outerConcourse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerConcourse.name = "Outer_Concourse";
            outerConcourse.transform.SetParent(groundGroup.transform);
            outerConcourse.transform.localPosition = new Vector3(0, -0.15f, 0);
            outerConcourse.transform.localScale = new Vector3(arenaWidth + 24f, 0.2f, arenaLength + 24f);
            outerConcourse.GetComponent<MeshRenderer>().sharedMaterial = outerGroundMat;

            // Surrounding Apron (Pavement Walkway)
            GameObject apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "Surrounding_Apron";
            apron.transform.SetParent(groundGroup.transform);
            apron.transform.localPosition = new Vector3(0, -0.02f, 0);
            apron.transform.localScale = new Vector3(arenaWidth, 0.1f, arenaLength);
            apron.GetComponent<MeshRenderer>().sharedMaterial = pavementMat;

            // Battlefield Pitch (Lawn Turf with Court Texture)
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Battlefield_Pitch";
            pitch.transform.SetParent(groundGroup.transform);
            pitch.transform.localPosition = new Vector3(0, 0.05f, 0);
            pitch.transform.localScale = new Vector3(fieldWidth, 0.1f, fieldLength);
            pitch.GetComponent<MeshRenderer>().sharedMaterial = courtMat;

            // Low Stone Curb framing the pitch
            BuildCourtCurb(groundGroup.transform, halfFieldW, halfFieldL, metalMat);

            // ----------------------------------------------------
            // 4. PERIMETER FENCES (Closed Arena)
            // ----------------------------------------------------
            BuildSurroundingFences(fencesGroup.transform, halfArenaW, halfArenaL, woodMat, trimMat, metalMat, goldMat, lanternMat);

            // ----------------------------------------------------
            // 5. TRAINER PODIUMS (North and South)
            // ----------------------------------------------------
            if (buildTrainerPodiums)
            {
                float podiumZ = halfFieldL + 3.2f;
                // South (Trainer 1 - Red)
                GameObject podiumSouth = BuildTrainerPodium(podiumsGroup.transform, new Vector3(0, 0, -podiumZ), 0f, "TrainerPodium_Red", redMat, woodMat, goldMat, metalMat);
                // North (Trainer 2 - Blue)
                GameObject podiumNorth = BuildTrainerPodium(podiumsGroup.transform, new Vector3(0, 0, podiumZ), 180f, "TrainerPodium_Blue", blueMat, woodMat, goldMat, metalMat);
            }

            // ----------------------------------------------------
            // 6. STADIUM FLOODLIGHT TOWERS
            // ----------------------------------------------------
            if (buildFloodlights)
            {
                float lightX = halfArenaW + 1.8f;
                float lightZ = halfArenaL + 1.8f;

                BuildFloodlightTower(lightingGroup.transform, new Vector3(-lightX, 0, -lightZ), new Vector3(0, 45, 0), "Floodlight_SW", metalMat, lightLensMat);
                BuildFloodlightTower(lightingGroup.transform, new Vector3(lightX, 0, -lightZ), new Vector3(0, -45, 0), "Floodlight_SE", metalMat, lightLensMat);
                BuildFloodlightTower(lightingGroup.transform, new Vector3(-lightX, 0, lightZ), new Vector3(0, 135, 0), "Floodlight_NW", metalMat, lightLensMat);
                BuildFloodlightTower(lightingGroup.transform, new Vector3(lightX, 0, lightZ), new Vector3(0, -135, 0), "Floodlight_NE", metalMat, lightLensMat);
            }

            // ----------------------------------------------------
            // 7. COURT CORNER PYLONS / FLAGS
            // ----------------------------------------------------
            BuildCornerFlags(groundGroup.transform, halfFieldW, halfFieldL, metalMat, redMat, blueMat);

            // ----------------------------------------------------
            // 8. SPAWN POINTS
            // ----------------------------------------------------
            spawnTrainer1 = CreateSpawnPoint(spawnsGroup.transform, "Spawn_Trainer1", new Vector3(0, 0.65f, -(halfFieldL + 3.2f)), Quaternion.identity);
            spawnTrainer2 = CreateSpawnPoint(spawnsGroup.transform, "Spawn_Trainer2", new Vector3(0, 0.65f, (halfFieldL + 3.2f)), Quaternion.Euler(0, 180, 0));
            spawnPokemon1 = CreateSpawnPoint(spawnsGroup.transform, "Spawn_Pokemon1", new Vector3(0, 0.15f, -fieldLength * 0.22f), Quaternion.identity);
            spawnPokemon2 = CreateSpawnPoint(spawnsGroup.transform, "Spawn_Pokemon2", new Vector3(0, 0.15f, fieldLength * 0.22f), Quaternion.Euler(0, 180, 0));

            // ----------------------------------------------------
            // 9. SAMPLE CHARACTER PLACEMENT
            // ----------------------------------------------------
            if (placeSampleCharacter)
            {
                TryPlaceSampleCharacter(spawnTrainer1);
            }

            // ----------------------------------------------------
            // 10. SETUP CAMERA RIG
            // ----------------------------------------------------
            SetupArenaCamera();
        }

        #region Helper Creation Methods
        private GameObject CreateGroup(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private Transform CreateSpawnPoint(Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent);
            sp.transform.localPosition = pos;
            sp.transform.localRotation = rot;
            return sp.transform;
        }
        #endregion

        #region Curb Framing
        private void BuildCourtCurb(Transform parent, float halfW, float halfL, Material mat)
        {
            GameObject curbRoot = new GameObject("Court_Curbs");
            curbRoot.transform.SetParent(parent);

            float curbH = 0.08f;
            float curbW = 0.25f;

            // North and South curbs
            CreateBeam(curbRoot.transform, new Vector3(0, 0.06f, halfL), new Vector3(halfW * 2f + curbW * 2f, curbH, curbW), mat, "Curb_North");
            CreateBeam(curbRoot.transform, new Vector3(0, 0.06f, -halfL), new Vector3(halfW * 2f + curbW * 2f, curbH, curbW), mat, "Curb_South");

            // East and West curbs
            CreateBeam(curbRoot.transform, new Vector3(halfW, 0.06f, 0), new Vector3(curbW, curbH, halfL * 2f), mat, "Curb_East");
            CreateBeam(curbRoot.transform, new Vector3(-halfW, 0.06f, 0), new Vector3(curbW, curbH, halfL * 2f), mat, "Curb_West");
        }
        #endregion

        #region Fences Construction
        private void BuildSurroundingFences(Transform parent, float halfW, float halfL, Material woodMat, Material trimMat, Material metalMat, Material goldMat, Material lanternMat)
        {
            float gateWidth = 4.2f;

            // 1. East Fence (+X) - Solid
            BuildFenceLine(parent, new Vector3(halfW, 0, -halfL), new Vector3(halfW, 0, halfL), woodMat, trimMat, "Fence_East");

            // 2. West Fence (-X) - Solid
            BuildFenceLine(parent, new Vector3(-halfW, 0, -halfL), new Vector3(-halfW, 0, halfL), woodMat, trimMat, "Fence_West");

            // 3. South Fence (-Z) with Entrance Gate
            BuildFenceLine(parent, new Vector3(-halfW, 0, -halfL), new Vector3(-gateWidth * 0.5f, 0, -halfL), woodMat, trimMat, "Fence_South_Left");
            BuildFenceLine(parent, new Vector3(gateWidth * 0.5f, 0, -halfL), new Vector3(halfW, 0, -halfL), woodMat, trimMat, "Fence_South_Right");
            if (buildEntranceGates)
            {
                BuildEntranceGate(parent, new Vector3(0, 0, -halfL), 0f, gateWidth, "Gate_South", woodMat, trimMat, metalMat, goldMat);
            }

            // 4. North Fence (+Z) with Entrance Gate
            BuildFenceLine(parent, new Vector3(-halfW, 0, halfL), new Vector3(-gateWidth * 0.5f, 0, halfL), woodMat, trimMat, "Fence_North_Left");
            BuildFenceLine(parent, new Vector3(gateWidth * 0.5f, 0, halfL), new Vector3(halfW, 0, halfL), woodMat, trimMat, "Fence_North_Right");
            if (buildEntranceGates)
            {
                BuildEntranceGate(parent, new Vector3(0, 0, halfL), 180f, gateWidth, "Gate_North", woodMat, trimMat, metalMat, goldMat);
            }

            // 5. Four Sturdy Corner Pillars with glowing lanterns
            if (buildCornerLanterns)
            {
                BuildCornerPillar(parent, new Vector3(-halfW, 0, -halfL), woodMat, trimMat, lanternMat, "CornerPillar_SW");
                BuildCornerPillar(parent, new Vector3(halfW, 0, -halfL), woodMat, trimMat, lanternMat, "CornerPillar_SE");
                BuildCornerPillar(parent, new Vector3(-halfW, 0, halfL), woodMat, trimMat, lanternMat, "CornerPillar_NW");
                BuildCornerPillar(parent, new Vector3(halfW, 0, halfL), woodMat, trimMat, lanternMat, "CornerPillar_NE");
            }

            // 6. Invisible Arena Boundary Colliders (Guarantees closed arena)
            BuildBoundaryColliders(parent, halfW, halfL);
        }

        private void BuildFenceLine(Transform parent, Vector3 start, Vector3 end, Material woodMat, Material trimMat, string name)
        {
            GameObject fenceObj = new GameObject(name);
            fenceObj.transform.SetParent(parent);

            Vector3 delta = end - start;
            float totalDist = delta.magnitude;
            Vector3 dir = delta.normalized;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            int postCount = Mathf.Max(2, Mathf.RoundToInt(totalDist / postSpacing) + 1);
            float actualPostSpacing = totalDist / (postCount - 1);

            // 1. Posts
            for (int i = 0; i < postCount; i++)
            {
                Vector3 postPos = start + dir * (i * actualPostSpacing);
                BuildFencePost(fenceObj.transform, postPos, woodMat, trimMat);
            }

            // 2. Horizontal Rails (Top, Mid, Bottom)
            Vector3 midPoint = (start + end) * 0.5f;
            float railThickness = 0.08f;
            float railWidth = 0.12f;

            // Top Rail
            CreateBeamRotated(fenceObj.transform, midPoint + Vector3.up * (fenceHeight - 0.1f), new Vector3(railWidth, railThickness, totalDist), rot, woodMat, "TopRail");
            // Mid Rail
            CreateBeamRotated(fenceObj.transform, midPoint + Vector3.up * (fenceHeight * 0.5f), new Vector3(railWidth, railThickness, totalDist), rot, woodMat, "MidRail");
            // Bottom Rail
            CreateBeamRotated(fenceObj.transform, midPoint + Vector3.up * 0.25f, new Vector3(railWidth, railThickness, totalDist), rot, woodMat, "BottomRail");

            // 3. Vertical Pickets / Slats
            int picketCount = Mathf.RoundToInt(totalDist / picketSpacing);
            float actualPicketSpacing = totalDist / picketCount;
            float picketHeight = fenceHeight - 0.25f;

            for (int p = 0; p < picketCount; p++)
            {
                float distAlong = (p + 0.5f) * actualPicketSpacing;
                Vector3 picketPos = start + dir * distAlong + Vector3.up * (picketHeight * 0.5f + 0.15f);

                // Skip pickets that would intersect posts
                bool nearPost = false;
                for (int i = 0; i < postCount; i++)
                {
                    if (Mathf.Abs(distAlong - i * actualPostSpacing) < 0.15f)
                    {
                        nearPost = true;
                        break;
                    }
                }
                if (nearPost) continue;

                GameObject picket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                picket.name = $"Picket_{p}";
                picket.transform.SetParent(fenceObj.transform);
                picket.transform.position = picketPos;
                picket.transform.rotation = rot;
                picket.transform.localScale = new Vector3(0.04f, picketHeight, 0.09f);
                picket.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

                // Pointed cap on each picket
                GameObject pCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pCap.name = "Cap";
                pCap.transform.SetParent(picket.transform);
                pCap.transform.localPosition = new Vector3(0, 0.52f, 0);
                pCap.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                pCap.transform.localScale = new Vector3(0.12f, 0.6f, 0.9f);
                pCap.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                DestroyCollider(pCap);
            }
        }

        private void BuildFencePost(Transform parent, Vector3 pos, Material woodMat, Material trimMat)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(parent);
            post.transform.position = pos + Vector3.up * (fenceHeight * 0.5f);
            post.transform.localScale = new Vector3(0.22f, fenceHeight, 0.22f);
            post.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Decorative Post Cap (Pokéball colored trim)
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "PostCap";
            cap.transform.SetParent(post.transform);
            cap.transform.localPosition = new Vector3(0, 0.52f, 0);
            cap.transform.localScale = new Vector3(1.3f, 0.4f, 1.3f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(cap);

            // Top Finial Knob
            GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finial.name = "Finial";
            finial.transform.SetParent(post.transform);
            finial.transform.localPosition = new Vector3(0, 0.68f, 0);
            finial.transform.localScale = new Vector3(0.8f, 0.35f, 0.8f);
            finial.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(finial);
        }

        private void BuildCornerPillar(Transform parent, Vector3 pos, Material woodMat, Material trimMat, Material lanternMat, string name)
        {
            GameObject pillar = new GameObject(name);
            pillar.transform.SetParent(parent);
            pillar.transform.position = pos;

            float pillarH = fenceHeight + 0.6f;

            // Base Block
            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "Base";
            baseBlock.transform.SetParent(pillar.transform);
            baseBlock.transform.localPosition = new Vector3(0, pillarH * 0.5f, 0);
            baseBlock.transform.localScale = new Vector3(0.48f, pillarH, 0.48f);
            baseBlock.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Capital Crown
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crown.name = "Crown";
            crown.transform.SetParent(pillar.transform);
            crown.transform.localPosition = new Vector3(0, pillarH + 0.1f, 0);
            crown.transform.localScale = new Vector3(0.58f, 0.2f, 0.58f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(crown);

            // Glowing Crystal / Lantern Orb
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "LanternOrb";
            orb.transform.SetParent(pillar.transform);
            orb.transform.localPosition = new Vector3(0, pillarH + 0.4f, 0);
            orb.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            orb.GetComponent<MeshRenderer>().sharedMaterial = lanternMat;
            DestroyCollider(orb);

            // Optional subtle corner light
            Light pLight = orb.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.range = 8f;
            pLight.intensity = 150f;
            pLight.color = new Color(0.3f, 0.85f, 1.0f);
        }

        private void BuildEntranceGate(Transform parent, Vector3 pos, float yRotation, float width, string name, Material woodMat, Material trimMat, Material metalMat, Material goldMat)
        {
            GameObject gateObj = new GameObject(name);
            gateObj.transform.SetParent(parent);
            gateObj.transform.position = pos;
            gateObj.transform.rotation = Quaternion.Euler(0, yRotation, 0);

            float halfG = width * 0.5f;
            float pillarH = 3.6f;

            // Gate Left Pillar
            GameObject pLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeft.name = "GatePillar_L";
            pLeft.transform.SetParent(gateObj.transform);
            pLeft.transform.localPosition = new Vector3(-halfG, pillarH * 0.5f, 0);
            pLeft.transform.localScale = new Vector3(0.4f, pillarH, 0.4f);
            pLeft.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Gate Right Pillar
            GameObject pRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pRight.name = "GatePillar_R";
            pRight.transform.SetParent(gateObj.transform);
            pRight.transform.localPosition = new Vector3(halfG, pillarH * 0.5f, 0);
            pRight.transform.localScale = new Vector3(0.4f, pillarH, 0.4f);
            pRight.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Gold Finials on pillars
            CreateGateFinial(pLeft.transform, trimMat, goldMat);
            CreateGateFinial(pRight.transform, trimMat, goldMat);

            // Overhead Arch
            GameObject archBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archBeam.name = "ArchBeam";
            archBeam.transform.SetParent(gateObj.transform);
            archBeam.transform.localPosition = new Vector3(0, pillarH + 0.15f, 0);
            archBeam.transform.localScale = new Vector3(width + 0.4f, 0.25f, 0.35f);
            archBeam.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            DestroyCollider(archBeam);

            // Arch Peak Crown
            GameObject archPeak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            archPeak.name = "ArchPeak";
            archPeak.transform.SetParent(gateObj.transform);
            archPeak.transform.localPosition = new Vector3(0, pillarH + 0.45f, 0);
            archPeak.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            archPeak.transform.localScale = new Vector3(1.2f, 0.12f, 0.45f);
            archPeak.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(archPeak);

            // Center Pokéball Emblem Medallion
            GameObject medallion = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            medallion.name = "PokeBall_Medallion";
            medallion.transform.SetParent(gateObj.transform);
            medallion.transform.localPosition = new Vector3(0, pillarH + 0.45f, 0.08f);
            medallion.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            medallion.transform.localScale = new Vector3(0.85f, 0.08f, 0.85f);
            medallion.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            DestroyCollider(medallion);

            // Center Button of Medallion
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn.name = "CenterButton";
            btn.transform.SetParent(medallion.transform);
            btn.transform.localPosition = new Vector3(0, 0.55f, 0);
            btn.transform.localScale = new Vector3(0.28f, 0.3f, 0.28f);
            btn.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(btn);

            // Decorative Metal Gates (Ajar / welcoming opening)
            BuildGateWing(gateObj.transform, new Vector3(-halfG + 0.1f, 0, 0), 25f, width * 0.45f, metalMat, "GateWing_L");
            BuildGateWing(gateObj.transform, new Vector3(halfG - 0.1f, 0, 0), -155f, width * 0.45f, metalMat, "GateWing_R");
        }

        private void CreateGateFinial(Transform pillar, Material trimMat, Material goldMat)
        {
            GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finial.name = "GoldFinial";
            finial.transform.SetParent(pillar);
            finial.transform.localPosition = new Vector3(0, 0.58f, 0);
            finial.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            finial.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyCollider(finial);

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb";
            orb.transform.SetParent(pillar);
            orb.transform.localPosition = new Vector3(0, 0.72f, 0);
            orb.transform.localScale = new Vector3(0.8f, 0.35f, 0.8f);
            orb.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            DestroyCollider(orb);
        }

        private void BuildGateWing(Transform parent, Vector3 pivot, float yRot, float wingWidth, Material metalMat, string name)
        {
            GameObject wing = new GameObject(name);
            wing.transform.SetParent(parent);
            wing.transform.localPosition = pivot;
            wing.transform.localRotation = Quaternion.Euler(0, yRot, 0);

            float gateH = 2.0f;
            // Frame bars
            CreateBeam(wing.transform, new Vector3(wingWidth * 0.5f, gateH * 0.5f, 0), new Vector3(wingWidth, gateH, 0.05f), metalMat, "GatePanel");
        }

        private void BuildBoundaryColliders(Transform parent, float halfW, float halfL)
        {
            GameObject collidersObj = new GameObject("BoundaryColliders");
            collidersObj.transform.SetParent(parent);

            float wallH = 4.5f; // high enough to stop any player/creature jumping over
            float wallThickness = 0.5f;

            // North Wall
            CreateWallCollider(collidersObj.transform, new Vector3(0, wallH * 0.5f, halfL + wallThickness * 0.5f), new Vector3(halfW * 2f + 2f, wallH, wallThickness), "Collider_North");
            // South Wall
            CreateWallCollider(collidersObj.transform, new Vector3(0, wallH * 0.5f, -halfL - wallThickness * 0.5f), new Vector3(halfW * 2f + 2f, wallH, wallThickness), "Collider_South");
            // East Wall
            CreateWallCollider(collidersObj.transform, new Vector3(halfW + wallThickness * 0.5f, wallH * 0.5f, 0), new Vector3(wallThickness, wallH, halfL * 2f + 2f), "Collider_East");
            // West Wall
            CreateWallCollider(collidersObj.transform, new Vector3(-halfW - wallThickness * 0.5f, wallH * 0.5f, 0), new Vector3(wallThickness, wallH, halfL * 2f + 2f), "Collider_West");
        }

        private void CreateWallCollider(Transform parent, Vector3 pos, Vector3 size, string name)
        {
            GameObject colGo = new GameObject(name);
            colGo.transform.SetParent(parent);
            colGo.transform.position = pos;
            BoxCollider bc = colGo.AddComponent<BoxCollider>();
            bc.size = size;
        }
        #endregion

        #region Trainer Podiums
        private GameObject BuildTrainerPodium(Transform parent, Vector3 pos, float yRot, string name, Material teamMat, Material woodMat, Material goldMat, Material metalMat)
        {
            GameObject podium = new GameObject(name);
            podium.transform.SetParent(parent);
            podium.transform.position = pos;
            podium.transform.rotation = Quaternion.Euler(0, yRot, 0);

            float pW = 3.6f;
            float pD = 2.6f;
            float pH = 0.6f;

            // Main Raised Deck
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(podium.transform);
            deck.transform.localPosition = new Vector3(0, pH * 0.5f, 0);
            deck.transform.localScale = new Vector3(pW, pH, pD);
            deck.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Team Standing Circle
            GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.name = "TrainerRing";
            circle.transform.SetParent(podium.transform);
            circle.transform.localPosition = new Vector3(0, pH + 0.01f, 0.2f);
            circle.transform.localScale = new Vector3(2.2f, 0.02f, 2.2f);
            circle.GetComponent<MeshRenderer>().sharedMaterial = teamMat;
            DestroyCollider(circle);

            // Front Steps (2 steps descending forward toward the field)
            float stepD = 0.45f;
            float step1H = pH * 0.66f;
            float step2H = pH * 0.33f;

            CreateBeam(podium.transform, new Vector3(0, step1H * 0.5f, -(pD * 0.5f + stepD * 0.5f)), new Vector3(pW * 0.8f, step1H, stepD), woodMat, "Step1");
            CreateBeam(podium.transform, new Vector3(0, step2H * 0.5f, -(pD * 0.5f + stepD * 1.5f)), new Vector3(pW * 0.8f, step2H, stepD), woodMat, "Step2");

            // Back Safety Railing
            float railH = 1.0f;
            CreateBeam(podium.transform, new Vector3(0, pH + railH * 0.5f, pD * 0.5f - 0.05f), new Vector3(pW, railH, 0.08f), metalMat, "BackRail");
            // Left & Right Safety Railing
            CreateBeam(podium.transform, new Vector3(-pW * 0.5f + 0.05f, pH + railH * 0.5f, 0), new Vector3(0.08f, railH, pD), metalMat, "LeftRail");
            CreateBeam(podium.transform, new Vector3(pW * 0.5f - 0.05f, pH + railH * 0.5f, 0), new Vector3(0.08f, railH, pD), metalMat, "RightRail");

            // Trainer Command Console / Pedestal at the front
            GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
            console.name = "CommandConsole";
            console.transform.SetParent(podium.transform);
            console.transform.localPosition = new Vector3(0, pH + 0.55f, -pD * 0.5f + 0.4f);
            console.transform.localScale = new Vector3(0.8f, 1.1f, 0.4f);
            console.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            // Console Screen Display
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(console.transform);
            screen.transform.localPosition = new Vector3(0, 0.35f, -0.45f);
            screen.transform.localRotation = Quaternion.Euler(-25f, 0, 0);
            screen.transform.localScale = new Vector3(0.85f, 0.45f, 0.1f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = teamMat;
            DestroyCollider(screen);

            return podium;
        }
        #endregion

        #region Stadium Floodlights
        private void BuildFloodlightTower(Transform parent, Vector3 pos, Vector3 rotEuler, string name, Material metalMat, Material lensMat)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent);
            tower.transform.position = pos;
            tower.transform.rotation = Quaternion.Euler(rotEuler);

            float mastH = floodlightHeight;

            // 1. Concrete Foundation Base
            GameObject fBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fBase.name = "Foundation";
            fBase.transform.SetParent(tower.transform);
            fBase.transform.localPosition = new Vector3(0, 0.4f, 0);
            fBase.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);
            fBase.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            // 2. Main Vertical Mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(tower.transform);
            mast.transform.localPosition = new Vector3(0, mastH * 0.5f + 0.4f, 0);
            mast.transform.localScale = new Vector3(0.5f, mastH * 0.5f, 0.5f);
            mast.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            // 3. Floodlight Head Crossbar
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "LightRack";
            head.transform.SetParent(tower.transform);
            head.transform.localPosition = new Vector3(0, mastH + 0.4f, 0.4f);
            head.transform.localRotation = Quaternion.Euler(28f, 0, 0); // angled down toward field
            head.transform.localScale = new Vector3(2.6f, 0.3f, 0.3f);
            head.GetComponent<MeshRenderer>().sharedMaterial = metalMat;
            DestroyCollider(head);

            // 4. Three Spotlight Fixtures on the Crossbar
            float[] offsets = new float[] { -0.9f, 0f, 0.9f };
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fixture.name = $"SpotFixture_{i}";
                fixture.transform.SetParent(head.transform);
                fixture.transform.localPosition = new Vector3(offsets[i], 0.2f, 0.25f);
                fixture.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                fixture.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
                fixture.GetComponent<MeshRenderer>().sharedMaterial = metalMat;
                DestroyCollider(fixture);

                // Emissive Lens
                GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lens.name = "Lens";
                lens.transform.SetParent(fixture.transform);
                lens.transform.localPosition = new Vector3(0, -0.95f, 0);
                lens.transform.localScale = new Vector3(0.92f, 0.1f, 0.92f);
                lens.GetComponent<MeshRenderer>().sharedMaterial = lensMat;
                DestroyCollider(lens);
            }

            // 5. Functional Spotlight illuminating the arena
            GameObject lightGo = new GameObject("SpotLight");
            lightGo.transform.SetParent(head.transform);
            lightGo.transform.localPosition = new Vector3(0, 0, 0.5f);
            lightGo.transform.localRotation = Quaternion.Euler(0, 0, 0);

            Light spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 65f;
            spot.innerSpotAngle = 40f;
            spot.range = 45f;
            spot.intensity = floodlightIntensity;
            spot.color = new Color(1.0f, 0.97f, 0.92f);
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.85f;
        }
        #endregion

        #region Corner Flags
        private void BuildCornerFlags(Transform parent, float halfW, float halfL, Material metalMat, Material redMat, Material blueMat)
        {
            GameObject flags = new GameObject("CornerFlags");
            flags.transform.SetParent(parent);

            BuildFlag(flags.transform, new Vector3(-halfW, 0.1f, -halfL), metalMat, redMat, "Flag_SW");
            BuildFlag(flags.transform, new Vector3(halfW, 0.1f, -halfL), metalMat, redMat, "Flag_SE");
            BuildFlag(flags.transform, new Vector3(-halfW, 0.1f, halfL), metalMat, blueMat, "Flag_NW");
            BuildFlag(flags.transform, new Vector3(halfW, 0.1f, halfL), metalMat, blueMat, "Flag_NE");
        }

        private void BuildFlag(Transform parent, Vector3 pos, Material poleMat, Material flagMat, string name)
        {
            GameObject flagObj = new GameObject(name);
            flagObj.transform.SetParent(parent);
            flagObj.transform.position = pos;

            float poleH = 1.4f;
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(flagObj.transform);
            pole.transform.localPosition = new Vector3(0, poleH * 0.5f, 0);
            pole.transform.localScale = new Vector3(0.06f, poleH * 0.5f, 0.06f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;
            DestroyCollider(pole);

            // Flag cloth
            GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = "Cloth";
            cloth.transform.SetParent(flagObj.transform);
            cloth.transform.localPosition = new Vector3(0.2f, poleH - 0.2f, 0);
            cloth.transform.localScale = new Vector3(0.4f, 0.28f, 0.02f);
            cloth.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
            DestroyCollider(cloth);
        }
        #endregion

        #region Sample Character
        private void TryPlaceSampleCharacter(Transform targetSpawn)
        {
            if (targetSpawn == null) return;

#if UNITY_EDITOR
            string[] possiblePaths = new string[]
            {
                "Assets/model/charach.fbx",
                "Assets/model/chara2.fbx"
            };

            GameObject charaPrefab = null;
            foreach (var path in possiblePaths)
            {
                charaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (charaPrefab != null) break;
            }

            if (charaPrefab != null)
            {
                GameObject charaInst = UnityEditor.PrefabUtility.InstantiatePrefab(charaPrefab) as GameObject;
                if (charaInst != null)
                {
                    charaInst.name = "Trainer_Chara";
                    charaInst.transform.SetParent(targetSpawn);
                    charaInst.transform.localPosition = Vector3.zero;
                    charaInst.transform.localRotation = Quaternion.identity;
                    charaInst.transform.localScale = Vector3.one;
                }
            }
#endif
        }
        #endregion

        #region Camera Rig Setup
        private void SetupArenaCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            if (mainCam != null)
            {
                // Position for broad stadium overview
                mainCam.transform.position = new Vector3(0, 18f, -(fieldLength * 0.5f + 14f));
                mainCam.transform.rotation = Quaternion.Euler(32f, 0, 0);

                ArenaCameraController camCtrl = mainCam.GetComponent<ArenaCameraController>();
                if (camCtrl == null)
                {
                    camCtrl = mainCam.gameObject.AddComponent<ArenaCameraController>();
                }
                camCtrl.arenaCenter = transform;
            }
        }
        #endregion

        #region Geometry Utilities
        private static void CreateBeam(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent);
            beam.transform.localPosition = localPos;
            beam.transform.localScale = localScale;
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateBeamRotated(Transform parent, Vector3 worldPos, Vector3 scale, Quaternion rot, Material mat, string name)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent);
            beam.transform.position = worldPos;
            beam.transform.rotation = rot;
            beam.transform.localScale = scale;
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void DestroyCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isEditor && !Application.isPlaying) DestroyImmediate(col);
                else Destroy(col);
            }
        }
        #endregion
    }
}
