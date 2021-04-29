using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : MonoBehaviour
{
    public ComputeShader shader;

    [SerializeField]
    private int width = 1920, height = 1080;
    private int depth = 1;
    [SerializeField]
    private int numAgents = 1000000;
    [SerializeField]
    private float moveSpeed = 50.0f;
    [SerializeField]
    private float diffuseSpeed = 10.0f;
    [SerializeField]
    private float evaporateSpeed = 0.3f;

    [SerializeField]
    private int senseRange = 3;
    [SerializeField]
    private float sensorLength = 8.0f;
    [SerializeField]
    private float sensorAngleSpacing = 30.0f;
    [SerializeField]
    private float turnSpeed = 50.0f;
    [SerializeField]
    private float marchingError = 0.1f;

    private RenderTexture trailMap;
    private RenderTexture trailMapProcessed;
    private ComputeBuffer agentsBuffer;

    private Dictionary<string, int> kernelIndices;

    public struct Agent
    {
        public Vector2 position;
        public float angle;
        public Vector4 type;
    } // size = 7 * 4 bytes

    private Agent[] agents;

    void Start()
    {
        kernelIndices = new Dictionary<string, int>();
        kernelIndices.Add("Update", shader.FindKernel("Update")); // Thread Shape [16, 1, 1]
        kernelIndices.Add("Postprocess", shader.FindKernel("Postprocess")); // Thread Shape [8, 8, 1]

        createNewTexture(ref trailMap);

        agents = new Agent[numAgents];
        for (int i = 0; i < agents.Length; i++)
        {
            float angle = Random.Range(0, 2 * Mathf.PI);
            float len = Random.value * height * 0.9f / 2.0f;
            float x = Mathf.Cos(angle) * len;
            float y = Mathf.Sin(angle) * len;

            agents[i].position = new Vector2(width / 2 + x, height / 2 + y);
            agents[i].angle = angle + Mathf.PI;

            Vector4 type = Vector4.zero;
            type[Random.Range(0, 3)] = 1;
            agents[i].type = type;
            
        }

        agentsBuffer = new ComputeBuffer(agents.Length, sizeof(float) * 7);
        agentsBuffer.SetData(agents);
    }

    void FixedUpdate()
    {
        shader.SetTexture(kernelIndices["Update"], "TrailMap", trailMap);

        shader.SetInt("width", width);
        shader.SetInt("height", height);
        shader.SetInt("numAgents", numAgents);
        shader.SetFloat("moveSpeed", moveSpeed);
        shader.SetFloat("deltaTime", Time.fixedDeltaTime);

        shader.SetInt("senseRange", senseRange);
        shader.SetFloat("sensorLength", sensorLength);
        shader.SetFloat("sensorAngleSpacing", sensorAngleSpacing * Mathf.Deg2Rad);
        shader.SetFloat("turnSpeed", turnSpeed);
        shader.SetFloat("marchingError", marchingError);
        shader.SetBuffer(kernelIndices["Update"], "agents", agentsBuffer);
        shader.Dispatch(kernelIndices["Update"], numAgents / 16, 1, 1);

        createNewTexture(ref trailMapProcessed);

        shader.SetFloat("evaporateSpeed", evaporateSpeed);
        shader.SetFloat("diffuseSpeed", diffuseSpeed);
        shader.SetTexture(kernelIndices["Postprocess"], "TrailMap", trailMap);
        shader.SetTexture(kernelIndices["Postprocess"], "TrailMapProcessed", trailMapProcessed);

        shader.Dispatch(kernelIndices["Postprocess"], width / 8, height / 8, 1);

        trailMap.Release();
        trailMap = trailMapProcessed;
    }

    private void createNewTexture(ref RenderTexture renderTexture)
    {
        renderTexture = new RenderTexture(width, height, depth);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(trailMapProcessed, destination);
    }

    private void OnDestroy()
    {
        trailMap.Release();
        trailMapProcessed.Release();
        agentsBuffer.Release();
    }
}
