# 世界模拟资源索引（核查版）

> 核查日期：2026-10-27。本文把用户提供的《面向整个模型推演的资源清单》按模块整理、核对、并映射到本项目（Myriad Creation / 纷繁的世界）的模拟系统与内容管线。它不是数据库使用教程，也不是学术综述；它的用途是：**让后续每一次"用一个数据库"的决定，都附带版本、许可、覆盖与失败模式**。

## 1. 核查方法与限制（先读这一节）

- 核查手段：官方站点页面直接抓取（`Invoke-WebRequest`）+ 公开检索交叉验证。`web_fetch` 工具在本会话被本地网络策略拦截，故一律以站点正文/仓库 README/Zenodo 记录为准。
- 访问日期：2026-10-27。
- **限制**：部分站点拒抓（403/502/SSL）或页面由 JS 渲染，相关条目只能标 `UNVERIFIED`。这不等于清单错误，只表示"本次没有拿到一手证据"，接入前必须补核。
- 状态图例：
  - `✅` 官方页面直接确认（给 URL）。
  - `⚠️` 部分确认或本次未抓到一手页（数字/许可需复核）。
  - `❌` 与官方口径不符（给正确值）。
- **许可必须区分三件事**：能否读（access）、能否再分发（redistribute）、能否商用（commercial）。本文把「可否随游戏包分发」单列一栏，因为这是本项目真正的分水岭。

## 2. 覆盖与口径总则

1. **不插值**：任何语言/社会/地块，只允许四种状态——已编码、仅有词表、仅有目录、无数据。禁止用平均值填补空值（清单第 1 条缺口的工程化表述）。
2. **版本锁死**：所有引用必须带版本（Glottolog 5.3、Grambank release、CLICS⁴、Lexibank 2、D-PLACE v3.3.0、Seshat Equinox-2020……）。数据库每年更新，数字会漂移。
3. **主键分层**：语言用 Glottocode；社会/文化用 D-PLACE 的 society ID（内部再映射到 Glottocode）；政体用 Seshat polity ID；考古用站点/项目 ID。跨库 join 一律走显式映射表，不假设"同名即同一实体"。
4. **覆盖声明随数据走**：每个进入 `StreamingAssets` 的生成文件，都必须在 `DATASETS.lock.json` 记录来源、版本、许可、裁剪规则、生成日期。

## 3. 语言结构层

