namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts
{
    public class NPC : RotatableScenePart
    {
        public string Tag { get; init; }

        public NPC(
            string modelId) : base(modelId)
        {
        }
    }
}
