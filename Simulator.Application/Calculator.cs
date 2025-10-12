using System;
using System.Collections.Generic;
using System.Linq;
using Craft.Logging;
using Craft.Math;
using Simulator.Domain;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;

namespace Simulator.Application
{
    // Denne klasse er stateless. Den indeholder den matematik, der bruges til at fremskrive tilstanden for et fysisk system og
    // bruges af Engine-klassen, som i øvrigt er den, der holder staten
    public static class Calculator
    {
        private enum StateEvent
        {
            None,
            CollisionWithBoundary,
            CollisionBetweenBodies
        }

        public static State PropagateState(
            Scene scene,
            State state,
            double deltaT,
            ILogger logger,
            out List<BoundaryCollisionReport> boundaryCollisionReports,
            out List<BodyCollisionReport> bodyCollisionReports) // Dictionary of body id vs effective normal vectors of boundary collisions that took place in the propagation
        {
            if (state.Index == 1114)
            {
                var a = 0;
            }

            if (scene == null)
            {
                throw new InvalidOperationException("Please set a scene before calling Engine.PropagateState");
            }

            boundaryCollisionReports = new List<BoundaryCollisionReport>();
            bodyCollisionReports = new List<BodyCollisionReport>();

            var timeLeftInCurrentIncrement = deltaT;
            var idsOfHandledBodies = new HashSet<int>();
            var handledCollisions = new HashSet<Tuple<int, int>>();
            var indexOfInputState = state.Index;

            logger?.WriteLine(LogMessageCategory.Debug, $"Propagating state {indexOfInputState}:", "propagation");

            var iteration = 1;
            while (timeLeftInCurrentIncrement > 1E-12)
            {
                if (iteration > 100)
                {
                    // Something wrong
                    var a = 0;
                }

                // Beregn positionsforskydninger givet de gældende kræfter (hvor vi vel at mærke ikke tager højde for boundaries)
                var propagatedBodyStateMap = CalculatePropagatedBodyStateMap(
                    state,
                    scene.StandardGravity,
                    scene.GravitationalConstant,
                    scene.CoefficientOfFriction,
                    scene.IncludeCustomForces,
                    timeLeftInCurrentIncrement,
                    idsOfHandledBodies);

                // Identify first (if any) collision with boundary
                IdentifyFirstCollisionWithABoundary(
                        propagatedBodyStateMap,
                        scene.Boundaries,
                        timeLeftInCurrentIncrement,
                        out var bodyState,
                        out var boundary,
                        out var timeUntilCollisionWithBoundary,
                        out var lineSegmentEndPointInvolvedInCollision,
                        out var effectiveSurfaceNormalForBoundary);

                BodyState bodyState1 = null;
                BodyState bodyState2 = null;
                var timeUntilCollisionBetweenBodies = double.NaN;

                if (scene.HandleBodyCollisions)
                {
                    IdentifyFirstCollisionBetweenTwoBodies(
                        propagatedBodyStateMap,
                        scene.CheckForCollisionBetweenBodiesCallback,
                        idsOfHandledBodies,
                        handledCollisions,
                        timeLeftInCurrentIncrement,
                        out bodyState1,
                        out bodyState2,
                        out timeUntilCollisionBetweenBodies);
                }

                StateEvent stateEvent;

                if (double.IsNaN(timeUntilCollisionWithBoundary))
                {
                    stateEvent = double.IsNaN(timeUntilCollisionBetweenBodies)
                        ? StateEvent.None
                        : StateEvent.CollisionBetweenBodies;
                }
                else
                {
                    if (double.IsNaN(timeUntilCollisionBetweenBodies))
                    {
                        stateEvent = StateEvent.CollisionWithBoundary;
                    }
                    else
                    {
                        stateEvent = timeUntilCollisionWithBoundary < timeUntilCollisionBetweenBodies
                            ? StateEvent.CollisionWithBoundary
                            : StateEvent.CollisionBetweenBodies;
                    }
                }

                var timeElapsed = double.NaN;

                switch (stateEvent)
                {
                    case StateEvent.None:
                        state = new State(propagatedBodyStateMap.Keys.ToList());
                        timeLeftInCurrentIncrement = 0.0;
                        logger?.WriteLine(LogMessageCategory.Debug, $"  Iteration {iteration}, progress: 100%", "propagation");
                        logger?.WriteLine(LogMessageCategory.Debug, "    Result:", "propagation");
                        LogState(state, logger);
                        break;
                    case StateEvent.CollisionWithBoundary:
                        PropagateStatePartly(propagatedBodyStateMap, timeUntilCollisionWithBoundary, timeLeftInCurrentIncrement);
                        boundaryCollisionReports.Add(new BoundaryCollisionReport(bodyState, boundary, effectiveSurfaceNormalForBoundary));

                        // Figure out what should happen with the body that collided with a boundary by asking the scene
                        if (scene.CollisionBetweenBodyAndBoundaryOccuredCallBack != null)
                        {
                            var outcome = scene.CollisionBetweenBodyAndBoundaryOccuredCallBack.Invoke(bodyState.Body);

                            switch (outcome)
                            {
                                case OutcomeOfCollisionBetweenBodyAndBoundary.Block:
                                    {
                                        propagatedBodyStateMap[bodyState].EliminateVelocityComponentTowardsGivenSurfaceNormal(effectiveSurfaceNormalForBoundary);
                                        state = new State(propagatedBodyStateMap.Values.ToList());
                                        idsOfHandledBodies.Add(bodyState.Body.Id);
                                        break;
                                    }
                                case OutcomeOfCollisionBetweenBodyAndBoundary.Reflect:
                                    {
                                        propagatedBodyStateMap[bodyState].ReflectVelocity(boundary, lineSegmentEndPointInvolvedInCollision, effectiveSurfaceNormalForBoundary);
                                        state = new State(propagatedBodyStateMap.Values.ToList());
                                        //idsOfHandledBodies.Add(bodyState.Body.Id);
                                        break;
                                    }
                                default:
                                    throw new ArgumentException();
                            }
                        }
                        else
                        {
                            // The scene doesn't have an opinion, but it actually should
                            throw new InvalidOperationException("No callback for handling collision between body and boundary was provided by the scene");
                        }

                        timeElapsed = timeUntilCollisionWithBoundary;
                        timeLeftInCurrentIncrement -= timeElapsed;
                        logger?.WriteLine(LogMessageCategory.Debug, $"  Body{bodyState.Body.Id} collided with boundary after {timeUntilCollisionWithBoundary} seconds. Time Left: {timeLeftInCurrentIncrement} seconds", "propagation");
                        logger?.WriteLine(LogMessageCategory.Debug, $"  Iteration {iteration} progress: {100 * (deltaT - timeLeftInCurrentIncrement) / deltaT:F5}%", "propagation");
                        logger?.WriteLine(LogMessageCategory.Debug, "    Result:", "propagation");
                        LogState(state, logger);
                        break;
                    case StateEvent.CollisionBetweenBodies:
                        PropagateStatePartly(propagatedBodyStateMap, timeUntilCollisionBetweenBodies, timeLeftInCurrentIncrement);
                        bodyCollisionReports.Add(new BodyCollisionReport(bodyState1.Body, bodyState2.Body));

                        // Figure out what should happen with the two bodies that collided by asking the scene
                        if (scene.CollisionBetweenTwoBodiesOccuredCallBack != null)
                        {
                            var outcome = scene.CollisionBetweenTwoBodiesOccuredCallBack.Invoke(
                                bodyState1.Body, bodyState2.Body);

                            switch (outcome)
                            {
                                case OutcomeOfCollisionBetweenTwoBodies.Block:
                                    {
                                        propagatedBodyStateMap[bodyState1].NaturalVelocity = new Vector2D(0, 0);
                                        propagatedBodyStateMap[bodyState2].NaturalVelocity = new Vector2D(0, 0);
                                        state = new State(propagatedBodyStateMap.Values.ToList());
                                        idsOfHandledBodies.Add(bodyState1.Body.Id);
                                        idsOfHandledBodies.Add(bodyState2.Body.Id);
                                        break;
                                    }
                                case OutcomeOfCollisionBetweenTwoBodies.ElasticCollision:
                                    {
                                        propagatedBodyStateMap[bodyState1].HandleElasticCollision(propagatedBodyStateMap[bodyState2]);
                                        state = new State(propagatedBodyStateMap.Values.ToList());
                                        idsOfHandledBodies.Add(bodyState1.Body.Id);
                                        idsOfHandledBodies.Add(bodyState2.Body.Id);
                                        break;
                                    }
                                case OutcomeOfCollisionBetweenTwoBodies.Ignore:
                                {
                                    state = new State(propagatedBodyStateMap.Values.ToList());
                                    handledCollisions.Add(new Tuple<int, int>(
                                        Math.Min(bodyState1.Body.Id, bodyState2.Body.Id),
                                        Math.Max(bodyState1.Body.Id, bodyState2.Body.Id)));

                                    //idsOfHandledBodies.Add(bodyState1.Body.Id);
                                    //idsOfHandledBodies.Add(bodyState2.Body.Id);
                                    break;
                                }
                                default:
                                    throw new ArgumentException();
                            }
                        }
                        else
                        {
                            // The scene doesn't have an opinion, but it actually should
                            throw new InvalidOperationException("No callback for handling collision between two bodies was provided by the scene");
                        }

                        timeElapsed = timeUntilCollisionBetweenBodies;
                        timeLeftInCurrentIncrement -= timeElapsed;
                        logger?.WriteLine(LogMessageCategory.Debug, $"  Body{bodyState1.Body.Id} and Body{bodyState2.Body.Id} collided after {timeUntilCollisionBetweenBodies} seconds. Time Left: {timeLeftInCurrentIncrement} seconds", "propagation");
                        logger?.WriteLine(LogMessageCategory.Debug, $"  Iteration {iteration} progress: {100 * (deltaT - timeLeftInCurrentIncrement) / deltaT:F5}%", "propagation");

                        if (timeUntilCollisionBetweenBodies < -0.001)
                        {
                            //var a = 0;
                        }

                        //LogState(state, logger);
                        break;
                    default:
                        throw new ArgumentException("Invalid state event");
                }

                if (timeElapsed > 1e-8)
                {
                    idsOfHandledBodies.Clear();
                }

                iteration++;
            }

            state.Index = indexOfInputState + 1;

            return state;
        }

