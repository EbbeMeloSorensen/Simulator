using System;
using Craft.Math;
using Simulator.Domain;
using Simulator.Domain.BodyStates;
using Simulator.Domain.BodyStates.Interfaces;

namespace Game.TowerDefense.ViewModel.BodyStates;

public class BodyStateProjectile : BodyState, ILifeSpan
{
    public int LifeSpan { get; set; }

    public override Vector2D Velocity
    {
        get => NaturalVelocity;
    }

    protected BodyStateProjectile(
        Body body) : base(body)
    {
    }

    public BodyStateProjectile(
        Body body,
        Vector2D position) : base(body, position)
    {
    }

    public override BodyState Clone()
    {
        return new BodyStateProjectile(Body, Position)
        {
            NaturalVelocity = NaturalVelocity,
            LifeSpan = LifeSpan
        };
    }

    public override BodyState Propagate(
        double time,
        Vector2D force)
    {
        var acceleration = force / Body.Mass;
        var nextNaturalVelocity = NaturalVelocity + time * acceleration;
        var nextPosition = Position + time * NaturalVelocity;

        return new BodyStateProjectile(Body)
        {
            Position = nextPosition,
            NaturalVelocity = nextNaturalVelocity,
            LifeSpan = Math.Max(0, LifeSpan - 1)
        };
    }
}