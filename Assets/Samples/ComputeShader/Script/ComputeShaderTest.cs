using UnityEngine;
using UnityEngine.UI;

public class ComputeShaderTest : MonoBehaviour
{
    [SerializeField]
    private RawImage m_image;
    [SerializeField]
    private ComputeShader shader;

    int width = 256 * 2;
    int height = 256 * 2;

    // Start is called before the first frame update
    void Start()
    {
        RunShader();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void RunShader()
    {
        int kernelHandle = shader.FindKernel("CSMain");

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        shader.SetTexture(kernelHandle, "Result", renderTexture);
        /*
         前面两个整型决定了我需要多少线程组，为了满足每个线程都只有一个像素对应，我们给定这两个整型为（基于256*256的贴图）
        线程组x单位 = Texture Width/Single Thread With =32
        线程组y单位 = TextureHeihgt/Single ThreadHeight =32
        得到一共需要的线程组：32*32
        Dispatch后三个参数为 横向的ThreadGroup数量, 纵向的ThreadGroup数量, 深度方向的ThreadGroup数量
         */
        shader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);

        m_image.texture = renderTexture;

        //Destroy(renderTexture);

    }

}
