using System;
using System.Collections.Generic;
using System.Linq;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain
{
    public class State
    {
        public List<BodyState> BodyStates { get; private set; }

        public int Index { get; set; }

        public State()
        {
            BodyStates = new List<BodyState>();
        }

        public State(
            IEnumerable<BodyState> bodyStates)
        {
            BodyStates = bodyStates.ToList();
        }

        public void AddBodyState(
            BodyState bodyState)
        {
            BodyStates.Add(bodyState);
        }

        public State Clone()
        {
            return new State(BodyStates.Select(bs => bs.Clone()));
        }

        public BodyState TryGetBodyState(
            int bodyId)
        {
            return BodyStates.SingleOrDefault(bs => bs.Body.Id == bodyId);
        }

        public void RemoveBodyState(
            BodyState bodyState)
        {
            BodyStates.Remove(bodyState);
        }

        public void RemoveBodyStates(
            IEnumerable<int> bodyIds)
        {
            BodyStates = BodyStates.Where(bs => !bodyIds.Contains(bs.Body.Id)).ToList();
        }

        public Vector2D CenterOfMass()
        {
            var count = BodyStates.Count;

            if (count == 0)
            {
                return null;
            }

            var totalMass = BodyStates.Select(bs => bs.Body.Mass).Sum();

            return new Vector2D(
                BodyStates.Select(bs => bs.Body.Mass * bs.Position.X / totalMass).Sum(),
                BodyStates.Select(bs => bs.Body.Mass * bs.Position.Y / totalMass).Sum());
        }

        public Vector2D CenterOfInitialBody()
        {
            return BodyStates.Count == 0 ? null : BodyStates.First().Position;
        }

        public double CalculateTotalEnergy(
            double standardGravity)
        {
            var potentialEnergy = BodyStates.Sum(_ => _.Body.Mass * standardGravity * -_.Position.Y);
            var kineticEnergy = BodyStates.Sum(_ => 0.5 * _.Body.Mass * Math.Pow(_.Velocity.Length, 2));

            return potentialEnergy + kineticEnergy;
        }
    }
}