        public static void LogState(
            State state,
            ILogger logger)
        {
            if (logger == null)
            {
                return;
            }

            logger.WriteLine(LogMessageCategory.Debug, $"      Energy: {state.CalculateTotalEnergy(10.0)}", "propagation");
            logger.WriteLine(LogMessageCategory.Debug, $"      Bodies:", "propagation");

            state.BodyStates.ForEach(bs =>
            {
                logger.WriteLine(LogMessageCategory.Debug, $"        Body{bs.Body.Id}: Position: ({bs.Position.X}, {bs.Position.Y}, Natural Velocity: ({bs.NaturalVelocity.X}, {bs.NaturalVelocity.Y}))", "propagation");
            });
        }

        // Tager en tilstand som input og beregner den tilstand, der gør sig gældende på et senere tidspunkt, givet de positioner, hastigheder og kræfter, der gælder.
        // Bemærk, at metoden ikke tager højde for boundaries af nogen art
        // Bemærk også, at bodies, der er markeret som værende "håndteret", hvilket indebærer, at de allerede har fået tildelt en position og hastighed, bibeholder deres position og hastighed
        // Key er den propagerede, og value er den oprindelige
        private static Dictionary<BodyState, BodyState> CalculatePropagatedBodyStateMap(
            State state,
            double standardGravity,
            double gravitationalConstant,
            double coefficientOfFriction,
            bool includeCustomForces,
            double timeLeftInCurrentIncrement,
            HashSet<int> idsOfHandledBodies)
        {
            var forceMap =
                state.BodyStates.ToDictionary(bs => bs, bs => new Vector2D(0, 0));

            if (Math.Abs(coefficientOfFriction) > 0.0001)
            {
                state.BodyStates.ForEach(bs =>
                {
                    if (Math.Abs(bs.NaturalVelocity.Length) < 0.00001) return;

                    var force = bs.Body.Mass * coefficientOfFriction;

                    var acceleration = force / bs.Body.Mass;
                    var speed = bs.NaturalVelocity.Length;
                    var nextSpeed = speed - timeLeftInCurrentIncrement * acceleration;

                    // Ensure that the frictional force wont cause the body to reverse its direction of movement
                    if (nextSpeed < 0.0) return;

                    var forceDirection = -bs.NaturalVelocity.Normalize();

                    forceMap[bs] += force * forceDirection;
                });
            }

            if (Math.Abs(standardGravity) > 0.0001)
            {
                var forceDirection = new Vector2D(0, 1);

                state.BodyStates.ForEach(bs =>
                {
                    if (bs.Body.AffectedByGravity)
                    {
                        forceMap[bs] += bs.Body.Mass * standardGravity * forceDirection;
                    }
                });
            }

            if (Math.Abs(gravitationalConstant) > 1E-15)
            {
                var innerLoopSkipCount = 1;

                state.BodyStates.ForEach(bs1 =>
                {
                    state.BodyStates.Skip(innerLoopSkipCount).ToList().ForEach(bs2 =>
                    {
                        var body1 = bs1.Body;
                        var body2 = bs2.Body;
                        var bodyState1 = bs1;
                        var bodyState2 = bs2;

                        var vectorFrom1To2 = bodyState2.Position - bodyState1.Position;
                        var distance = vectorFrom1To2.Length;

                        if (Math.Abs(distance) < 0.0001)
                        {
                            // Gravitational force would become ridiculously large
                            return;
                        }

                        vectorFrom1To2 = vectorFrom1To2.Normalize();

                        var force = gravitationalConstant * body1.Mass * body2.Mass * vectorFrom1To2 / Math.Pow(distance, 2);

                        forceMap[bodyState1] += force;
                        forceMap[bodyState2] -= force;
                    });

                    innerLoopSkipCount++;
                });
            }

            if (includeCustomForces)
            {
                state.BodyStates.ForEach(bs =>
                {
                    var bsc = bs as BodyStateClassic;

                    if (bsc == null)
                    {
                        return;
                    }

                    forceMap[bs] += bs.Body.Mass * bsc.EffectiveCustomForce;
                });
            }

            return state.BodyStates.ToDictionary(
                _ => idsOfHandledBodies.Contains(_.Body.Id)
                    ? _.Clone()
                    : _.Propagate(timeLeftInCurrentIncrement, forceMap[_]),
                _ => _.Clone());
        }

