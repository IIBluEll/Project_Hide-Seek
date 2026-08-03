using HM.CodeBase;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class AIDebugPanel_view : AView
    {
        private const float PANEL_MARGIN = 12f;
        private const float PANEL_WIDTH = 620f;
        private const float PANEL_HEIGHT = 590f;
        private const float CONTENT_PADDING = 14f;
        private const int FONT_SIZE = 15;

        private string _content = string.Empty;
        private GUIStyle _panelStyle;
        private GUIStyle _contentStyle;

        public override void Clear()
        {
            base.Clear();
            _content = string.Empty;
        }

        public void SetContent(string content)
        {
            _content = content ?? string.Empty;
        }

        private void OnGUI()
        {
            InitializeStyles();

            float panelWidth = Mathf.Min(PANEL_WIDTH , Screen.width - PANEL_MARGIN * 2f);
            float panelHeight = Mathf.Min(PANEL_HEIGHT , Screen.height - PANEL_MARGIN * 2f);
            Rect panelRect = new(PANEL_MARGIN , PANEL_MARGIN , panelWidth , panelHeight);
            Rect contentRect = new(
                panelRect.x + CONTENT_PADDING ,
                panelRect.y + CONTENT_PADDING ,
                panelRect.width - CONTENT_PADDING * 2f ,
                panelRect.height - CONTENT_PADDING * 2f);

            GUI.Box(panelRect , GUIContent.none , _panelStyle);
            GUI.Label(contentRect , _content , _contentStyle);
        }

        private void InitializeStyles()
        {
            if ( _panelStyle != null && _contentStyle != null )
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = Texture2D.grayTexture;

            _contentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft ,
                fontSize = FONT_SIZE ,
                richText = true ,
                wordWrap = false
            };
            _contentStyle.normal.textColor = Color.white;
        }
    }
}
