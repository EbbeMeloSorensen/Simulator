using System;
using System.Collections.Generic;
using System.Linq;
using Craft.Math;
using Craft.Utils.Linq;
using Simulator.Domain;
using Simulator.Domain.Props;
using Game.TowerDefense.ViewModel.Bodies.Enemies;
using Game.TowerDefense.ViewModel.BodyStates;

namespace Game.TowerDefense.ViewModel
{
    public static class Helpers
    {
        public static void AddPath(
            this Scene scene,
            Path path,
            double width,
            int firstPropId)
        {
            var propId = firstPropId;

            path.WayPoints.AdjacentPairs().ToList().ForEach(_ =>
            {
                AddPathSegment(scene, _.Item1, _.Item2, width, propId++);
            });

            path.WayPoints.ForEach(_ =>
            {
                scene.Props.Add(new PropCircle(propId, width, _));
            });
        }

        public static void AddPathSegment(
            this Scene scene,
            Vector2D start,
            Vector2D end,
            double width,
            int propId)
        {
            if (start.X == end.X)
            {
                var x = start.X;
                var y0 = Math.Min(start.Y, end.Y);
                var y1 = Math.Max(start.Y, end.Y);

                scene.Props.Add(new PropRectangle(propId, width, y1 - y0, new Vector2D(x, (y0 + y1) / 2)));
            }
            else if (start.Y == end.Y)
            {
                var x0 = Math.Min(start.X, end.X);
                var x1 = Math.Max(start.X, end.X);
                var y = start.Y;
                scene.Props.Add(new PropRectangle(propId, x1 - x0, width, new Vector2D((x0 + x1) / 2, y)));
            }
            else
            {
                var v = end - start;
                var w = v.Length;
                var h = width;
                var center = (start + end) / 2;
                var orientation = -v.AsPolarVector().Angle;

                scene.Props.Add(new PropRotatableRectangle(propId, w, h, center, orientation));
            }
        }

        public static int AddPigWave(
            this Dictionary<int, List<BodyStateEnemy>> enemies,
            int stateIndex,
            Path path,
            double speed,
            int life,
            int count,
            int spacing,
            double radius,
            int nextEnemyId)
        {
            Enumerable.Range(0, count).Select(i => new
            {
                StateIndex = i * spacing + stateIndex,
                BodyState = new BodyStateEnemy(new Pig(nextEnemyId + i, radius), path.WayPoints.First())
                {
                    Path = path,
                    Speed = speed,
                    NaturalVelocity = new Vector2D(0.2, 0),
                    Life = life
                }
            }).ToList().ForEach(_ =>
            {
                if (!enemies.ContainsKey(_.StateIndex))
                {
                    enemies[_.StateIndex] = new List<BodyStateEnemy>();
                }

                enemies[_.StateIndex].Add(_.BodyState);
            });

            return nextEnemyId + count;
        }

        public static int AddRabbitWave(
            this Dictionary<int, List<BodyStateEnemy>> enemies,
            int stateIndex,
            Path path,
            double speed,
            int life,
            int count,
            int spacing,
            double radius,
            int nextEnemyId)
        {
            Enumerable.Range(0, count).Select(i => new
            {
                StateIndex = i * spacing + stateIndex,
                BodyState = new BodyStateEnemy(new Rabbit(nextEnemyId + i, radius), path.WayPoints.First())
                {
                    Path = path,
                    Speed = speed,
                    NaturalVelocity = new Vector2D(0.2, 0),
                    Life = life
                }
            }).ToList().ForEach(_ =>
            {
                if (!enemies.ContainsKey(_.StateIndex))
                {
                    enemies[_.StateIndex] = new List<BodyStateEnemy>();
                }

                enemies[_.StateIndex].Add(_.BodyState);
            });

            return nextEnemyId + count;
        }

        public static int AddFireDemonWave(
            this Dictionary<int, List<BodyStateEnemy>> enemies,
            int stateIndex,
            Path path,
            double speed,
            int life,
            int count,
            int spacing,
            double radius,
            int nextEnemyId)
        {
            Enumerable.Range(0, count).Select(i => new
            {
                StateIndex = i * spacing + stateIndex,
                BodyState = new BodyStateEnemy(new FireDemon(nextEnemyId + i, radius), path.WayPoints.First())
                {
                    Path = path,
                    Speed = speed,
                    NaturalVelocity = new Vector2D(0.2, 0),
                    Life = life
                }
            }).ToList().ForEach(_ =>
            {
                if (!enemies.ContainsKey(_.StateIndex))
                {
                    enemies[_.StateIndex] = new List<BodyStateEnemy>();
                }

                enemies[_.StateIndex].Add(_.BodyState);
            });

            return nextEnemyId + count;
        }
    }
}
