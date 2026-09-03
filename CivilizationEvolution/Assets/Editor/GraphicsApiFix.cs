using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 一次性工具：Windows 图形 API 强制 Direct3D11
    /// （修复 D3D12 GPU device error 崩溃——887a0006——
    /// 6000.6 + RTX 4060 D3D12 不稳定——DX11 稳）
    /// 用法：Unity -batchmode -executeMethod CivilizationEvolution.EditorTools.GraphicsApiFix.ForceD3D11
    /// </summary>
    public static class GraphicsApiFix
    {
        public static void ForceD3D11()
        {
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows,
                new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
            Debug.Log("[GraphicsApiFix] Windows 图形 API 已设为 Direct3D11（D3D12 崩溃修复）");
        }
    }
}
