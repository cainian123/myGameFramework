/********************************************************************************
** auth:  https://github.com/HushengStudent
** date:  2018/06/18 18:02:43
** desc:  等级条件执行节点;
*********************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class BTLevel : AbsDecorator
    {
        private int _level;

        public BTLevel(Hashtable table) : base(table)
        {
            string str = table["Level"].ToString();
            if (!int.TryParse(str, out _level))
            {
                LogUtil.LogUtility.PrintError("[BTLevel]get level is error!");
            }
        }

        protected override void AwakeEx()
        {
        }

        protected override void Reset()
        {
        }

        protected override void UpdateEx(float interval)
        {
            LogUtil.LogUtility.PrintWarning(string.Format("BTLevel's level:{0}", _level));
            Reslut = BehaviorState.Finish;
        }
    }
}