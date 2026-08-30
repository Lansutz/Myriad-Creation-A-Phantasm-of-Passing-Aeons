# 政体统一化：废弃旧 GovernmentType 单标签枚举，统一到七维 composition，补存档
$ErrorActionPreference = "Stop"

# ── 1. PoliticalSystem.cs：删 governmentType 字段 + 删旧 ReformGovernment ──
$p1 = "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\CivilizationEvolution\Assets\Scripts\Politics\PoliticalSystem.cs"
$c1 = [System.IO.File]::ReadAllText($p1) -replace "`r`n","`n"
function N($s){ $s -replace "`r`n","`n" }

$r1a_old = N @'
        public int realmId;
        public string realmName;
        public GameEnums.GovernmentType governmentType;

        // 财政
'@
$r1a_new = N @'
        public int realmId;
        public string realmName;
        // 政体：旧单标签 GovernmentType 枚举已废弃，统一由下方 composition 七维成分组合表达；
        // 粗分类（君主制/共和制）由 SupremeSuccessionLevel.IsMonarchy/IsRepublic 推导。

        // 财政
'@

$r1b_old = N @'
        /// <summary>政体改革</summary>
        public bool ReformGovernment(int realmId, GameEnums.GovernmentType newType)
        {
            if (!_realms.TryGetValue(realmId, out var realm)) return false;

            // 政体改革触发贵族反抗
            realm.AdjustClassRelation(GameEnums.SocialClass.NobilityClergy, -30f);
            realm.governmentType = newType;
            realm.stability = Mathf.Max(0f, realm.stability - 20f);

            return true;
        }

'@
$r1b_new = N @'
        /// <summary>
        /// 政体改革已统一到七维成分模型——见 GovernmentReform.Reform（按 PolityDimension 改 composition，
        /// 需支撑革新已持有，触发稳定性下降与编年史）。旧的单标签 GovernmentType 枚举与本方法已废弃。
        /// </summary>

'@

foreach($r in @(@{o=$r1a_old;n=$r1a_new;name="del_governmentType_field"},@{o=$r1b_old;n=$r1b_new;name="del_ReformGovernment"})){
  if($c1.Contains($r.o)){ $c1=$c1.Replace($r.o,$r.n); Write-Host ("[PoliticalSystem] OK: "+$r.name) }
  else { Write-Host ("[PoliticalSystem] FAIL: "+$r.name); exit 1 }
}
$c1 = $c1 -replace "`n","`r`n"
[System.IO.File]::WriteAllText($p1,$c1,(New-Object System.Text.UTF8Encoding $false))

# ── 2. GameEnums.cs：删 GovernmentType 枚举 ──
$p2 = "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\CivilizationEvolution\Assets\Scripts\Core\GameEnums.cs"
$c2 = [System.IO.File]::ReadAllText($p2) -replace "`r`n","`n"
$r2_old = N @'
        /// <summary>政体类型</summary>
        public enum GovernmentType
        {
            Tribal,
            Chiefdom,
            Feudal,
            Centralized,
            Theocratic,
            Republic,
            NomadicConfederation
        }

'@
$r2_new = N @'
        /// <summary>政体类型已废弃：政体由 GovernmentComposition 七维成分组合表达，
        /// 粗分类（君主/共和）由 SupremeSuccessionLevel 推导，不再使用单标签枚举。</summary>

'@
if($c2.Contains($r2_old)){ $c2=$c2.Replace($r2_old,$r2_new); Write-Host "[GameEnums] OK: 删 GovernmentType 枚举" }
else { Write-Host "[GameEnums] FAIL: GovernmentType 枚举未匹配"; exit 1 }
$c2 = $c2 -replace "`n","`r`n"
[System.IO.File]::WriteAllText($p2,$c2,(New-Object System.Text.UTF8Encoding $false))

# ── 3. SaveSystem.cs：RealmDTO 删 governmentType，加 composition 整体序列化 ──
$p3 = "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\CivilizationEvolution\Assets\Scripts\Core\SaveSystem.cs"
$c3 = [System.IO.File]::ReadAllText($p3) -replace "`r`n","`n"

$r3a_old = N @'
        public int realmId;
        public string realmName;
        public int governmentType;
        public float treasury;
'@
$r3a_new = N @'
        public int realmId;
        public string realmName;
        /// <summary>政体七维成分组合（整体序列化；GovernmentComposition 及其成员类全部 [Serializable]，无 Dictionary）</summary>
        public GovernmentComposition composition = new GovernmentComposition();
        public float treasury;
'@

$r3b_old = N @'
                realmId = r.realmId,
                realmName = r.realmName,
                governmentType = (int)r.governmentType,
                treasury = r.treasury,
'@
$r3b_new = N @'
                realmId = r.realmId,
                realmName = r.realmName,
                composition = r.composition,
                treasury = r.treasury,
'@

$r3c_old = N @'
                realmId = realmId,
                realmName = realmName,
                governmentType = (GameEnums.GovernmentType)governmentType,
                treasury = treasury,
'@
$r3c_new = N @'
                realmId = realmId,
                realmName = realmName,
                composition = composition,
                treasury = treasury,
'@

foreach($r in @(@{o=$r3a_old;n=$r3a_new;name="dto_add_composition_field"},@{o=$r3b_old;n=$r3b_new;name="from_add_composition"},@{o=$r3c_old;n=$r3c_new;name="to_add_composition"})){
  if($c3.Contains($r.o)){ $c3=$c3.Replace($r.o,$r.n); Write-Host ("[SaveSystem] OK: "+$r.name) }
  else { Write-Host ("[SaveSystem] FAIL: "+$r.name); exit 1 }
}

# SaveSystem 需要 using CivilizationEvolution.Politics（GovernmentComposition 所在命名空间）
if($c3 -notmatch "using CivilizationEvolution\.Politics"){
  $usingOld = N "using CivilizationEvolution.Core;"
  $usingNew = N "using CivilizationEvolution.Core;`nusing CivilizationEvolution.Politics;"
  if($c3.Contains($usingOld)){ $c3=$c3.Replace($usingOld,$usingNew); Write-Host "[SaveSystem] OK: 加 using Politics" }
  else { Write-Host "[SaveSystem] WARN: 未找到 using CivilizationEvolution.Core，需手动加 using" }
}

$c3 = $c3 -replace "`n","`r`n"
[System.IO.File]::WriteAllText($p3,$c3,(New-Object System.Text.UTF8Encoding $false))

Write-Host "`n=== 政体统一化全部完成 ===" -ForegroundColor Green