| 数据库 | 清单取用部分 | 核查结果 | 许可 / 可分发 | 本项目落点 | 已知失败模式 |
|---|---|---|---|---|---|
| CLDF | 数据集总目录，跨库统一读取格式 | `✅` CLDF 1.x 规范站存在（cldf.clld.org），是格式不是数据 | 规范本身开源；具体数据集各自许可 | 仅作"读取约定"，不直接进游戏 | 同一语言在不同数据集里是不同 CLDF 行，仍需 Glottocode join |
| Glottolog | languoid 目录、Newick 谱系、AES 濒危、Glottocode | `✅` 当前 **5.3**；分类页显示 **8,238 languoids**；Zenodo 数据集记录许可 **CC BY 4.0** | ✅ 可再分发（署名） | 语言身份主键；文化/族群谱系树的参照 | 8,238 是当前版本总数，含方言/语支；"语言数"口径需另取 |
| Grambank | 195 形态句法特征、约 2,467 语言 | `✅` 官网：**2,467 varieties / 195 features / 441,663 datapoints**（其中 362,025 为排除 "not known"）；**CC BY 4.0** | ✅ 可再分发（署名） | 构念语言的类型学签名校验（设计期） | 覆盖偏形态句法；词序等特征编码有缺失值 |
| WALS | 192 特征、2,676 语言、值矩阵约 17% 完整 | `✅` 站点存在、许可 **CC BY 4.0**；`⚠️` 具体语言数/完整度未能从官方页逐项复核（WALS Online 2013 数据） | ✅ 可再分发（署名） | 类型学辅助校验，不作主依据 | 覆盖率低、特征定义与 Grambank 不一一对应 |
| AUTOTYP | 260+ 变量、1,319 语言、约 26 万数据点 | `⚠️` autotyp.clld.org 本次 502；数字 `UNVERIFIED` | 待核（CLDF 生态通常 CC BY(-SA)） | 论元结构/语义角色/领属分类的精细参数（若接入） | 版本与许可必须先补核 |
| PHOIBLE | 2,186 语言、3,020 库存、3,183 音段类型 | `✅` 官网 2019 版：**3,020 inventories / 3,183 segment types / 2,186 languages**，含区别特征；许可 **CC BY-SA 3.0** | ⚠️ 可分发但有 **ShareAlike** 传染性，需隔离数据层 | 音系"类型学落点"校验：生成音系必须能在 PHOIBLE 找到 attested 对应，否则标 `TYPOLOGICALLY_UNATTESTED`（设计期） | 同一语言多来源库存互相冲突；特征系统是项目自建，非音系学定论 |
| Lexibank | 134 数据集、3,107 语言、173 万词形 | `✅` 官网：**134 datasets / 5,477 languages / 3,205 concepts / 1,734,794 words**；**CC BY 4.0**。清单的 3,107 语言 `❌`（旧口径） | ✅ 可再分发（署名） | 同源关系/词表参考（设计期）；可为自建词表提供词形与语义 gloss 样本 | 各子数据集质量与标准化程度不一；同源判断是专家/算法混合 |
| ASJP | 40 词核心词表，5,000+ 变体 | `✅` 当前 **version 21**、40 词表；**CC BY 4.0**；`⚠️` 具体变体数未复核 | ✅ 可再分发（署名） | "最小词汇存在证明"与粗相似度参照（设计期） | 只有 40 词，语义覆盖极窄；转写为 ASJP 码，非 IPA |
| CLICS | 3,100+ 语言共词化网络 | `✅` 当前 **CLICS⁴**：**3,447 language varieties**（2026 引用）；**CC BY 4.0**。清单 3,100+ `❌`（CLICS³ 时代口径） | ✅ 可再分发（署名） | 语义演变/多义模式校验；命名与词根语义关联的参照 | 共词化 ≠ 词源同源；概念列表有限（CLICS 概念集） |
| SAILS | 南美原住民语法结构，CLDF StructureDataset，含 construction-based 子集 | `✅` 站点存在；SUB 数据集确为 construction-based；许可 **CC BY-NC-ND 2.0** | ❌ **NC + NoDerivs**：不可商用、不可改写分发 → 只能设计期参考 | 区域句法/形态参照（设计期）；"构式级"记录法的思路借鉴 | 覆盖仅南美；ND 禁止改写，连翻译整理都受限 |
| MAGRAM / CrossGram | 992 条语法化路径、8 参数；l-/c-parameters | CrossGram `✅` 站点存在、**CC BY 4.0**；MAGRAM `⚠️` 本次 502，**992 条路径 `UNVERIFIED`** | CrossGram 可分发；MAGRAM 待核 | 语法化路径的可比较参数（设计期） | 路径是专家归纳，不是可执行规则；语言覆盖差异大 |
| Paralex / UniMorph | 屈折范式 CSV；UniMorph 169+ 语言、约 1.22 亿形式 | UniMorph `✅` 页面明示 **169 annotated languages**，每语言单独许可列；`⚠️` 1.22 亿形式未复核。Paralex `⚠️` 未抓取 | UniMorph 逐语言不同（含 CC BY-SA 类）；Paralex 待核 | 屈折范式熵/自然类分析（设计期） | UniMorph 是 NLP 标注，不等于语言学范式；语言覆盖偏欧洲 |
| IE-CoR + BEAST2 | 4,981 组专家同源集、170 义项、89 位语言学家；贝叶斯谱系断代 | IE-CoR `✅` 站点存在（Science 论文）；`⚠️` 4,981/170/89 未复核。BEAST2 `✅` 软件存在；清单"2026 年校准研究" `UNVERIFIED` | IE-CoR 待核；BEAST2 开源 | 谱系断代方法参照（设计期） | 纯树模型对接触密集语系过度简化；校准结论必须报模型敏感性，**不要把未核实的研究写成既定事实** |

