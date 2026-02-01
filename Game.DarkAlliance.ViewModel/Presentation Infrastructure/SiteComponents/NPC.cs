namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents
{
    public class NPC : RotatableSiteComponent
    {
        public string Tag { get; init; }

        public NPC(
            string modelId) : base(modelId)
        {
        }
    }
}
