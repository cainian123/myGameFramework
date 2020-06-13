/********************************************************************************
** auth:  https://github.com/HushengStudent
** date:  2018/05/13 16:04:39
** desc:  导表生成c#;
*********************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FrameworkEditor
{
    public static class TableExportCs
    {
        public static Dictionary<TableFiledType, string> _tableTypeDict = new Dictionary<TableFiledType, string>()
        {
            {TableFiledType.INT,"int"},
            {TableFiledType.FLOAT,"float"},
            {TableFiledType.STRING,"string"},
            {TableFiledType.BOOL,"bool"},
        };
        public static Dictionary<TableFiledType, string> _tableReadDict = new Dictionary<TableFiledType, string>()
        {
            {TableFiledType.INT,"        ReadInt32(ref byteArr,ref bytePos,out {0});"},
            {TableFiledType.FLOAT,"        ReadFloat(ref byteArr,ref bytePos,out {0});"},
            {TableFiledType.STRING,"        ReadString(ref byteArr,ref bytePos,out {0});"},
            {TableFiledType.BOOL,"        ReadBoolean(ref byteArr,ref bytePos,out {0});"}
        };
        private static Dictionary<int, List<string>> _infoDict = new Dictionary<int, List<string>>();
        private static string _targetPath = Application.dataPath.ToLower() + "/Scripts/Common/Table/";
        private static string _fileName = string.Empty;
        private static string _code = string.Empty;

        public static void ExportCs(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            _infoDict.Clear();
            _fileName = string.Empty;
            _code = string.Empty;

            _infoDict = TableReader.ReadCsvFile(path);
            _code = template;
            if (_infoDict.ContainsKey(2))
            {
                _fileName = Path.GetFileNameWithoutExtension(path);
                var filePath = _targetPath + _fileName + ".cs";
                _code = _code.Replace("#fileName#", _fileName);

                var fields = string.Empty;
                var mainKey = string.Empty;
                var funcs = string.Empty;

                var line = _infoDict[2];
                for (var i = 0; i < line.Count; i++)
                {
                    var target = line[i];
                    var temp = target.Split(":".ToArray());
                    var type = TableFiledType.STRING;
                    if (temp.Length < 2)
                    {
                        LogHelper.PrintWarning(string.Format("#配表未指定类型{0}行,{1}列,请先初始化#path:" + path, 2.ToString(), i.ToString()));
                        return;
                    }
                    else
                    {
                        type = TableReader.GetTableFiledType(temp[1]);
                    }
                    fields = fields + "\r\n " + "    public " + _tableTypeDict[type].ToString() + " " + temp[0] + ";";
                    if (i == 0)
                    {
                        mainKey = temp[0];
                    }
                    funcs = funcs + "\r\n " + string.Format(_tableReadDict[type], temp[0]);
                }
                _code = _code.Replace("#fields#", fields);
                _code = _code.Replace("#mainKey#", mainKey);
                _code = _code.Replace("#function#", funcs);
                File.WriteAllText(filePath, _code);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "cs 导出成功，等待编译通过！", "确认");
            }
        }

        public static string template =
            "//------------------------------------------------------------------------------\r\n" +
            "// <auto-generated>\r\n" +
            "//     This code was generated by a tool.\r\n" +
            "//\r\n" +
            "//     Changes to this file may cause incorrect behavior and will be lost if\r\n" +
            "//     the code is regenerated.\r\n" +
            "// </auto-generated>\r\n" +
            "//------------------------------------------------------------------------------\r\n" +
            " \r\n" +
            "using Framework; \r\n" +
            "using System.Collections; \r\n" +
            "using System.Collections.Generic; \r\n" +
            "using UnityEngine; \r\n" +
            " \r\n" +
            "public class #fileName#DB : BaseTableDB<#fileName#>\r\n" +
            "{\r\n" +
            "    public static readonly #fileName#DB instance=new #fileName#DB();\r\n" +
            "}\r\n" +
            " \r\n" +
            "// Generated from: #fileName#.csv\r\n" +
            "public class #fileName# : TableData\r\n" +
            "{\r\n" +
            "#fields#\r\n" +
            " \r\n" +
            "    public override int Key\r\n" +
            "    {\r\n" +
            "        get\r\n" +
            "        {\r\n" +
            "            return #mainKey#; \r\n" +
            "        }\r\n" +
            "    }\r\n" +
            " \r\n" +
            "    public override void Decode(byte[] byteArr, ref int bytePos)\r\n" +
            "    {\r\n" +
            "#function#\r\n" +
            " \r\n" +
            "    }\r\n" +
            " \r\n" +
            "}\r\n";
    }
}