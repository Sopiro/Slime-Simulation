using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Vector3Int numThreads = new Vector3Int(8, 8, 1);
    public ComputeShader testShader;
    private RenderTexture target;

    private Dictionary<string, int> kernelIndices;

    [SerializeField]
    private int width, height;
    private int depth = 24;

    void Start()
    {
        initRenderTexture();

        kernelIndices = new Dictionary<string, int>();
        kernelIndices.Add("TestRandom", testShader.FindKernel("TestRandom"));
    }

    void Update()
    {
    }

    private void initRenderTexture()
    {
        target = new RenderTexture(width, height, depth);
        target.enableRandomWrite = true;
        target.filterMode = FilterMode.Point;
        target.Create();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (target == null) initRenderTexture();

        testShader.SetTexture(kernelIndices["TestRandom"], "res", target);
        testShader.SetFloat("width", target.width);
        testShader.SetFloat("height", target.height);

        testShader.Dispatch(0, width / numThreads.x, height / numThreads.y, numThreads.z);

        Graphics.Blit(target, destination);
    }
}
