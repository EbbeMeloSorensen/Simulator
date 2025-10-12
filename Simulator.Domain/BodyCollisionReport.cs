namespace Simulator.Domain
{
    public class BodyCollisionReport
    {
        public Body Body1 { get; }
        public Body Body2 { get; }

        public BodyCollisionReport(
            Body body1,
            Body body2)
        {
            Body1 = body1;
            Body2 = body2;
        }
    }
}