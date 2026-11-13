// GameWorld 主声明文件（B3-part1 / task-70）
// 实现按职责拆分到同目录 GameWorld.*.cs（partial）；本文件仅保留类声明，供 Unity 脚本资产/文件名匹配。
// [SerializeField] 字段与字段名仍在 GameWorld.Core.cs（partial 编译为同一类型，序列化/场景不变）。
using UnityEngine;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour, IWorldReadModel
{
}
}
