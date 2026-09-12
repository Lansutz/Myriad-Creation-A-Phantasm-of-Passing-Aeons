using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
 /// 宗教组织节点（双维度谱系：组织父 × 学派父）
 /// 组织父（parentReligionId）：组织谱系树——宗教→宗派（组织性分裂·互斥）
 /// →传统（思想性传承·可共存）→分支传统（传播/再分化——任意深度）
 /// 学派父（schoolParentId）：思想源链——礼仪祖先学派（如亚述教会←
 /// 赛琉基亚-泰西封学派/狄奥多若——与聂斯托里无关）
 /// 类型（nodeType）：教统=组织性（有主教链/法脉）；传统=思想性（可创建）；学派=前组织形态
 /// （未稳定——无固定仪轨/座堂圣座——可演化为礼/传统/宗派）
 /// 教统（hasSuccession）：组织连续性（使徒统绪/法脉）——宗派必须有；
 /// 无教统学派（如聂斯托里）只能思想并入，不能独立组织化
    [System.Serializable]
    public class ReligionDef
    {
        public int religionId;
 /// <summary>内置名（本地化表有 &lt;id&gt;_name 键时优先）</summary>
        public string religionName;
 /// <summary>组织父（-1=宗教根；&gt;=0=宗派/传统/分支——组织谱系树）</summary>
        public int parentReligionId = -1;
 /// <summary>学派父（-1=无思想源；&gt;=0=祖先学派——思想谱系独立于组织谱系）</summary>
        public int schoolParentId = -1;
 /// <summary>节点类型（学派=前组织形态——可演化）</summary>
        public ReligionNodeType nodeType = ReligionNodeType.Succession;
 /// <summary>教统（使徒统绪/法脉——组织连续性；宗派必须有；无教统学派不能独立组织化）</summary>
        public bool hasSuccession = true;

 /// <summary>宇宙观形态（一神/多神/二元/泛灵/祭祀型/mana）</summary>
        public string worldview = "";
 /// <summary>创教者（琐罗亚斯德/摩尼/佛陀/耶稣/穆罕默德——宗教创生事件）</summary>
        public string founder = "";
 /// <summary>正统/异端标记（教派斗争——正统清洗异端）</summary>
        public bool orthodoxy = true;
 /// <summary>美德特质（宗教对性格的判定——引用 PersonalityTraitDatabase 基 id：
 /// 持美德者虔诚+同信仰好感+可成圣人候选）</summary>
        public List<string> virtues = new List<string>();
 /// <summary>罪行特质（宗教对性格的判定——持罪行者虔诚-+罪行标记：
 /// 同一性格在不同宗教判定不同——lustful 天主教=罪行/肉欲高扬信仰=美德）</summary>
        public List<string> sins = new List<string>();
 /// <summary>教统领袖（中性：教宗/牧首/伊玛目/谢赫/祖师）</summary>
        public string headName = "";
 /// <summary>礼仪领袖（可空——米兰主教=安布罗修礼；教宗=拉丁礼）</summary>
        public string riteHeadName = "";
 /// <summary>共融归属（罗马共融/东正共融/乌玛/苏菲道统群）</summary>
        public string communionName = "";
 /// <summary>主流传统标记（传播基准——大众实践；领袖传统=正统基准——
 /// 无领袖传统的宗教用共识[逊尼=乌里玛共识/多神=无中央标准]）</summary>
        public bool isMainstreamTradition = false;
 /// 仪式语言（口头仪式用语——礼拜/弥撒/祭祀进行时说的话——
 /// 与日常语言分离——神圣性载体）：
 /// 拉丁语[天主教弥撒]/教会斯拉夫语[东正教]/科普特语[科普特礼拜]/
 /// 古典叙利亚语[叙利亚·亚述礼拜]/古典阿拉伯语[伊斯兰礼拜——必用]/
 /// 文言[儒教祭祀祝辞]；原始崇拜=无（口语）
        public string liturgicalLanguage = "";
 /// 经典语言（圣典/经文写作语言——文本神圣性——经典不可译性）：
 /// 希伯来语·希腊语[圣经原文]/拉丁语[武加大]/古典阿拉伯语[古兰经——
 /// 不可译——礼拜必用原文]/巴利语[上座部三藏]/梵语[大乘经]/
 /// 阿维斯陀语[祆教经]/文言[五经四书]——经典语言是神学的语言门槛
 /// （改宗须习经典语言——识字阶层垄断神学解释）
        public string scripturalLanguage = "";
 /// <summary>支柱选择（该节点从 DoctrinePool 选的选项 id——教统的支柱
 /// 以领袖传统为准；选项差异=偏离度来源）</summary>
        public List<string> selectedDoctrines = new List<string>();
 /// 支撑革新（宗教演化×革新对接——革新串联所有系统）：
 /// 神谱编纂←文字｜教义系统化/经书←文字+哲学｜寺庙圣殿←营建｜
 /// 国家祭祀[郊祀]←官僚｜教统组织化←行政+文书｜经文传播←印刷术
 /// 空=基础可用（era0——原始崇拜无前置）
        public List<int> requiredInnovations = new List<int>();
 /// 具体礼名列表（狭义——如"科普特礼"/"埃塞俄比亚礼"：
 /// 同一礼仪传统下可有多个具体礼——埃塞俄比亚礼属亚历山大传统）
        public List<string> rites = new List<string>();
 /// 礼仪传统（广义——五大礼仪传统之一：罗马传统/拜占庭传统/
 /// 亚历山大传统/安条克传统/东叙利亚传统；亚美尼亚=独立传统）
 /// ——具体礼名（rites）从属于礼仪传统（riteFamily）
        public string riteFamily = "";
 /// <summary>教义学派（传统第三级·次级——如"经院学派"/"中观学派"；学派可升格为礼）</summary>
        public string school = "";
 /// <summary>地图基色（RGBA 0-255；未配置时按 id 自动分配）</summary>
        public Color color = Color.white;

        public bool IsRoot => parentReligionId < 0;

 /// <summary>主礼拜仪轨（rites 第一项——传统级显示用；无礼返回空）</summary>
        public string PrimaryRite => rites != null && rites.Count > 0 ? rites[0] : "";
    }

 /// <summary>宗教数据表（ContentRegistry 加载 Religions.json——数据驱动）</summary>

 /// <summary>宗教地图级别（三级谱系显示）</summary>

 /// <summary>宗教节点类型（组织性质——非树级别）</summary>
}
