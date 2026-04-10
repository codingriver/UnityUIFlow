using System;
using System.Collections;
using System.Threading.Tasks;

namespace UnityUIFlow
{
    internal static class UnityUIFlowTestTaskUtility
    {
        public static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                throw task.Exception.Flatten().InnerException;
            }
        }

        public static IEnumerator Await<T>(Task<T> task, Action<T> onCompleted)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                throw task.Exception.Flatten().InnerException;
            }

            onCompleted?.Invoke(task.Result);
        }
    }
}