## 4. 文化—制度层

| 数据库 | 清单取用部分 | 核查结果 | 许可 / 可分发 | 本项目落点 | 已知失败模式 |
|---|---|---|---|---|---|
| D-PLACE | SCCS、Ethnographic Atlas 等聚合；清单称 v3.3（2026） | `✅` 核对确认：Zenodo 聚合数据集为 **v3.3.0（2026-01-13）**，许可 **CC BY-NC 4.0**（此前文档误记为 v3.0，已修正） | ❌ **NC**：不可商用分发；只能设计期参照 | 制度空间参照：生计/社会结构/婚姻/居住/政治整合的变异范围（设计期） | 社会样本非随机；编码有年代与来源差异；NC 不能进游戏包 |
| eHRAF World Cultures | 全文民族志、OCM 段落索引、400+ 文化 | `✅` 站点存在，页面显示 **limited access（订阅制）**；`⚠️` 规模数字未复核 | ❌ 订阅内容，不可分发 | 制度细节的案例检索（设计期，人工查阅） | 访问门槛；抽样偏差（民族志传统强的区域过代表） |
| Seshat | 约 400 政体、30 区域、近 200 变量；Equinox 版 | `✅` GitHub 明确"data is available but **not openly licensed (NC restriction)**"，许可 **CC BY-NC-SA**；Equinox-2020；`⚠️` ~400 政体未复核 | ❌ **NC-SA**：不可商用分发 | 社会复杂性阈值校准：酋邦—早期国家—帝国的判断（设计期） | 变量定义依赖编码手册；政体样本偏旧世界；NC-SA 双限制 |
| Ethnographic Atlas（经 D-PLACE） | 1,000+ 社会的生计/定居/继承/政治层级 | `⚠️` 经 D-PLACE 提供，继承其 **NC** 约束；社会数未复核 | ❌ NC | 与 D-PLACE 互补的社会样本（设计期） | 同 D-PLACE；父子社会编码粒度不一 |
| 口头传统目录（Berezkin / FFDB / Ashliman） | 958 群体母题目录；狩猎采集口述语料；公开标注文本 | `⚠️` 本次未逐项抓取，全部 `UNVERIFIED` | 不明（部分为学术项目页面，须逐库确认） | 神话母题/叙事结构参照（设计期） | 母题分类框架不统一；翻译层丢失结构 |
| Global Jukebox / Smithsonian 等 | 1,026 民族、5,776 条录音；数万条传统音乐录音 | `⚠️` globaljukebox.org 本次 SSL 抓取失败，数字 `UNVERIFIED`；Smithsonian 类为许可/订阅内容 | 不明/受限 | 仪式声音、乐器分布参照（设计期） | 音频版权复杂；民族级编码与 D-PLACE society ID 需人工对齐 |

## 5. 环境—物产层

