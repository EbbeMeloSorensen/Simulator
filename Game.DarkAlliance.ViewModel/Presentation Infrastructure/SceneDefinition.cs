using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure
{
    public class SceneDefinition
    {
        public IReadOnlyList<ScenePart> Parts { get; init; }

        public SceneDefinition()
        {
            // For a start, just hardcode a scene. Later, we will read this from some data source.

            var parts = new List<ScenePart>();
            
            parts.Add(new ScenePart("human male", new Vector3D(0.25, 0, 0)));
            parts.Add(new ScenePart("human female", new Vector3D(0, 0, 0)));

            Parts = new List<ScenePart>(parts);
        }
    }
}
