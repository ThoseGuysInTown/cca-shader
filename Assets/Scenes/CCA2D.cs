using UnityEngine;
using EasyButtons;

public class CCA2D : MonoBehaviour
{
    

    const int MAX_RANGE = 10;
    [Header("CCA Primary Params")]
    [Range(1, MAX_RANGE)]
    public int range = 1;

    const int MAX_THRESHOLD = 25;
    [Range(0, MAX_THRESHOLD)]
    public int threshold = 3;

    const int MAX_STATES = 20;
    [Range(0, MAX_STATES)]
    public int nstates = 3;

    public bool moore;


    ///// tw


    [Header("Setup")]
    [Range(8, 2048)]
    public int rez = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 0;

    [Range(1, 50)]
    public int stepMod = 1;

    [Range(0.0f, 1f)]
    public float hueMax = 1;

    [Range(0.0f, 1f)]
    public float hueMin = 0f;

    public Material outMat;
    public ComputeShader cs;

    private RenderTexture readTex;
    private RenderTexture writeTex;
    private RenderTexture outTex;

    private int stepKernel;

    void Update()
    {
        if (Time.frameCount % stepMod == 0)
        {
            for (int i = 0; i < stepsPerFrame; i++)
            {
                Step();
            }
        }

    }

    /*
     * 
     * 
     *  RESET 
     * 
     * 
     */
    void Start()
    {
        Reset();
    }

    //https://github.com/madsbangh/EasyButtons
    [Button]
    private void Reset()
    {
        readTex = CreateTexture(RenderTextureFormat.RFloat);
        writeTex = CreateTexture(RenderTextureFormat.RFloat);
        outTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        stepKernel = cs.FindKernel("StepKernel");

        GPUResetKernel();
    }


    [Button]
    public void RandomizeParams()
    {
        var rand = new System.Random();
        range = (int)(rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        threshold = (int)(rand.NextDouble() * MAX_THRESHOLD - 1) + 1;
        nstates = (int)(rand.NextDouble() * (MAX_STATES - 2)) + 2;
        moore = rand.NextDouble() <= 0.5;

        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);
    }

    [Button]
    public void ResetAndRandomize()
    {
        RandomizeParams();
        RandomizeColors();
        Reset();
    }

    [Button]
    public void RandomizeColors()
    {
        var rand = new System.Random(Time.frameCount);

        // Step 1: Create a gradient
        Gradient g = new Gradient();
        int keycount = 8;
        GradientColorKey[] c = new GradientColorKey[keycount];
        GradientAlphaKey[] a = new GradientAlphaKey[keycount];
        // 2/1/19/n
        float sMax = 2, sMin = 0, vMax = 1, vMin = 0;
        for (int i = 0; i < keycount; i++)
        {
            float h = (float)rand.NextDouble() * (hueMax - hueMin) + hueMin;
            float s = (float)rand.NextDouble() * (sMax - sMin) + sMin;
            float v = (float)rand.NextDouble() * (vMax - vMin) + vMin;
            Color nc = Color.HSVToRGB(h, s, v);
            c[i].color = nc;
            a[i].time = c[i].time = (i * (1.0f / keycount));
            a[i].alpha = 1.0f;
        }
        g.SetKeys(c, a);


        // Step 2: Sample colors from the gradient
        // this gives us more "related" colors than just selecting random colors
        Vector4[] colors = new Vector4[nstates];
        for (int i = 0; i < nstates; i++)
        {
            float t = (float)rand.NextDouble();
            colors[i] = g.Evaluate(t);
        }
        cs.SetVectorArray("colors", colors);
    }


    protected RenderTexture CreateTexture(RenderTextureFormat format)
    {
        RenderTexture texture = new RenderTexture(rez, rez, 1, format);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.useMipMap = false;
        texture.Create();
        return texture;
    }

    private void GPUResetKernel()
    {
        int k = cs.FindKernel("ResetKernel");
        cs.SetTexture(k, "writeTex", writeTex);

       cs.SetInt("range", range);
       cs.SetInt("threshold", threshold);
       cs.SetInt("nstates", nstates);
       cs.SetBool("moore", moore);

        cs.SetInt("rez", rez);
        cs.Dispatch(k, rez, rez, 1);
        SwapTex();
    }

    /*
     * 
     * 
     *  STEP 
     * 
     * 
     */
    [Button]
    public void Step()
    {
        cs.SetTexture(stepKernel, "readTex", readTex);
        cs.SetTexture(stepKernel, "writeTex", writeTex);
        cs.SetTexture(stepKernel, "outTex", outTex);

        cs.Dispatch(stepKernel, rez, rez, 1);

        SwapTex();

        outMat.SetTexture("_UnlitColorMap", outTex);
    }


    private void SwapTex()
    {
        RenderTexture tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }

        //https://github.com/madsbangh/EasyButtons
    [Button]
    private void Test()
    {
        Reset();
        Step();
    }

}
