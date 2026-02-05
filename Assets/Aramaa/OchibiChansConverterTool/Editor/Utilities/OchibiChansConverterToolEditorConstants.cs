#if UNITY_EDITOR
// Assets/Aramaa/OchibiChansConverterTool/Editor/Utilities/OchibiChansConverterToolEditorConstants.cs
//
// =====================================================================
// 概要
// =====================================================================
// - Editor 拡張で共通利用する定数（バージョン・URL 等）をまとめたクラスです。
// - 特殊な文字列や数字を一箇所に集約し、変更点を追いやすくします。
//
// =====================================================================

namespace Aramaa.OchibiChansConverterTool.Editor.Utilities
{
    /// <summary>
    /// OchibiChansConverterTool の Editor 共有定数をまとめます。
    /// </summary>
    internal static class OchibiChansConverterToolEditorConstants
    {
        public const string ToolVersion = "0.3.4";
        public const string LatestVersionUrl = "https://raw.githubusercontent.com/aramaa-vr/ochibi-chans-converter-tool/master/Assets/Aramaa/OchibiChansConverterTool/package.json";
        public const string SupportDiscordUrl = "https://discord.gg/BJ3BpVnMna";
        public const string ToolsMenuPath = "Tools/Aramaa/Ochibi-chans おちびちゃんズ化ツール";
        public const string GameObjectMenuPath = "GameObject/Aramaa/Ochibi-chans おちびちゃんズ化ツール";
        public const string FaceMeshCacheFileName = "ChibiFaceMeshCache.json";
        public const string BaseFolder = "Assets/夕時茶屋";
        public const string AddMenuPrefabFileName = "Ochibichans_Addmenu.prefab";
        public const string AddMenuNameKeyword = "Ochibichans_Addmenu";
    }
}
#endif