| 数据库 | 清单取用部分 | 核查结果 | 许可 / 可分发 | 本项目落点 | 已知失败模式 |
|---|---|---|---|---|---|
| Köppen-Geiger / Holdridge | 1 km 现代气候分类；38 生命地带 | Köppen `⚠️`（Beck et al. 2018 通常 CC BY 4.0，本次未抓到一手页）；Holdridge 38 类 `⚠️` | 待核（多为 CC BY） | 气候带—植被约束（构建期生成 world 参数） | **现代基线**用于全新世早期有系统偏差；见第 7 节 |
| WWF TEOW / RESOLVE 2017 | 清单写"846 个陆地生态区、14 生物群系" | `✅` ecoregions.appspot.com 是 **Ecoregions 2017（RESOLVE）**；`⚠️` **846 属 RESOLVE 口径**，WWF TEOW 是另一套（通常记 867 个陆地生态区）→ 清单把两个数据集混写 `❌` | RESOLVE 2017 许可待核（站点未给明确条款）；WWF TEOW 有使用条款 | 生态区→群系映射（构建期生成 biome 属性表） | 生态区边界不等于游戏地块；两套分类不可混用 |
| Neotoma | 花粉/大化石/木炭/年代学；古环境 DNA 扩展 | `✅` 站点存在：**31,259 sites / 14,057,918 datapoints / 46,312 datasets**；许可/禁运条款未抓到一手页 `⚠️` | 待核（须读 Data Use & Embargo Policy） | 全新世植被重建，校正"现代生态区"错位（构建期校准） | 代理点空间分布不均；年代模型不确定 |
| PMIP | PMIP3/PMIP4 中全新世与工业前实验 | `✅` 站点存在，**当前已到 PMIP7**（清单 PMIP3/4 已过时 `❌`）；mid-Holocene/LGM 实验仍在；数据经 ESGF/PMIP-DATA | 各模式组许可不同，多数可引用使用 | 过去气候场，与 Neotoma 互校（设计期/构建期） | 模式间离散度大；降尺度到地块需自建方法并声明 |
| ArchaeoGLOBE | 146 区域单元、10 时间区间、万年土地利用 | `⚠️` 未抓到一手数据页，数字 `UNVERIFIED` | 待核 | 土地利用强度初值校准（设计期） | 时间/空间分辨率粗；区域插值会制造伪精度 |
| Crop Origins / FAO GAEZ / DAD-IS | 866 粮食作物、901 野生祖先；280 作物适宜性；家畜品种分布 | GAEZ `✅` 站点存在（FAO+IIASA，土地评估/作物适宜性）；DAD-IS `✅` 站点存在（品种数据库/国家协调员）；Crop Origins 866/901 `⚠️` 未复核 | GAEZ 条款需逐项确认；DAD-IS 多为检索用途；Crop Origins 待核 | "可驯化—可生产—可储存"链条：作物/产量/家畜参数（构建期校准，输出自建参数表） | 现代品种/现代气候；GAEZ 输入投入假设影响产量；不可直接当古代产量 |

## 6. 考古与古 DNA、时间演化层

| 数据库 | 清单取用部分 | 核查结果 | 许可 / 可分发 | 本项目落点 | 已知失败模式 |
|---|---|---|---|---|---|
| Open Context | 近 240 万条记录、200+ 项目、1,600+ 学者；逐记录 URL | `✅` 站点存在（发布/归档/API）；`⚠️` 具体数字未复核 | 逐记录许可不同（多为 CC BY） | 遗址级物质文化参照（设计期） | 记录粒度差异极大；不可当统一编码表 |
| tDAR | 数千数据集、2 万+ 文档、50 万+ 图像 | `⚠️` 本次 403，全部 `UNVERIFIED` | 访问/许可逐项确认 | 发掘报告与文档检索（设计期） | 多为灰文献，标准化差 |
| AADR | 数千古代/现代个体、最多 123 万位点 | `⚠️` Dataverse 页面由 JS 渲染，本次无内容；数字 `UNVERIFIED` | 须确认再分发与伦理条款（古 DNA 敏感） | 种群迁移/混合作为人口史参数（设计期，只读结论） | 样本偏欧洲/北美；古 DNA 伦理与身份议题敏感 |
| IE-CoR / BEAST2 | 见语言层 | 见语言层 | 见语言层 | 谱系断代（设计期） | 见语言层 |

## 7. 清单需要修正的地方（校核差异汇总）

