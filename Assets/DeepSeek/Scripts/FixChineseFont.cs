using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepSeek
{
    /// <summary>
    /// 修复中文乱码
    /// 用法：
    /// 1. 确保项目中已有中文字体资源（如 SimHei SDF）
    /// 2. 将此脚本挂载到任意 GameObject
    /// 3. 在 Inspector 中选择字体
    /// 4. 运行场景，或右键菜单执行修复
    /// </summary>
    [ExecuteInEditMode]
    public class FixChineseFont : MonoBehaviour
    {
        [Header("字体设置")]
        [SerializeField] private TMP_FontAsset chineseFont;
        
        [Header("按钮文字（可选：批量修改为英文）")]
        [SerializeField] private bool autoFixButtonText = false;
        
        private void Start()
        {
            if (autoFixButtonText)
            {
                FixAllButtonText();
            }
            
            if (chineseFont != null)
            {
                FixAllTextInScene();
            }
        }
        
        /// <summary>
        /// 修复场景中所有 TextMeshPro 文本的字体
        /// </summary>
        [ContextMenu("修复所有字体")]
        public void FixAllTextInScene()
        {
            if (chineseFont == null)
            {
                UnityEngine.Debug.LogWarning("请先在 Inspector 中设置中文 TMP 字体资源");
                return;
            }
            
            var allTexts = FindObjectsOfType<TextMeshProUGUI>();
            int count = 0;
            
            foreach (var text in allTexts)
            {
                if (text.font != chineseFont)
                {
                    text.font = chineseFont;
                    count++;
                }
            }
            
            UnityEngine.Debug.Log($"已修复 {count} 个 TextMeshPro 文本的字体");
        }
        
        /// <summary>
        /// 将测试按钮文字批量改为英文（避免中文显示问题）
        /// </summary>
        [ContextMenu("修改按钮文字为英文")]
        public void FixAllButtonText()
        {
            var allTexts = FindObjectsOfType<TextMeshProUGUI>();
            int count = 0;
            
            foreach (var text in allTexts)
            {
                string original = text.text;
                
                switch (original)
                {
                    case "启动 Python":
                        text.text = "Start Python";
                        count++;
                        break;
                    case "停止 Python":
                        text.text = "Stop Python";
                        count++;
                        break;
                    case "发送心跳":
                        text.text = "Ping";
                        count++;
                        break;
                    case "分析意图":
                        text.text = "Parse Intent";
                        count++;
                        break;
                    case "记录事件":
                        text.text = "Log Event";
                        count++;
                        break;
                    case "获取统计":
                        text.text = "Get Stats";
                        count++;
                        break;
                    case "生成报告":
                        text.text = "Generate Report";
                        count++;
                        break;
                    case "结束会话":
                        text.text = "End Session";
                        count++;
                        break;
                    case "请输入要分析的文本...":
                        text.text = "Enter text to analyze...";
                        count++;
                        break;
                }
            }
            
            UnityEngine.Debug.Log($"已修改 {count} 个按钮文字为英文");
        }
    }
}
