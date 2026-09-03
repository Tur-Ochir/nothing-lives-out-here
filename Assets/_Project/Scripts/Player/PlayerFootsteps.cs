using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles playing surface-dependent footstep sound effects based on player movement distance.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    public enum SurfaceType
    {
        Default,
        Snow,
        Wood,
        DirtyGround,
        Grass,
        Gravel,
        Rock,
        Water
    }

    [System.Serializable]
    public class SurfaceSoundGroup
    {
        public SurfaceType surfaceType;
        public string[] nameOrTagKeywords;
        public AudioClip[] walkClips;
        public AudioClip[] runClips;
    }

    [Header("Cadence Settings")]
    [Tooltip("Base distance in meters required to trigger one footstep sound while walking.")]
    public float walkStepDistance = 1.6f;
    [Tooltip("Distance multiplier when crouching.")]
    public float crouchStepMultiplier = 0.85f;
    [Tooltip("Minimum velocity required to accumulate footstep distance.")]
    public float minVelocityThreshold = 0.2f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float walkVolume = 0.8f;
    [Range(0f, 1f)] public float crouchVolume = 0.4f;
    public float minPitch = 0.94f;
    public float maxPitch = 1.06f;

    [Header("Raycast / Detection")]
    public float raycastDistance = 1.5f;
    public LayerMask groundLayers = ~0; // Default everything

    [Header("Surface Sound Configurations")]
    public List<SurfaceSoundGroup> surfaces = new List<SurfaceSoundGroup>();

    [Header("Default Fallback Sounds")]
    public AudioClip[] defaultWalkClips;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private AudioSource audioSource;
    private float distanceTraveled = 0f;
    private int lastPlayedIndex = -1;
    private SurfaceType lastDetectedSurface = SurfaceType.Default;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        // Ensure dedicated AudioSource for footsteps
        AudioSource[] sources = GetComponents<AudioSource>();
        foreach (var src in sources)
        {
            // If there's an unused audio source or create new
            if (!src.loop && src.clip == null)
            {
                audioSource = src;
                break;
            }
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D for first-person player immersion
    }

    private void Start()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.RegisterAudioSource(audioSource, SoundManager.SoundCategory.SFX, 1f);
        }

        // Initialize default surface presets if empty
        if (surfaces == null || surfaces.Count == 0)
        {
            InitializeDefaultKeywords();
        }
    }

    private void OnDestroy()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.UnregisterAudioSource(audioSource);
        }
    }

    private void Update()
    {
        // Only process if moving on ground and movement is enabled
        if (playerMovement != null && !playerMovement.canMove)
        {
            distanceTraveled = 0f;
            return;
        }

        ProcessFootstepAccumulation();
    }

    private void ProcessFootstepAccumulation()
    {
        if (controller == null) return;

        // Calculate horizontal velocity
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        // If controller.velocity is 0 (due to SimpleMove in some frames), fall back to move direction * speed
        if (speed < minVelocityThreshold && playerMovement != null && playerMovement.MoveDirection.sqrMagnitude > 0.01f)
        {
            speed = playerMovement.speed * playerMovement.MoveDirection.magnitude;
        }

        bool isGrounded = controller.isGrounded;

        if (isGrounded && speed >= minVelocityThreshold)
        {
            bool isCrouching = playerMovement != null && playerMovement.IsCrouching;
            float targetStepDistance = walkStepDistance * (isCrouching ? crouchStepMultiplier : 1f);

            distanceTraveled += speed * Time.deltaTime;

            if (distanceTraveled >= targetStepDistance)
            {
                distanceTraveled = 0f;
                PlayFootstepSound(isCrouching);
            }
        }
        else if (!isGrounded)
        {
            // Reset distance slightly when in air
            distanceTraveled = Mathf.Min(distanceTraveled, walkStepDistance * 0.5f);
        }
    }

    public void PlayFootstepSound(bool isCrouching = false)
    {
        SurfaceType currentSurface = DetectSurface();
        AudioClip clipToPlay = GetClipForSurface(currentSurface);

        if (clipToPlay == null)
        {
            clipToPlay = GetRandomClip(defaultWalkClips);
        }

        if (clipToPlay == null) return;

        // Apply pitch & volume nuances
        float pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        float volume = isCrouching ? crouchVolume : walkVolume;

        if (audioSource != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clipToPlay, volume);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clipToPlay, volume, pitch);
        }
    }

    private SurfaceType DetectSurface()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            // Check if hit Terrain
            if (hit.collider is TerrainCollider || hit.collider.GetComponent<Terrain>() != null)
            {
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                if (terrain != null && terrain.terrainData != null)
                {
                    string dominantLayer = GetDominantTerrainLayerName(terrain, hit.point);
                    if (!string.IsNullOrEmpty(dominantLayer))
                    {
                        foreach (var group in surfaces)
                        {
                            if (group.nameOrTagKeywords == null) continue;
                            foreach (var kw in group.nameOrTagKeywords)
                            {
                                if (!string.IsNullOrEmpty(kw) && dominantLayer.Contains(kw.ToLowerInvariant()))
                                {
                                    lastDetectedSurface = group.surfaceType;
                                    return group.surfaceType;
                                }
                            }
                        }
                    }
                }

                lastDetectedSurface = SurfaceType.Snow;
                return SurfaceType.Snow;
            }

            string colName = hit.collider.name.ToLowerInvariant();
            string tag = hit.collider.tag.ToLowerInvariant();
            string matName = "";

            if (hit.collider.sharedMaterial != null)
            {
                matName = hit.collider.sharedMaterial.name.ToLowerInvariant();
            }

            var renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                matName += " " + renderer.sharedMaterial.name.ToLowerInvariant();
            }

            // Match with configured surfaces
            foreach (var group in surfaces)
            {
                if (group.nameOrTagKeywords == null) continue;

                foreach (var kw in group.nameOrTagKeywords)
                {
                    if (string.IsNullOrEmpty(kw)) continue;
                    string lowerKw = kw.ToLowerInvariant();

                    if (colName.Contains(lowerKw) || tag.Contains(lowerKw) || matName.Contains(lowerKw))
                    {
                        lastDetectedSurface = group.surfaceType;
                        return group.surfaceType;
                    }
                }
            }
        }

        return lastDetectedSurface != SurfaceType.Default ? lastDetectedSurface : SurfaceType.Snow;
    }

    private string GetDominantTerrainLayerName(Terrain terrain, Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null || terrainData.alphamapLayers == 0) return string.Empty;

        Vector3 terrainPos = worldPos - terrain.transform.position;

        // Map world coordinates to alphamap/splatmap grid
        int mapX = Mathf.Clamp(Mathf.FloorToInt((terrainPos.x / terrainData.size.x) * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
        int mapZ = Mathf.Clamp(Mathf.FloorToInt((terrainPos.z / terrainData.size.z) * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
        int numLayers = terrainData.terrainLayers.Length;
        if (numLayers == 0) return string.Empty;

        int dominantIndex = 0;
        float maxWeight = 0f;

        for (int i = 0; i < numLayers; i++)
        {
            float weight = splatmapData[0, 0, i];
            if (weight > maxWeight)
            {
                maxWeight = weight;
                dominantIndex = i;
            }
        }

        TerrainLayer layer = terrainData.terrainLayers[dominantIndex];
        if (layer != null)
        {
            string layerInfo = layer.name;
            if (layer.diffuseTexture != null)
            {
                layerInfo += " " + layer.diffuseTexture.name;
            }
            return layerInfo.ToLowerInvariant();
        }

        return string.Empty;
    }

    private AudioClip GetClipForSurface(SurfaceType surface)
    {
        foreach (var group in surfaces)
        {
            if (group.surfaceType == surface)
            {
                return GetRandomClip(group.walkClips);
            }
        }
        return null;
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int index = UnityEngine.Random.Range(0, clips.Length);
        if (index == lastPlayedIndex)
        {
            index = (index + 1) % clips.Length;
        }
        lastPlayedIndex = index;
        return clips[index];
    }

    private void InitializeDefaultKeywords()
    {
        surfaces = new List<SurfaceSoundGroup>
        {
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.Wood,
                nameOrTagKeywords = new string[] { "wood", "floor", "ger", "plank", "table", "bed", "box", "chest" }
            },
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.Snow,
                nameOrTagKeywords = new string[] { "snow", "ice", "terrain", "winter" }
            },
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.DirtyGround,
                nameOrTagKeywords = new string[] { "dirt", "ground", "mud", "soil", "path" }
            },
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.Gravel,
                nameOrTagKeywords = new string[] { "gravel", "pebble", "stone", "rock" }
            },
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.Grass,
                nameOrTagKeywords = new string[] { "grass", "field", "meadow" }
            },
            new SurfaceSoundGroup
            {
                surfaceType = SurfaceType.Water,
                nameOrTagKeywords = new string[] { "water", "river", "lake", "puddle" }
            }
        };
    }
}