| # | 清单原文 | 核查结论 |
|---|---|---|
| 1 | Lexibank "3,107 种语言" | ❌ 官方当前为 **5,477 languages**（134 数据集/3,205 概念/1,734,794 词形，Lexibank 2） |
| 2 | CLICS "3,100+ 语言" | ❌ 当前是 **CLICS⁴，3,447 变体**（3,100+ 是 CLICS³ 时代口径） |
| 3 | D-PLACE "v3.3（2026 年）" | ✅ 复核后成立：聚合数据集为 **v3.3.0（2026-01-13，Zenodo）**；许可为 **CC BY-NC 4.0**（清单没写 NC）。此前误记为 v3.0，已修正 |
| 4 | Seshat "部分数据有 NC 许可" | ❌ 实际是整体 **not openly licensed / CC BY-NC-SA**；只可设计期参考 |
| 5 | PHOIBLE 未提许可 | ⚠️ **CC BY-SA 3.0**：有 ShareAlike 传染性，接入必须隔离数据层 |
| 6 | SAILS 未提许可 | ❌ **CC BY-NC-ND 2.0**：NC + 禁止演绎，不可进游戏、不可改写 |
| 7 | "WWF TEOW / RESOLVE 2017 的 846 个生态区" | ❌ 846 是 **RESOLVE 2017** 的口径；WWF TEOW 是另一套（通常记 867 个陆地生态区，**数量待核**），两者不能混写 |
| 8 | "PMIP3/PMIP4" | ❌ 已到 **PMIP7**（清单写的版本过时；mid-Holocene/LGM 数据仍可用） |
| 9 | "Glottolog 8,000+ languoid" | ✅ 成立，但应带版本：**5.3 / 8,238 languoids** |
| 10 | Grambank 数字 | ✅ 2,467 varieties / 195 features；补充：**441,663 datapoints（362,025 排除 not known）** |
| 11 | "2026 年 BEAST2 校准研究显示……" | ⚠️ `UNVERIFIED`：本次未能定位该研究。**不要把它当既定事实写进设计依据**；引用前必须给论文 |
| 12 | eHRAF "400+ 文化" | ✅ 站点存在但为 **limited access（订阅）**；规模未复核 |
| 13 | AUTOTYP 260 变量/1,319 语言/26 万数据点 | ⚠️ 站点 502，`UNVERIFIED` |
| 14 | MAGRAM 992 条路径 | ⚠️ 站点 502，`UNVERIFIED` |
| 15 | UniMorph 169 语言 / 1.22 亿形式 | 169 ✅；1.22 亿 `UNVERIFIED` |
| 16 | AADR 123 万位点 | ⚠️ 页面未抓到，`UNVERIFIED` |
| 17 | Berezkin 958 群体 / Ethnographic Atlas 1,245 群体 | ⚠️ `UNVERIFIED`（且 EA 受 D-PLACE 的 NC 约束） |
| 18 | Global Jukebox 1,026 民族 / 5,776 录音 | ⚠️ 站点抓取失败，`UNVERIFIED` |
| 19 | Open Context 近 240 万条记录 / tDAR 规模 | ⚠️ `UNVERIFIED`（站点存在，规模数字未复核） |

## 8. 结构性缺口（清单四条 + 本项目补充）

原清单四条均成立：① 没有库覆盖"全部语言×全部分支"；② 现代环境数据与早期文明存在年代错位；③ 文化库之间缺统一主键；④ 谱系推断的模型选择本身是假设。

本项目补充四条（工程视角）：

5. **许可分层不是学术问题，是发布问题**：NC/ND/订阅/不明许可的库，只能做设计期参照，不能进 `StreamingAssets`；SA 类需要隔离数据层并写 NOTICE。
6. **体积与分辨率**：846 生态区/1 km 气候栅格/千万级古生态点都不该进游戏包；正确做法是"离线校准 → 生成小参数表"，原始数据不入库。
7. **内容层 ≠ 类型学层**：Grambank/WALS 描述的是"语言的结构参数"，不是"这个词怎么写"。本项目需要的是可用词形、音系与命名素材，这两类数据要靠 Lexibank/ASJP/自建 conlang 规则，而不是类型学库。
8. **口径陷阱**：同一指标在不同库指不同东西（语言数 vs 变体数 vs 数据集数；社会数 vs 文化数 vs 政体数；生态区 846 vs 867）。入库前必须把口径写进字段名或注释，否则后续系统会"正确地算错"。

