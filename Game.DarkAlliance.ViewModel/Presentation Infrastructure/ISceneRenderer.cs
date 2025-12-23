using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public interface ISceneRenderer
{
    Model3D Build(SceneDefinition sceneDefinition);
}