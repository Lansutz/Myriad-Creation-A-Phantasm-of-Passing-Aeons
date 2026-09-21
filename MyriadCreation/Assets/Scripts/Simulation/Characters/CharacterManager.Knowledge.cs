using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 角色知识系统访问层。
    /// 只暴露只读枚举，避免研究系统直接取得 CharacterManager 的内部字典。
    /// </summary>
    public partial class CharacterManager
    {
        public IEnumerable<CharacterData> GetAllCharacters()
        {
            return _characters.Values;
        }
    }
}
