using UnityEngine;

namespace CivilizationEvolution.UI.Common
{
    [CreateAssetMenu(fileName = "TypographyConfig", menuName = "UI/Typography Config")]
    public class TypographyConfig : ScriptableObject
    {
        [System.Serializable]
        public class TextStyle
        {
            public string name;
            public Font font;
            public int fontSize = 14;
            public FontStyle fontStyle = FontStyle.Normal;
            public Color color = new Color(0.816f, 0.784f, 0.722f, 1f);
            public float lineSpacing = 1.2f;
        }

        [Header("UI Sans - Source Han Sans")]
        public TextStyle body = new TextStyle { name = "Body", fontSize = 15, lineSpacing = 1.4f };
        public TextStyle bodySmall = new TextStyle { name = "Body Small", fontSize = 13, lineSpacing = 1.3f };
        public TextStyle caption = new TextStyle { name = "Caption", fontSize = 11, lineSpacing = 1.2f };
        public TextStyle label = new TextStyle { name = "Label", fontSize = 13, fontStyle = FontStyle.Bold };
        public TextStyle button = new TextStyle { name = "Button", fontSize = 15, fontStyle = FontStyle.Bold };
        public TextStyle tab = new TextStyle { name = "Tab", fontSize = 15, fontStyle = FontStyle.Bold };
        public TextStyle number = new TextStyle { name = "Number", fontSize = 15, fontStyle = FontStyle.Bold };

        [Header("UI Serif - Source Han Serif")]
        public TextStyle tooltipTitle = new TextStyle { name = "Tooltip Title", fontSize = 18, fontStyle = FontStyle.Bold };
        public TextStyle windowTitle = new TextStyle { name = "Window Title", fontSize = 26, fontStyle = FontStyle.Bold };
        public TextStyle characterName = new TextStyle { name = "Character Name", fontSize = 22, fontStyle = FontStyle.Bold };
        public TextStyle entityName = new TextStyle { name = "Entity Name", fontSize = 20, fontStyle = FontStyle.Bold };
        public TextStyle historyText = new TextStyle { name = "History Text", fontSize = 16, lineSpacing = 1.55f };

        [Header("Colors - Dark Panel")]
        public Color textPrimary = new Color(0.816f, 0.784f, 0.722f, 1f);
        public Color textSecondary = new Color(0.667f, 0.635f, 0.576f, 1f);
        public Color textDisabled = new Color(0.467f, 0.443f, 0.400f, 1f);
        public Color textTitle = new Color(0.839f, 0.800f, 0.722f, 1f);

        [Header("Colors - Light Tooltip")]
        public Color tooltipTextPrimary = new Color(0.224f, 0.212f, 0.184f, 1f);
        public Color tooltipTextSecondary = new Color(0.384f, 0.361f, 0.318f, 1f);

        [Header("Semantic Colors")]
        public Color positive = new Color(0.373f, 0.471f, 0.392f, 1f);
        public Color negative = new Color(0.529f, 0.345f, 0.310f, 1f);
        public Color important = new Color(0.502f, 0.424f, 0.286f, 1f);
    }
}
