using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MyTime.LowLevel
{
    /// <summary>
    /// Unity PlayerLoop의 Update 단계에 커스텀 시간 시스템 마커를 삽입하고 관리합니다.
    /// </summary>
    public static class CustomLoopManager
    {
        // Profiler에 표시될 고유 마커 타입
        public struct CustomTimeSystemMarker { }

        /// <summary>
        /// 커스텀 시간 루프에서 매 프레임 발생하는 업데이트 이벤트
        /// </summary>
        public static event Action OnCustomTimeUpdate;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Domain Reload 비활성화 대응 (Enter Play Mode Options)
            OnCustomTimeUpdate = null;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            PlayerLoopSystem currentLoop = PlayerLoop.GetCurrentPlayerLoop();

            RemoveSystem(ref currentLoop, typeof(CustomTimeSystemMarker));

            PlayerLoopSystem customSystem = new PlayerLoopSystem
            {
                type = typeof(CustomTimeSystemMarker),
                updateDelegate = OnCustomUpdate
            };

            if (InsertSystem(ref currentLoop, typeof(Update), customSystem))
            {
                PlayerLoop.SetPlayerLoop(currentLoop);
            }
        }

        private static void OnCustomUpdate()
        {
            if (!Application.isPlaying) return;

            OnCustomTimeUpdate?.Invoke();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitEditorCleanup()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                // 플레이 모드를 나갈 때 내 노드만 깔끔하게 제거
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    PlayerLoopSystem currentLoop = PlayerLoop.GetCurrentPlayerLoop();
                    if (RemoveSystem(ref currentLoop, typeof(CustomTimeSystemMarker)))
                    {
                        PlayerLoop.SetPlayerLoop(currentLoop);
                    }
                }
            };
        }
#endif

        // 특정 마커 타입을 트리에서 재귀적으로 찾아 제거
        public static bool RemoveSystem(ref PlayerLoopSystem root, Type targetMarkerType)
        {
            if (root.subSystemList == null || root.subSystemList.Length == 0)
                return false;

            var list = new List<PlayerLoopSystem>(root.subSystemList);
            bool removed = false;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].type == targetMarkerType)
                {
                    list.RemoveAt(i);
                    removed = true;
                }
                else
                {
                    var child = list[i];
                    if (RemoveSystem(ref child, targetMarkerType))
                    {
                        list[i] = child;
                        removed = true;
                    }
                }
            }

            if (removed)
            {
                root.subSystemList = list.ToArray();
            }

            return removed;
        }

        private static bool InsertSystem(ref PlayerLoopSystem root, Type targetType, PlayerLoopSystem systemToInsert)
        {
            if (root.type == targetType)
            {
                List<PlayerLoopSystem> subSystems = root.subSystemList != null
                    ? new List<PlayerLoopSystem>(root.subSystemList)
                    : new List<PlayerLoopSystem>();

                subSystems.Add(systemToInsert);
                root.subSystemList = subSystems.ToArray();
                return true;
            }

            if (root.subSystemList == null) return false;

            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                if (InsertSystem(ref root.subSystemList[i], targetType, systemToInsert))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
