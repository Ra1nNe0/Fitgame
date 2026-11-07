using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Дорожка с препятствиями. Работает по расписанию: игра заранее говорит,
    /// КОГДА препятствие должно дойти до игрока, а дорожка сама решает, когда его выпустить.
    /// Так быстрые и медленные препятствия никогда не приходят одновременно.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        /// <summary>Препятствие дошло до игрока — пора проверить движение.</summary>
        public event Action<Obstacle> ReachedPlayer;
        /// <summary>Препятствие появилось на экране.</summary>
        public event Action<Obstacle> Spawned;
        /// <summary>Отбитый шар долетел до босса.</summary>
        public event Action OrbHitBoss;

        struct Planned
        {
            public ObstacleKind Kind;
            public float Arrival;   // момент, когда препятствие должно дойти до игрока
        }

        readonly List<Planned> plan = new List<Planned>();
        readonly List<Obstacle> active = new List<Obstacle>();
        readonly List<Obstacle> reached = new List<Obstacle>();

        float playerX;
        float groundY;
        float despawnX;
        float originX;      // откуда вылетают препятствия
        float bossX;
        float clock;
        float lastArrival;

        public void Setup(float playerX, float groundY, float despawnX)
        {
            this.playerX = playerX;
            this.groundY = groundY;
            this.despawnX = despawnX;
        }

        /// <summary>Начать уровень: очистить дорожку и обнулить часы.</summary>
        public void Begin(float firstDelay, float originX, float bossX)
        {
            Clear();
            this.originX = originX;
            this.bossX = bossX;
            clock = 0f;
            lastArrival = firstDelay;
        }

        /// <summary>На сколько секунд вперёд уже составлено расписание.</summary>
        public float PlannedAhead
        {
            get { return lastArrival - clock; }
        }

        /// <summary>Сколько обязательных препятствий ещё не проверено.</summary>
        public int Unresolved
        {
            get
            {
                int count = 0;
                foreach (Planned p in plan)
                    if (!Obstacle.IsBonusKind(p.Kind)) count++;
                foreach (Obstacle o in active)
                    if (!o.Resolved && !o.IsBonus) count++;
                return count;
            }
        }

        /// <summary>Добавить препятствие в расписание через gap секунд после предыдущего.</summary>
        public void Plan(ObstacleKind kind, float gap)
        {
            lastArrival = Mathf.Max(lastArrival, clock) + gap;
            plan.Add(new Planned { Kind = kind, Arrival = lastArrival });
        }

        /// <summary>Убрать всё с дорожки.</summary>
        public void Clear()
        {
            foreach (Obstacle o in active)
                if (o != null) Destroy(o.gameObject);
            active.Clear();
            plan.Clear();
        }

        public void Remove(Obstacle obstacle)
        {
            if (active.Remove(obstacle)) Destroy(obstacle.gameObject);
        }

        /// <summary>Один шаг игры: вызывается каждый кадр, пока идёт уровень.</summary>
        public void Tick(float deltaTime, float speed)
        {
            clock += deltaTime;

            // Выпускаем запланированные препятствия с таким расчётом, чтобы они пришли вовремя.
            for (int i = plan.Count - 1; i >= 0; i--)
            {
                Planned p = plan[i];
                float travelTime = (originX - playerX) / (speed * Obstacle.SpeedFactorOf(p.Kind));
                if (clock >= p.Arrival - travelTime)
                {
                    plan.RemoveAt(i);
                    Obstacle created = Obstacle.Create(p.Kind, new Vector3(originX, groundY, 0f));
                    active.Add(created);
                    if (Spawned != null) Spawned(created);
                }
            }

            reached.Clear();
            int orbHits = 0;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Obstacle o = active[i];
                o.Advance(speed * deltaTime, deltaTime);
                float x = o.transform.position.x;

                if (o.Reflected)
                {
                    if (x >= bossX)
                    {
                        active.RemoveAt(i);
                        Destroy(o.gameObject);
                        orbHits++;
                    }
                }
                else if (!o.Resolved && x <= playerX + o.ContactDistance)
                {
                    o.Resolved = true;
                    reached.Add(o);
                }
                else if (x < despawnX)
                {
                    active.RemoveAt(i);
                    Destroy(o.gameObject);
                }
            }

            // Сообщаем после цикла: обработчики могут менять список препятствий.
            foreach (Obstacle o in reached)
                if (ReachedPlayer != null) ReachedPlayer(o);
            for (int i = 0; i < orbHits; i++)
                if (OrbHitBoss != null) OrbHitBoss();
        }
    }
}
