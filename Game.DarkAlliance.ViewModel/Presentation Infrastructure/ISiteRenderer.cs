using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public interface ISiteRenderer
{
    Model3D Build(SiteSpecs siteSpecs);
}