        private static void IdentifyFirstCollisionBetweenTwoBodies(
            Dictionary<BodyState, BodyState> propagatedBodyStateMap,
            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback,
            HashSet<int> idsOfHandledBodies,
            HashSet<Tuple<int, int>> handledBodyCollisions,
            double timeLeftInCurrentIncrement,
            out BodyState bodyState1,
            out BodyState bodyState2,
            out double timeUntilCollision)
        {
            BodyState bodyState1InvolvedInCollision = null;
            BodyState bodyState2InvolvedInCollision = null;
            var timeSinceFirstCollision = double.NaN;
            var innerLoopSkipCount = 1;

            foreach (var kvp1 in propagatedBodyStateMap)
            {
                var bs1Before = kvp1.Value;
                var bs1After = kvp1.Key;
                var body1 = bs1Before.Body;

                if (idsOfHandledBodies.Contains(body1.Id))
                {
                    continue;
                }

                foreach (var kvp2 in propagatedBodyStateMap.Skip(innerLoopSkipCount++))
                {
                    var bs2Before = kvp2.Value;
                    var bs2After = kvp2.Key;
                    var body2 = bs2Before.Body;

                    if (idsOfHandledBodies.Contains(body2.Id))
                    {
                        continue;
                    }

                    if (checkForCollisionBetweenBodiesCallback != null)
                    {
                        if (!checkForCollisionBetweenBodiesCallback.Invoke(body1, body2))
                        {
                            continue;
                        }
                    }

                    if (handledBodyCollisions.Any())
                    {
                        if (handledBodyCollisions.Contains(new Tuple<int, int>(
                            Math.Min(body1.Id, body2.Id), 
                            Math.Max(body1.Id, body2.Id))))
                        {
                            continue;
                        }
                    }

                    if (!(body1 is CircularBody) ||
                        !(body2 is CircularBody))
                    {
                        throw new NotImplementedException();
                    }

                    var radius1 = (body1 as CircularBody).Radius;
                    var radius2 = (body2 as CircularBody).Radius;

                    var vectorFrom1To2After = bs2After.Position - bs1After.Position;
                    var distanceAfter = vectorFrom1To2After.Length;
                    var radiusSum = radius1 + radius2;

                    if (radiusSum < distanceAfter)
                    {
                        // no collision
                        continue;
                    }

                    var vectorFrom1To2Before = bs2Before.Position - bs1Before.Position;
                    var distanceBefore = vectorFrom1To2Before.Length;

                    if (radiusSum >= distanceBefore)
                    {
                        // The bodies already intersected to begin with
                        // Therefore we dont regard the intersection after propagation as a collision
                        // Denne konstruktion gør, at f.eks. shoot 'em up 7 virker (der mister man energi, når man kolliderer med en enemy)
                        // men det fucker f.eks. scenerne med Newtons Cradle op..
                        // Måske skal det være scene-specifikt, om den skal gøre dette eller ej..
                        continue;
                    }

                    // There is a collision
                    var p1 = bs1After.Position;
                    var p2 = bs2After.Position;
                    var v1 = bs1Before.NaturalVelocity;
                    var v2 = bs2Before.NaturalVelocity;

                    // Determine the time that has passed since the two bodies touched, according to their original velocities

                    // Du har i vanlig stil ikke prioriteret at dokumentere, hvorledes du beregner det, men det er noget med at
                    // du opstiller en funktion, som er et andengradspolynomium, og som beskriver afstanden mellem de 2 bodies
                    // som funktion af tiden, og så identificerer du det tidspunkt, hvor afstanden er 0.
                    // Noget tyder på, at du ikke gør det rigtigt, og endda på, at det skyldes, at du ikke finder de rigtige nulpunkter
                    // for polynomiet!

                    var dp = p1 - p2;
                    var dv = v1 - v2;

                    var A = dv.X * dv.X + dv.Y * dv.Y;
                    var B = 2 * dp.X * dv.X + 2 * dp.Y * dv.Y;
                    var C = dp.X * dp.X + dp.Y * dp.Y - Math.Pow(radius1 + radius2, 2);
                    var discriminant = B * B - 4 * A * C;

                    if (Math.Abs(A) < 0.00001 || discriminant < 0.00001)
                    {
                        // no collision (such as if the 2 bodies have identical velocities)
                        continue;
                    }

                    var t = -(-B - Math.Sqrt(discriminant)) / (2 * A);

                    var t1 = -B + Math.Sqrt(discriminant) / (2 * A);
                    var t2 = -B - Math.Sqrt(discriminant) / (2 * A);

                    if (t < 0)
                    {
                        //var a = 0;
                    }

                    if (t > timeLeftInCurrentIncrement)
                    {
                        //var a = 0;
                    }

                    if (!double.IsNaN(timeSinceFirstCollision) &&
                        !(t > timeSinceFirstCollision)) continue;

                    bodyState1InvolvedInCollision = bs1After;
                    bodyState2InvolvedInCollision = bs2After;
                    timeSinceFirstCollision = t;
                }
            }

            bodyState1 = bodyState1InvolvedInCollision;
            bodyState2 = bodyState2InvolvedInCollision;

            timeUntilCollision = double.IsNaN(timeSinceFirstCollision)
                ? double.NaN
                : timeLeftInCurrentIncrement - timeSinceFirstCollision;

            if (timeUntilCollision < -0.000001)
            {
                //var a = 0;
            }
        }

