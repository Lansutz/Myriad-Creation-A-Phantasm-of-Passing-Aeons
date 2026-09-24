namespace MyriadCreation.Simulation.Characters
{
    /// <summary>
    /// 子结构类型枚举——身体部位下的具体病变位置
    /// 子结构的定量数据（condition 0-100）就是病因数据，后台模拟不直接显示
    /// 病名（白内障/青光眼等）是子结构病变达到阈值后的诊断结果
    /// </summary>
    public enum SubStructureType
    {
        // ===== 眼部（左眼/右眼共用）=====
        EyeLens,            // 晶状体 → 混浊度 → 白内障
        EyeAqueous,         // 房水系统 → 眼压 → 青光眼
        EyeRetina,          // 视网膜 → 损伤度 → 视网膜病变/脱离
        EyeCornea,          // 角膜 → 混浊/损伤 → 角膜白斑/角膜炎

        // ===== 耳部（左耳/右耳共用）=====
        EarDrum,            // 外耳道/鼓膜 → 炎症/损伤 → 中耳炎/鼓膜穿孔
        EarInner,           // 听神经/内耳 → 退化度 → 神经性耳聋/耳鸣

        // ===== 大脑 =====
        BrainCognition,     // 认知功能 → 衰退度 → 痴呆/失能
        BrainVessel,        // 脑血管 → 堵塞/出血度 → 中风
        BrainMental,        // 精神状态 → 异常度 → 抑郁/焦虑/偏执

        // ===== 牙齿 =====
        ToothBody,          // 牙体 → 蛀蚀度 → 龋齿
        ToothGum,           // 牙龈 → 炎症度 → 牙周炎

        // ===== 头发 =====
        HairFollicle,       // 毛囊 → 萎缩度 → 秃顶

        // ===== 面部 =====
        FaceSkin,           // 面部皮肤 → 损伤/疤痕度 → 毁容/疤痕

        // ===== 颅骨 =====
        SkullBone,          // 颅骨 → 损伤度 → 颅骨骨折

        // ===== 心脏 =====
        HeartMyocardium,    // 心肌 → 缺血/肥大度 → 冠心病/心梗/心肌肥厚
        HeartValve,         // 瓣膜 → 病变度 → 瓣膜狭窄/关闭不全
        HeartCoronary,      // 冠状动脉 → 堵塞度 → 冠心病

        // ===== 肺部（左肺/右肺共用）=====
        LungTissue,         // 肺泡/气道 → 炎症/堵塞度 → 肺炎/结核/哮喘

        // ===== 腹部内脏 =====
        AbdomenDigestive,   // 消化系统 → 炎症/功能紊乱度 → 痢疾/霍乱/胃病
        AbdomenMetabolic,   // 代谢系统 → 异常度 → 糖尿病/痛风
        AbdomenLiverKidney, // 肝肾 → 功能衰退度 → 肝肾衰竭

        // ===== 脊柱 =====
        SpineVertebra,      // 椎体/椎间盘 → 变形/损伤度 → 驼背/椎间盘突出/脊柱断裂

        // ===== 皮肤 =====
        SkinTissue,         // 表皮/真皮 → 损伤/病变度 → 麻风/烧伤/冻伤/皮炎

        // ===== 关节（肩/髋/膝共用）=====
        JointCartilage,     // 软骨 → 磨损度 → 骨关节炎
        JointSynovium,      // 滑膜 → 炎症度 → 类风湿关节炎
        JointLigament,      // 韧带 → 损伤度 → 韧带撕裂/脱臼

        // ===== 骨骼（手臂/腿共用）=====
        BoneTissue,         // 骨组织 → 损伤度 → 骨折

        // ===== 肌肉（手臂/腿共用）=====
        MuscleFiber,        // 肌纤维 → 损伤/萎缩度 → 肌肉拉伤/肌萎缩

        // ===== 手/脚 =====
        Extremity,          // 整体 → 损伤度 → 手伤/脚伤/跛足
    }
}
