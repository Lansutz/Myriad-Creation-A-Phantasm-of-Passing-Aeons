namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 宏观身体部位枚举
    /// 每个部位下有若干子结构（SubStructureType），子结构的定量数据就是病因数据
    /// </summary>
    public enum BodyPartType
    {
        // ===== 头部 =====
        LeftEye,            // 左眼
        RightEye,           // 右眼
        LeftEar,            // 左耳
        RightEar,           // 右耳
        Brain,              // 大脑
        Teeth,              // 牙齿
        Hair,               // 头发（外观为主，无HP）
        Face,               // 面部（外观+损伤）
        Skull,              // 颅骨

        // ===== 躯干 =====
        Heart,              // 心脏
        LeftLung,           // 左肺
        RightLung,          // 右肺
        Abdomen,            // 腹部内脏（消化/代谢/肝肾合并）
        Spine,              // 脊柱
        Skin,               // 皮肤（全身，不细分部位）

        // ===== 左臂 =====
        LeftShoulder,       // 左肩
        LeftArmBone,        // 左臂骨骼
        LeftArmMuscle,      // 左臂肌肉
        LeftHand,           // 左手

        // ===== 右臂 =====
        RightShoulder,      // 右肩
        RightArmBone,       // 右臂骨骼
        RightArmMuscle,     // 右臂肌肉
        RightHand,          // 右手

        // ===== 左腿 =====
        LeftHip,            // 左髋
        LeftKnee,           // 左膝
        LeftLegBone,        // 左腿骨骼
        LeftLegMuscle,      // 左腿肌肉
        LeftFoot,           // 左脚

        // ===== 右腿 =====
        RightHip,           // 右髋
        RightKnee,          // 右膝
        RightLegBone,       // 右腿骨骼
        RightLegMuscle,     // 右腿肌肉
        RightFoot,          // 右脚
    }
}
