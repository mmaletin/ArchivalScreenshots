
#if UNITY_EDITOR

namespace ArchivalScreenshots
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Based on a class from unity recorder package
    /// </summary>
    internal static class GameViewUtils
    {
        const int miscSize = 1; // Used when no main GameView exists (ex: batchmode)

        public static object currentSize
        {
            get
            {
                var gv = GetMainGameView();
                if (gv == null)
                    return new[] { miscSize, miscSize };
                var prop = gv.GetType().GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                return prop.GetValue(gv, new object[] { });
            }
        }

        private static object initialSize;

        public static void SetTempResolution(Vector2Int resolution)
        {
            initialSize = currentSize;

            var sizeObj = NewSizeObj(resolution.x, resolution.y);

            var group = Group();
            var AddCustomSize = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            AddCustomSize.Invoke(group, new[] { sizeObj });

            SelectSize(sizeObj);
        }

        public static void RestoreResolution()
        {
            if (initialSize == null)
            {
                return;
            }

            SelectSize(initialSize);

            var group = Group();

            var GetTotalCount = group.GetType().GetMethod("GetTotalCount", BindingFlags.Public | BindingFlags.Instance);
            int totalCount = (int)GetTotalCount.Invoke(group, null);

            var RemoveCustomSize = group.GetType().GetMethod("RemoveCustomSize", BindingFlags.Public | BindingFlags.Instance);
            RemoveCustomSize.Invoke(group, new object[] { (totalCount - 1) });

            initialSize = null;
        }

        public static void SelectSize(object size)
        {
            var index = IndexOf(size);

            var gameView = GetMainGameView();
            if (gameView == null)
                return;
            var obj = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(gameView, new[] { index, size });
        }

        #region Reflection

#if UNITY_2019_3_OR_NEWER
        static Type s_GameViewType = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainPlayModeView";
#else
        static Type s_GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainGameView";
#endif
        static EditorWindow GetMainGameView()
        {
            var getMainGameView = s_GameViewType.GetMethod(s_GetGameViewFuncName, BindingFlags.NonPublic | BindingFlags.Static);
            if (getMainGameView == null)
            {
                Debug.LogError(string.Format("Can't find the main Game View : {0} function was not found in {1} type ! Did API change ?",
                    s_GetGameViewFuncName, s_GameViewType));
                return null;
            }
            var res = getMainGameView.Invoke(null, null);
            return (EditorWindow)res;
        }

        static object NewSizeObj(int width, int height)
        {
            var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            var c = T.GetConstructor(new[] { tt, typeof(int), typeof(int), typeof(string) });
            var sizeObj = c.Invoke(new object[] { 1, width, height, "[TEMP]" });
            return sizeObj;
        }

        static object Group()
        {
            var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = sizes.GetValue(null, new object[] { });

            var currentGroup = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
            var group = currentGroup.GetValue(instance, new object[] { });
            return group;
        }

        static int IndexOf(object sizeObj)
        {
            var group = Group();
            var method = group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            var index = (int)method.Invoke(group, new[] { sizeObj });

            var builtinList = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            method = builtinList.GetType().GetMethod("Contains");
            if ((bool)method.Invoke(builtinList, new[] { sizeObj }))
                return index;

            method = group.GetType().GetMethod("GetBuiltinCount");
            index += (int)method.Invoke(group, new object[] { });

            return index;
        }

        #endregion
    }
}

#endif