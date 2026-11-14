#pragma warning disable 0618 // 迁移期：DTO 需保留七板块旧字段，5A.3 聚合迁移完成后移除
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Culture;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// 文化入档/还原映射（A2 迁移：映射器留在状态所有者侧，存档层只持 DTO）。
    /// </summary>
    public static class CultureSaveMapping
    {
        public static CultureSaveData ToSave(CultureData culture)
        {
            if (culture == null) return null;

            var dto = new CultureSaveData
            {
                cultureId = culture.cultureId,
                cultureName = culture.cultureName ?? "",
                cultureColorHex = culture.cultureColorHex ?? "#ffffff",
                stage = culture.stage,
                livelihoodType = culture.livelihoodType,
                mobilityType = culture.mobilityType,
                burialType = culture.burialType,
                burialTypes = culture.burialTypes != null ? new List<int>(culture.burialTypes) : new List<int>(),
                worshipVector = culture.worshipVector,
                materialStyle = culture.materialStyle,
                symbolicFocus = culture.symbolicFocus,
                symbolicFoci = culture.symbolicFoci != null ? new List<int>(culture.symbolicFoci) : new List<int>(),
                environmentAdapt = culture.environmentAdapt,
                environmentAdapts = culture.environmentAdapts != null ? new List<int>(culture.environmentAdapts) : new List<int>(),
                productionPreference = culture.productionPreference,
                faithId = culture.faithId,
                languageId = culture.languageId ?? "",
                languageFamilyId = culture.languageFamilyId ?? "",
                ethnicGroupId = culture.ethnicGroupId ?? "",
                heritageId = culture.heritageId ?? "",
                ethosId = culture.ethosId ?? "",
                traditionIds = culture.traditionIds != null ? new List<string>(culture.traditionIds) : new List<string>(),
                maturity = culture.maturity,
                spreadPower = culture.spreadPower,
                parentCultureId = culture.parentCultureId,
                childCultureIds = culture.childCultureIds != null ? new List<int>(culture.childCultureIds) : new List<int>(),
                allowsBranching = culture.allowsBranching,
                heraldryEnabled = culture.heraldryEnabled,
                mergedIntoCultureId = culture.mergedIntoCultureId,
                lastSplitDay = culture.lastSplitDay,
                color = culture.color,
                innovationAffinities = culture.innovationAffinities != null ? new List<string>(culture.innovationAffinities) : new List<string>()
            };

            InheritanceLaw law = culture.defaultSuccessionLaw ?? InheritanceLaw.Primogeniture();
            dto.defaultSuccessionLaw = new InheritanceLawSaveData
            {
                scope = (int)law.scope,
                branch = (int)law.branch,
                gender = (int)law.gender,
                age = (int)law.age,
                titleMode = (int)law.titleMode,
                landMode = (int)law.landMode
            };

            if (culture.officialTitles != null)
            {
                foreach (OfficeTitleEntry title in culture.officialTitles)
                {
                    if (title == null) continue;
                    dto.officialTitles.Add(new OfficeTitleSaveData
                    {
                        office = title.office ?? "",
                        polityKey = title.polityKey ?? "",
                        titleKey = title.titleKey ?? ""
                    });
                }
            }

            return dto;
        }

        public static CultureData ToRuntime(CultureSaveData dto)
        {
            if (dto == null) return null;

            var culture = new CultureData
            {
                cultureId = dto.cultureId,
                cultureName = dto.cultureName ?? "",
                cultureColorHex = dto.cultureColorHex ?? "#ffffff",
                stage = dto.stage,
                livelihoodType = dto.livelihoodType,
                mobilityType = dto.mobilityType,
                burialType = dto.burialType,
                burialTypes = dto.burialTypes != null ? new List<int>(dto.burialTypes) : new List<int>(),
                worshipVector = dto.worshipVector,
                materialStyle = dto.materialStyle,
                symbolicFocus = dto.symbolicFocus,
                symbolicFoci = dto.symbolicFoci != null ? new List<int>(dto.symbolicFoci) : new List<int>(),
                environmentAdapt = dto.environmentAdapt,
                environmentAdapts = dto.environmentAdapts != null ? new List<int>(dto.environmentAdapts) : new List<int>(),
                productionPreference = dto.productionPreference,
                faithId = dto.faithId,
                languageId = dto.languageId ?? "",
                languageFamilyId = dto.languageFamilyId ?? "",
                ethnicGroupId = dto.ethnicGroupId ?? "",
                heritageId = dto.heritageId ?? "",
                ethosId = dto.ethosId ?? "",
                traditionIds = dto.traditionIds != null ? new List<string>(dto.traditionIds) : new List<string>(),
                maturity = dto.maturity,
                spreadPower = dto.spreadPower,
                parentCultureId = dto.parentCultureId,
                childCultureIds = dto.childCultureIds != null ? new List<int>(dto.childCultureIds) : new List<int>(),
                allowsBranching = dto.allowsBranching,
                heraldryEnabled = dto.heraldryEnabled,
                mergedIntoCultureId = dto.mergedIntoCultureId,
                lastSplitDay = dto.lastSplitDay,
                color = dto.color,
                innovationAffinities = dto.innovationAffinities != null ? new List<string>(dto.innovationAffinities) : new List<string>()
            };

            InheritanceLawSaveData law = dto.defaultSuccessionLaw;
            culture.defaultSuccessionLaw = law != null
                ? new InheritanceLaw(
                    (InheritanceScope)law.scope,
                    (InheritanceBranch)law.branch,
                    (InheritanceGender)law.gender,
                    (InheritanceAge)law.age,
                    (TitleInheritanceMode)law.titleMode,
                    (LandInheritanceMode)law.landMode)
                : InheritanceLaw.Primogeniture();

            if (dto.officialTitles != null)
            {
                foreach (OfficeTitleSaveData title in dto.officialTitles)
                {
                    if (title == null) continue;
                    culture.officialTitles.Add(new OfficeTitleEntry
                    {
                        office = title.office ?? "",
                        polityKey = title.polityKey ?? "",
                        titleKey = title.titleKey ?? ""
                    });
                }
            }

            return culture;
        }
    }
}
