using HideSeek.Common;
using HM.CodeBase;
using UnityEngine;

namespace HideSeek.Loading
{
    public sealed class LoadingScreen_view : AView
    {
        private const float MAXIMUM_CONTENT_WIDTH = 920f;

        private GUIStyle _titleStyle;
        private GUIStyle _difficultyStyle;
        private GUIStyle _statusStyle;

        private string _difficultyText = DifficultyProvider.DEFAULT_DIFFICULTY.ToString();
        private string _statusText = "Preparing";
        private float _progress01;

        public override void Clear()
        {
            _difficultyText = DifficultyProvider.DEFAULT_DIFFICULTY.ToString();
            _statusText = "Preparing";
            _progress01 = 0f;
        }

        public void SetDifficulty(GAME_DIFFICULTY difficulty)
        {
            _difficultyText = difficulty.ToString();
        }

        public void SetStatus(string status)
        {
            _statusText = string.IsNullOrWhiteSpace(status) ? "Loading" : status;
        }

        public void SetProgress(float progress01)
        {
            _progress01 = Mathf.Clamp01(progress01);
        }

        private void OnGUI()
        {
            InitializeStyles();

            GUI.depth = -1000;

            Color previousColor = GUI.color;

            GUI.color = new Color(0.012f , 0.016f , 0.022f , 1f);
            GUI.DrawTexture(new Rect(0f , 0f , Screen.width , Screen.height) , Texture2D.whiteTexture);

            float contentWidth = Mathf.Min(Screen.width * 0.76f , MAXIMUM_CONTENT_WIDTH);
            float contentLeft = (Screen.width - contentWidth) * 0.5f;
            float titleTop = Screen.height * 0.35f;

            GUI.color = Color.white;
            GUI.Label(
                new Rect(contentLeft , titleTop , contentWidth , 72f) ,
                "PROJECT ISOLATION" ,
                _titleStyle);

            GUI.Label(
                new Rect(contentLeft , titleTop + 70f , contentWidth , 32f) ,
                $"DIFFICULTY  {_difficultyText}" ,
                _difficultyStyle);

            float barTop = titleTop + 145f;
            Rect barBackgroundRect = new(contentLeft , barTop , contentWidth , 10f);
            Rect barFillRect = new(contentLeft , barTop , contentWidth * _progress01 , 10f);

            GUI.color = new Color(0.12f , 0.15f , 0.18f , 1f);
            GUI.DrawTexture(barBackgroundRect , Texture2D.whiteTexture);

            GUI.color = new Color(0.72f , 0.1f , 0.08f , 1f);
            GUI.DrawTexture(barFillRect , Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(
                new Rect(contentLeft , barTop + 20f , contentWidth , 30f) ,
                $"{_statusText}  {Mathf.RoundToInt(_progress01 * 100f)}%" ,
                _statusStyle);

            GUI.color = previousColor;
        }

        private void InitializeStyles()
        {
            if ( _titleStyle != null )
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft ,
                fontSize = 38 ,
                fontStyle = FontStyle.Bold ,
                normal =
                {
                    textColor = new Color(0.9f , 0.92f , 0.94f , 1f)
                }
            };

            _difficultyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft ,
                fontSize = 18 ,
                fontStyle = FontStyle.Bold ,
                normal =
                {
                    textColor = new Color(0.72f , 0.1f , 0.08f , 1f)
                }
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight ,
                fontSize = 15 ,
                normal =
                {
                    textColor = new Color(0.62f , 0.66f , 0.7f , 1f)
                }
            };
        }
    }
}
