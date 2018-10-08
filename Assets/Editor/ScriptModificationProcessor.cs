using UnityEditor;
using UnityEngine;
using System.Collections;

public class ScriptModificationProcessor : UnityEditor.AssetModificationProcessor
{
    public static void OnWillCreateAsset(string path)
    {
        path = path.Replace(".meta", "");
        int index = path.LastIndexOf(".");
        if (index != -1)
        {
            string file = path.Substring(index);

            if (file != ".cs" && file != ".js" && file != ".boo") return;

            index = Application.dataPath.LastIndexOf("Assets");
            path = Application.dataPath.Substring(0, index) + path;
            file = System.IO.File.ReadAllText(path);

            file = file.Replace("#COMPANYNAMESPACE#", PascalCaseNoSpaces(PlayerSettings.companyName));
            file = file.Replace("#PRODUCTNAMESPACE#", PascalCaseNoSpaces(PlayerSettings.productName));

            System.IO.File.WriteAllText(path, file);
            AssetDatabase.Refresh();
        }
    }

    public static string PascalCaseNoSpaces(string s)
    {
        string[] words = s.Split(' ');
        string result = string.Empty;

        foreach (string word in words)
        {
            result += char.ToUpper(word[0]) + word.Substring(1);
        }

        return result;
    }
}