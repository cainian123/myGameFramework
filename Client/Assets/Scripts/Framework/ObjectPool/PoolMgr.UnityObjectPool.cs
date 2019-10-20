/********************************************************************************
** auth:  https://github.com/HushengStudent
** date:  2018/12/16 20:34:27
** desc:  GameObject����ع���;
*********************************************************************************/

using Framework.ObjectPool;
using Object = UnityEngine.Object;

namespace Framework
{
    public partial class PoolMgr
    {
        /// <summary>
        /// Unity Object Pool;
        /// </summary>
        private UnityObjectPool _unityObjectPool = new UnityObjectPool();

        /// <summary>
        /// ��ȡUnity GameObject;
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public Object GetUnityObject(Object asset)
        {
            if (null == asset)
            {
                return null;
            }
            return _unityObjectPool.GetUnityObject(asset);
        }

        /// <summary>
        /// ����Unity GameObject;
        /// </summary>
        /// <param name="asset"></param>
        public void ReleaseUnityObject(Object asset)
        {
            if (null == asset)
            {
                return;
            }
            _unityObjectPool.ReleaseUnityObject(asset);
        }
    }
}