## 9. 对游戏的映射总表

| 数据库 | 用到哪一部分 | 进入本项目的位置 | 用途类型 |
|---|---|---|---|
| Glottolog | 谱系树 + Glottocode | `CultureData` / `EthnicGroupDef` 新增谱系字段；`LanguageDef` 身份 | 运行时内容（裁剪后可分发） |
| PHOIBLE | 音系库存 + 区别特征 | `LanguageDef` 新增 `phonemeInventory`；生成期校验 | 设计期校验（SA 隔离） |
| Lexibank / ASJP / CLICS | 词表、同源、共词化 | 自建词表与语义关联的参照 | 设计期 |
| Grambank / WALS / AUTOTYP | 类型学特征签名 | `tools/` 下的校验脚本报告 | 设计期 |
| D-PLACE / Seshat / eHRAF / SAILS | 制度、复杂度、区域语法 | 制度参数先验与阈值校准（文档+手工参数表） | 设计期（NC/订阅不入包） |
| RESOLVE 2017 / Köppen | 生态区、气候带 | 构建期生成 `Biome → 气候/生态` 映射表 | 构建期（许可待核） |
| Neotoma / PMIP | 古植被、过去气候 | 校准 `WorldConfig` 的气候参数（离线） | 构建期校准 |
| Crop Origins / GAEZ / DAD-IS | 作物/产量/家畜 | 生成"可驯化—可生产—可储存"参数表 | 构建期校准 |
| ArchaeoGLOBE / Open Context / tDAR / AADR | 土地利用、遗址、古 DNA | 设计期参照（不入内容） | 设计期 |

## 10. 引用与访问记录

| 来源 | URL | 访问日期 |
|---|---|---|
| Glottolog 5.3 | https://glottolog.org/ ; https://glottolog.org/glottolog/glottologinformation | 2026-10-27 |
| Grambank | https://grambank.clld.org/ | 2026-10-27 |
| PHOIBLE | https://phoible.org/ ; https://phoible.org/legal | 2026-10-27 |
| Lexibank | https://lexibank.clld.org/ | 2026-10-27 |
| ASJP | https://asjp.clld.org/ | 2026-10-27 |
| CLICS⁴ | https://clics.clld.org/ | 2026-10-27 |
| CrossGram | https://crossgram.clld.org/ | 2026-10-27 |
| SAILS | https://sails.clld.org/ ; https://sails.clld.org/legal | 2026-10-27 |
| UniMorph | https://unimorph.github.io/ | 2026-10-27 |
| IE-CoR | https://iecor.clld.org/ | 2026-10-27 |
| D-PLACE | https://d-place.org/ ; https://zenodo.org/records/18229037 | 2026-10-27 |
| Seshat | https://github.com/datasets/seshat（README） | 2026-10-27 |
| eHRAF | https://ehrafworldcultures.yale.edu/ | 2026-10-27 |
| Neotoma | https://www.neotomadb.org/ | 2026-10-27 |
| RESOLVE Ecoregions 2017 | https://ecoregions.appspot.com/ | 2026-10-27 |
| PMIP | https://pmip.lsce.ipsl.fr/ | 2026-10-27 |
| GAEZ | https://www.fao.org/gaez/en/ | 2026-10-27 |
| DAD-IS | https://www.fao.org/dad-is/ | 2026-10-27 |
| Open Context | https://opencontext.org/about/ | 2026-10-27 |

> 规则：任何新增引用，必须带"版本 + URL + 访问日期 + 许可"四要素，缺一不进设计依据。

