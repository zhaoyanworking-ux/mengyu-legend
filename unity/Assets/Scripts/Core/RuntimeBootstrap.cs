using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 若场景里还没有 GameManager（例如刚打开空场景就按下 Play），自动搭建演示世界。
    /// </summary>
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindObjectOfType<GameManager>() != null)
                return;
            DemoWorldBuilder.Build();
        }
    }
}