        private static void IdentifyFirstCollisionWithABoundary(
            Dictionary<BodyState, BodyState> propagatedBodyStateMap,
            List<IBoundary> boundaries,
            double timeLeftInCurrentIncrement,
            out BodyState bodyStateInvolvedInCollision,
            out IBoundary boundaryInvolvedInCollision,
            out double timeUntilCollision,
            out Vector2D lineSegmentEndPointInvolvedInCollision,
            out Vector2D effectiveSurfaceNormalForBoundary)
        {
            effectiveSurfaceNormalForBoundary = null;

            bodyStateInvolvedInCollision = null;
            boundaryInvolvedInCollision = null;
            lineSegmentEndPointInvolvedInCollision = null;
            var timeSinceFirstCollisionWithBoundary = double.NaN;

            foreach (var kvp in propagatedBodyStateMap)
            {
                var bsBefore = kvp.Value;
                var bsAfter = kvp.Key;

                var effectiveVelocity = (bsAfter.Position - bsBefore.Position) / timeLeftInCurrentIncrement;

                foreach (var boundary in boundaries)
                {
                    if (!boundary.Intersects(bsAfter))
                    {
                        continue;
                    }

                    var buffer = 0.000001; // Backtrack an additional micro meter to ensure we don't have intersection due to rounding errors
                    Vector2D effectiveSurfaceNormalForCurrentBoundary = null;

                    if (boundary is ILineSegment)
                    {
                        var lineSegment = boundary as ILineSegment;

                        var velocityComponentTowardsBoundary = Math.Abs(lineSegment.ProjectVectorOntoSurfaceNormal(effectiveVelocity));

                        var t = double.NaN;
                        Vector2D lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = null;

                        // Hvis denne evaluerer til true er kuglens hastighed PARALLEL med væggen,
                        // Gør det her egentlig overhovedet nogen forskel?
                        // well det sikrer i hvert fald imod division med 0
                        if (velocityComponentTowardsBoundary <= 0)
                        {
                            // Bodyens hastighed er parallel med væggen, hvilket er ensbetydende med at 
                            // den har ramt en af liniestykkets ender
                            lineSegmentEndPointInvolvedInCollisionForCurrentBoundary =
                                lineSegment.IsVectorPointingInSameDirectionAsLineSegmentVector(
                                    bsBefore.NaturalVelocity)
                                    ? lineSegment.Point1
                                    : lineSegment.Point2;
                        }
                        else
                        {
                            // Hvis vi er her, har vi at gøre med det generelle tilfælde, hvor kuglens hastighed IKKE parallel med liniestykket,
                            // men vi ved endnu ikke, om vi har ramt liniestykkets SIDE eller en af dens 2 ENDER.
                            // Det finder vi ud af ved at føre kuglen tilbage langs dens hastighedsvektor indtil den tangerer
                            // den linie, som definerer liniestykket.

                            // Backtrack the body to where the line that defines the line segment
                            // is a tangent to the circle
                            // Todo: Undersøg om det kan gøres polymorfisk frem for at bruge en switch case ladder
                            switch (bsAfter.Body)
                            {
                                case CircularBody body:
                                    {
                                        var vPointOnLineToBodyCenter = bsAfter.Position - lineSegment.Point1;
                                        var distanceFromBodyCenterToLineForLineSegment = Math.Abs(Vector2D.DotProduct(lineSegment.SurfaceNormal, vPointOnLineToBodyCenter));
                                        t = (body.Radius + buffer - distanceFromBodyCenterToLineForLineSegment) / velocityComponentTowardsBoundary;

                                        // Nu regner vi så lige ud, hvor kuglens centrum ville være, hvis vi førte den tilbage med dette t
                                        var backtrackedPosition = bsAfter.Position - effectiveVelocity * t;

                                        // Nu skal vi så finde ud af, om dette punkt er tættest på liniestykket eller et af dens endepunkter
                                        var lineSegmentPart = lineSegment.ClosestPartOfLineSegment(backtrackedPosition);

                                        switch (lineSegmentPart)
                                        {
                                            case LineSegmentPart.Point1:
                                                lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point1; ;
                                                break;
                                            case LineSegmentPart.Point2:
                                                lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point2; ;
                                                break;
                                            case LineSegmentPart.MiddleSection:
                                                effectiveSurfaceNormalForCurrentBoundary = Vector2D.DotProduct(effectiveVelocity, lineSegment.SurfaceNormal) < 0
                                                    ? lineSegment.SurfaceNormal
                                                    : -lineSegment.SurfaceNormal;
                                                break;
                                        }
                                        break;
                                    }
                                case RectangularBody body:
                                    {
                                        // Lige som for en cirkulær body skal vi her føre rektanglet tilbage indtil det tangerer den linie, der definerer liniestykket.
                                        // Hvis der efterfølgende gælder, at den stadig rører liniestykket, så har den ramt SIDEN af liniestykket, og ellers har den
                                        // ramt en af liniestykkets endepunkter.

                                        // Hvad med at lave en generel metode for BodyState, der beregner dens position efter tilbageføring langs hastighedsvektoren
                                        // Så kan du også gøre det polymorfisk i stedet for at bruge en switch case ladder
                                        var overshootDistance = lineSegment.CalculateOvershootDistance(bsAfter);
                                        t = (overshootDistance + buffer) / velocityComponentTowardsBoundary;

                                        var backtrackedPosition = bsAfter.Position - effectiveVelocity * t;

                                        switch (lineSegment)
                                        {
                                            case HorizontalLineSegment horizontalLineSegment:
                                                {
                                                    var x0 = backtrackedPosition.X - body.Width / 2;
                                                    var x1 = backtrackedPosition.X + body.Width / 2;

                                                    if (x1 < horizontalLineSegment.X0)
                                                    {
                                                        lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point1;
                                                        effectiveSurfaceNormalForCurrentBoundary = new Vector2D(-1, 0);
                                                    }
                                                    else if (x0 > horizontalLineSegment.X1)
                                                    {
                                                        lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point2;
                                                        effectiveSurfaceNormalForCurrentBoundary = new Vector2D(1, 0);
                                                    }
                                                    else
                                                    {
                                                        effectiveSurfaceNormalForCurrentBoundary = bsBefore.Velocity.Y > 0
                                                            ? new Vector2D(0, -1)
                                                            : new Vector2D(0, 1);
                                                    }

                                                    break;
                                                }
                                            case VerticalLineSegment verticalLineSegment:
                                                {
                                                    var y0 = backtrackedPosition.Y - body.Height / 2;
                                                    var y1 = backtrackedPosition.Y + body.Height / 2;

                                                    if (y1 < verticalLineSegment.Y0)
                                                    {
                                                        lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point1;
                                                        effectiveSurfaceNormalForCurrentBoundary = new Vector2D(0, -1);
                                                    }
                                                    else if (y0 > verticalLineSegment.Y1)
                                                    {
                                                        lineSegmentEndPointInvolvedInCollisionForCurrentBoundary = lineSegment.Point2;
                                                        effectiveSurfaceNormalForCurrentBoundary = new Vector2D(0, 1);
                                                    }
                                                    else
                                                    {
                                                        effectiveSurfaceNormalForCurrentBoundary = bsBefore.Velocity.X > 0
                                                            ? new Vector2D(-1, 0)
                                                            : new Vector2D(1, 0);
                                                    }

                                                    break;
                                                }
                                            default:
                                                {
                                                    throw new InvalidOperationException("Unable to handle collision");
                                                }
                                        }

                                        break;
                                    }
                            }
                        }

                        if (lineSegmentEndPointInvolvedInCollisionForCurrentBoundary != null)
                        {
                            // Hvis vi er her, har kuglen ramt en af liniestykkets ender, evt med en hastighedsvektor parallel med liniestykket.
                            // Et evt beregnet t i blokken ovenfor kan ikke bruges, så vi skal beregne t her

                            // Todo: Undersøg om det kan gøres polymorfisk frem for at bruge en switch case ladder
                            switch (bsAfter.Body)
                            {
                                case CircularBody body:
                                    {
                                        t = CalculateTimeSinceIntersection(bsAfter.Position, body.Radius,
                                            lineSegmentEndPointInvolvedInCollisionForCurrentBoundary,
                                            effectiveVelocity, buffer, out effectiveSurfaceNormalForCurrentBoundary);

                                        break;
                                    }
                                case RectangularBody body:
                                    {
                                        // Du skal vist ca gøre det samme som når du regner på kollision mellem rectangular body og punkt

                                        var x = lineSegmentEndPointInvolvedInCollisionForCurrentBoundary.X;
                                        var y = lineSegmentEndPointInvolvedInCollisionForCurrentBoundary.Y;
                                        var x0 = bsAfter.Position.X - body.Width / 2;
                                        var x1 = bsAfter.Position.X + body.Width / 2;
                                        var y0 = bsAfter.Position.Y - body.Height / 2;
                                        var y1 = bsAfter.Position.Y + body.Height / 2;

                                        // Hvor lang tid siden er det at punktet intersektede med en af de lodrette akser?
                                        var vx = bsBefore.Velocity.X;
                                        var vy = bsBefore.Velocity.Y;

                                        var tx = double.MaxValue;
                                        var ty = double.MaxValue;

                                        // Beregn også den normalvektoren for boundaryen, der gør sig gældende for kollisionen
                                        // sikr, at tx ikke bliver negativ
                                        if (vx > 0)
                                        {
                                            tx = (x1 - x + buffer) / vx;
                                        }
                                        else if (vx < 0)
                                        {
                                            tx = (x0 - x - buffer) / vx;
                                        }

                                        if (vy > 0)
                                        {
                                            ty = (y1 - y + buffer) / vy;
                                        }
                                        else if (vy < 0)
                                        {
                                            ty = (y0 - y - buffer) / vy;
                                        }

                                        effectiveSurfaceNormalForCurrentBoundary = ty < tx
                                            ? vy > 0 ? new Vector2D(0, -1) : new Vector2D(0, 1)
                                            : vx > 0 ? new Vector2D(-1, 0) : new Vector2D(1, 0);

                                        t = ty < tx ? ty : tx;
                                        break;
                                    }
                            }
                        }

                        if (!double.IsNaN(timeSinceFirstCollisionWithBoundary) &&
                            !(t > timeSinceFirstCollisionWithBoundary)) continue;

                        // The collision happens earlier than any other collision identified so far,
                        // so we update the output parameters
                        bodyStateInvolvedInCollision = bsAfter;
                        boundaryInvolvedInCollision = boundary;
                        timeSinceFirstCollisionWithBoundary = t;
                        lineSegmentEndPointInvolvedInCollision = lineSegmentEndPointInvolvedInCollisionForCurrentBoundary;
                        effectiveSurfaceNormalForBoundary = effectiveSurfaceNormalForCurrentBoundary;
                    }
                    else if (boundary is IHalfPlane)
                    {
                        // Denne blok håndterer både circular bodies og rectangular bodies
                        var halfPlane = boundary as IHalfPlane;

                        // Dette er længden af hastighedsvektoren projiceret ind på væggens normalvektor.
                        // Den er i praksis negativ, så vi gør den positiv
                        // BEMÆRK: DET HER VIRKER NOK IKKE LÆNGERE, NÅR NU DU PROPAGERER MED ET GENNEMSNIT AF VELOCITY BEFORE OG VELOCITY AFTER
                        //var velocityComponentTowardsBoundary = -halfPlane.ProjectVectorOntoSurfaceNormal(velocityBefore);
                        var velocityComponentTowardsBoundary = Math.Abs(halfPlane.ProjectVectorOntoSurfaceNormal(effectiveVelocity));

                        // Hvis denne evaluerer til true er kuglens hastighed parallel med væggen,
                        // så den glider så at sige langs muren
                        // Gør det her egentlig overhovedet nogen forskel?
                        // well det sikrer i hvert fald imod division med 0
                        if (velocityComponentTowardsBoundary <= 0)
                        {
                            continue;
                        }

                        // Tiden er lig med den "dybde", som kuglen har opnået divideret med størrelsen
                        // af dens hastighed i retning af væggen
                        var t = (buffer - halfPlane.DistanceToBody(bsAfter)) / velocityComponentTowardsBoundary;

                        if (!double.IsNaN(timeSinceFirstCollisionWithBoundary) &&
                            !(t > timeSinceFirstCollisionWithBoundary)) continue;

                        // The collision happens earlier than any other collision identified so far,
                        // so we update the output parameters
                        bodyStateInvolvedInCollision = bsAfter;
                        boundaryInvolvedInCollision = boundary;
                        timeSinceFirstCollisionWithBoundary = t;
                        lineSegmentEndPointInvolvedInCollision = null;
                        effectiveSurfaceNormalForBoundary = halfPlane.SurfaceNormal;
                    }
                    else if (boundary is BoundaryPoint)
                    {
                        // For at finde afstanden, L mellem boundary punktet og cirklens overflade
                        // i cirklens bevægelsesretning, gør vi følgende:
                        // 1) Find afstand mellem body centrum og boundary punktet
                        // 2) Find vinklen theta1 med prikproduktreglen
                        // 3) Find vinklen theta2 med sinus-relationen
                        // 4) Find vinklen theta3 ved at udnytte, at summen af vinkler er 180 grader
                        // 5) Find den søgte afstanden med sinusrelationen

                        var boundaryPoint = boundary as BoundaryPoint;
                        double t;

                        switch (bsAfter.Body)
                        {
                            case CircularBody body:
                                {
                                    t = CalculateTimeSinceIntersection(
                                        bsAfter.Position, body.Radius, boundaryPoint.Point, effectiveVelocity, buffer, out effectiveSurfaceNormalForCurrentBoundary);

                                    break;
                                }
                            case RectangularBody body:
                                {
                                    var x = boundaryPoint.Point.X;
                                    var y = boundaryPoint.Point.Y;
                                    var x0 = bsAfter.Position.X - body.Width / 2;
                                    var x1 = bsAfter.Position.X + body.Width / 2;
                                    var y0 = bsAfter.Position.Y - body.Height / 2;
                                    var y1 = bsAfter.Position.Y + body.Height / 2;

                                    // Hvor lang tid siden er det at punktet intersektede med en af de lodrette akser?
                                    var vx = bsBefore.NaturalVelocity.X;
                                    var vy = bsBefore.NaturalVelocity.Y;

                                    var tx = double.MaxValue;
                                    var ty = double.MaxValue;

                                    // Beregn også den normalvektoren for boundaryen, der gør sig gældende for kollisionen
                                    if (vx > 0)
                                    {
                                        tx = (x1 - x + buffer) / vx;
                                    }
                                    else if (vx < 0)
                                    {
                                        tx = (x0 - x - buffer) / vx;
                                    }

                                    if (vy > 0)
                                    {
                                        ty = (y1 - y + buffer) / vy;
                                    }
                                    else if (vy < 0)
                                    {
                                        ty = (y0 - y - buffer) / vy;
                                    }

                                    effectiveSurfaceNormalForCurrentBoundary = ty < tx
                                        ? vy > 0 ? new Vector2D(0, -1) : new Vector2D(0, 1)
                                        : vx > 0 ? new Vector2D(-1, 0) : new Vector2D(1, 0);

                                    t = ty < tx ? ty : tx;

                                    break;
                                }
                            default:
                                throw new ArgumentException();
                        }

                        if (!double.IsNaN(timeSinceFirstCollisionWithBoundary) &&
                            !(t > timeSinceFirstCollisionWithBoundary)) continue;

                        // The collision happens earlier than any other collision identified so far,
                        // so we update the output parameters
                        bodyStateInvolvedInCollision = bsAfter;
                        boundaryInvolvedInCollision = boundary;
                        timeSinceFirstCollisionWithBoundary = t;
                        lineSegmentEndPointInvolvedInCollision = null;
                        effectiveSurfaceNormalForBoundary = effectiveSurfaceNormalForCurrentBoundary;
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
            }

            timeUntilCollision = double.IsNaN(timeSinceFirstCollisionWithBoundary)
                ? double.NaN
                : Math.Max(0.0, timeLeftInCurrentIncrement - timeSinceFirstCollisionWithBoundary);
        }

        // Husk: KEY har den PROPAGEREDE tilstand for hver body, og VALUE-felterne har den OPRINDELIGE 
        private static void PropagateStatePartly(
            Dictionary<BodyState, BodyState> propagatedBodyStateMap,
            double timeInterval,
            double timeLeftInCurrentIncrement)
        {
            var fraction = timeInterval / timeLeftInCurrentIncrement;

            foreach (var kvp in propagatedBodyStateMap)
            {
                kvp.Value.Position += fraction * (kvp.Key.Position - kvp.Value.Position);
                //kvp.Key.NaturalVelocity = kvp.Value.NaturalVelocity; // Hvad Fanden er rationalet for det her lige? SATANS OGSÅ, MAND!!
                // Du vurderede, at det var uhensigtsmæssigt at ændre på hastigheden, hvis den kun propagerede delvist..
                // men det lader jo altså til at have nogle implikationer, som du ikke er skide glad for..

                // For eksemplet med den hoppende bold vil jeg altså mene, at du må være bedst tjent med at håndtere hastighed ligesom position
                // Så må du tage hånd om evt problemer på en anden måde
                kvp.Value.NaturalVelocity += fraction * (kvp.Key.NaturalVelocity - kvp.Value.NaturalVelocity);
            }
        }

        private static double CalculateTimeSinceIntersection(
            Vector2D circleCenter,
            double radius,
            Vector2D point,
            Vector2D circleVelocity,
            double buffer,
            out Vector2D effectiveSurfaceNormal)
        {
            var speed = circleVelocity.Length;

            if (speed < 0.000001)
            {
                effectiveSurfaceNormal = null;
                return 0;
            }

            var vectorFromBoundaryPointToBodyCenter = circleCenter - point;
            var distance = vectorFromBoundaryPointToBodyCenter.Length;
            var normalizedCircleVelocity = circleVelocity.Normalize(); 

            double overshootDistance;

            if (distance < 0.000001)
            {
                overshootDistance = radius;
                effectiveSurfaceNormal = -normalizedCircleVelocity;
            }
            else
            {
                var theta1 = Math.Acos(Vector2D.DotProduct(normalizedCircleVelocity, vectorFromBoundaryPointToBodyCenter) / distance);
                var theta2 = Math.Asin(distance * Math.Sin(theta1) / radius);
                var theta3 = Math.PI - theta1 - theta2;
                overshootDistance = Math.Sqrt(distance * distance + radius * radius - 2 * distance * radius * Math.Cos(theta3));

                effectiveSurfaceNormal = (circleCenter - (point + overshootDistance * normalizedCircleVelocity)).Normalize();
            }

            overshootDistance += buffer;

            return overshootDistance / speed;
        }
    }
}
