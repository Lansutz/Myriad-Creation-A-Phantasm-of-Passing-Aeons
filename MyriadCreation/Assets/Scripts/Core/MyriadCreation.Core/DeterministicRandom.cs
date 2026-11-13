using System;

namespace MyriadCreation.Core
{
    /// <summary>
    /// 确定性掷骰原语（L0：`MyriadCreation.Core` 无引用，任何层都能用）。
    ///
    /// 为什么放这里：原来"随机"分散在 `GameWorld.ThreadRng`（Environment.TickCount 播种）与
    /// 若干 `new Random()`（时间种子）里，导致同 seed 两次运行结果不同。要给**所有系统**
    /// 提供可复现随机，原语必须住在最底层，而不是住在 Simulation.WorldState（那会让 Culture/Faith/Laws
    /// 为了掷一次骰去引用世界状态程序集）。
    ///
    /// 用法：由 (世界 seed, 当日, 实体 id a, 实体 id b, salt) 派生——**纯函数、无共享状态**，
    /// 可在 Parallel.For 内安全调用，同输入必得同结果。salt 用来区分同一天同一实体的不同判定点。
    /// </summary>
    public static class DeterministicRandom
    {
        /// <summary>32 位混合哈希（splitmix 风格），无分配、无状态。</summary>
        public static uint Hash(int seed, int day, int a, int b, int salt)
        {
            uint h = (uint)seed;
            h ^= (uint)day * 0x9E3779B1u;
            h ^= (uint)a * 0x85EBCA77u;
            h ^= (uint)b * 0xC2B2AE3Du;
            h ^= (uint)salt * 0x27D4EB2Fu;
            h ^= h >> 15; h *= 0x2545F491u;
            h ^= h >> 13; h *= 0x9E3779B1u;
            h ^= h >> 16;
            return h;
        }

        /// <summary>[0,1) 均匀值，精度 1e-4（足够判定概率事件，且跨平台一致）。</summary>
        public static float Value01(int seed, int day, int a, int b, int salt)
        {
            return (Hash(seed, day, a, b, salt) % 10000u) * 0.0001f;
        }

        /// <summary>以 chance 概率返回 true。</summary>
        public static bool Roll01(int seed, int day, int a, int b, int salt, float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return Value01(seed, day, a, b, salt) < chance;
        }

        /// <summary>派生一个可复现的 System.Random（给仍需要序列随机的旧接口用）。</summary>
        public static Random Create(int seed, int day, int salt)
        {
            return new Random((int)(Hash(seed, day, salt, 0, 0) & 0x7FFFFFFF));
        }
    }
}
