using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Simulator.Domain;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;
using Simulator.Domain.Props;
using LineSegment = Simulator.Domain.Boundaries.LineSegment;

namespace Simulator.ViewModel
{
    public delegate ShapeViewModel ShapeSelectorCallback(
        BodyState bodyState);

    public delegate void ShapeUpdateCallback(
        ShapeViewModel shapeViewModel,
        BodyState bodyState);

    // Denne klasse observerer current scene udstillet af application laget og vedligeholder GeometryEditorViewModel
    public class SceneViewManager
    {
        private Application.Application _application;
        private Scene _activeScene;
        private GeometryEditorViewModel _geometryEditorViewModel;
        private int[] _propIds;
        private Brush brush;

        public Scene ActiveScene
        {
            get => _activeScene;
            set
            {
                _activeScene = value;
                _application.Engine.Scene = value;

                Reset(); // Dette gør, at:
                         // - Enginen resettes
                         // - GeometryEditoren cleares for alt, hvad den indeholder af shapes, lines osv.

                if (value == null) return;

                PrepareAnimation(); // Dette gør, at:
                                    // - Engingen sættes til at producere states ud fra den initielle tilstand i den nye scene
                                    // - GeometryEditoren populeres med shapes, lines osv fra den nye scene
                                    // - World Window for GeometryEditoren sættes som anvist af den nye scene

                // Du kunne jo prøve at skille det ad, så f.eks. det med at placere world window foregår separat.
                // Du kunne også bare sige, at det at flytte WorldWindow så at sige er en del af animationen - den må bare ikke begynde
                // at consume states før vinduet er placeret. Faktisk så må den ikke begynde StopWatch, før vinduet er placeret

                // Man kunne også lade SceneViewManager publicere et event om at animationen er klargjort, hvilket så også gerne skunne indebære,
                // at World Window er placeret..

                // Den skal fortælle GeometryEditoren, at den skal slide hen til en given placering, og når den så er der, skal den rejse et event over for
                // MainWindowViewModel om at den er klar. Tænk lige over arbejdsfordeling. Det er nok hensigtsmæssigt, hvis sliding ligger EKSTERNT i
                // forhold til GeometryEditorViewModel (senere kan du måske overveje at bygge det ind - eller i hvert fald bygge det ind i en genbrugelig
                // komponent)
            }
        }

        public ShapeSelectorCallback ShapeSelectorCallback { get; set; }

        public ShapeUpdateCallback ShapeUpdateCallback { get; set; }

        public SceneViewManager(
            Application.Application application,
            GeometryEditorViewModel geometryEditorViewModel,
            ShapeSelectorCallback shapeSelectorCallback = null,
            ShapeUpdateCallback shapeUpdateCallback = null)
        {
            _application = application;
            _geometryEditorViewModel = geometryEditorViewModel;

            _application.CurrentStateChangedCallback = UpdateScene;

            ShapeSelectorCallback = shapeSelectorCallback;
            ShapeUpdateCallback = shapeUpdateCallback;

            if (ShapeSelectorCallback == null)
            {
                SetShapeSelectorCallbackToDefault();
            }

            if (ShapeUpdateCallback == null)
            {
                SetShapeUpdateCallbackToDefault();
            }

            brush = new SolidColorBrush(Colors.Black);
        }

        public void SetShapeSelectorCallbackToDefault()
        {
            ShapeSelectorCallback = (bs) =>
            {
                switch (bs.Body)
                {
                    case RectangularBody body:
                        {
                            return new RectangleViewModel
                            {
                                Width = body.Width,
                                Height = body.Height
                            };
                        }
                    case CircularBody body:
                        {
                            return new EllipseViewModel
                            {
                                Width = 2 * body.Radius,
                                Height = 2 * body.Radius
                            };
                        }
                    default:
                        throw new ArgumentException("Unknown Body");
                }
            };
        }

        public void SetShapeUpdateCallbackToDefault()
        {
            ShapeUpdateCallback = (shapeViewModel, bs) =>
            {
                shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);
            };
        }

        public void ResetScene()
        {
            Reset();
            PrepareAnimation();

            var scene = _application.Engine.Scene;

            _geometryEditorViewModel.InitializeWorldWindow(
                scene.InitialWorldWindowFocus(),
                scene.InitialWorldWindowSize(),
                false);
        }

        private void PrepareAnimation()
        {
            var scene = _application.Engine.Scene;

            _geometryEditorViewModel.WorldWindowUpperLeftLimit = new Point(
                scene.WorldWindowUpperLeftLimit.X,
                scene.WorldWindowUpperLeftLimit.Y);

            _geometryEditorViewModel.WorldWindowBottomRightLimit = new Point(
                scene.WorldWindowBottomRightLimit.X,
                scene.WorldWindowBottomRightLimit.Y);

            // Vi vil helst ikke gøre det allerede her, da vi så ikke kan "slide"
            //_geometryEditorViewModel.InitializeWorldWindow(
            //    scene.InitialWorldWindowFocus(),
            //    scene.InitialWorldWindowSize(),
            //    false);

            var lineThickness = 0.01;

            _application.Engine.Scene.Boundaries.ForEach(b =>
            {
                if (!b.Visible) return;

                // Todo: prøv at kør det polymorfisk i stedet
                switch (b)
                {
                    case HalfPlane halfPlane:
                        var v = halfPlane.SurfaceNormal.Hat();
                        _geometryEditorViewModel.AddLine(
                            new PointD(halfPlane.Point.X - 500 * v.X, halfPlane.Point.Y - 500 * v.Y),
                            new PointD(halfPlane.Point.X + 500 * v.X, halfPlane.Point.Y + 500 * v.Y),
                            lineThickness,
                            brush);
                        break;
                    case LeftFacingHalfPlane halfPlane:
                        _geometryEditorViewModel.AddLine(
                            new PointD(halfPlane.X, -500),
                            new PointD(halfPlane.X, 500),
                            lineThickness,
                            brush);
                        break;
                    case RightFacingHalfPlane halfPlane:
                        _geometryEditorViewModel.AddLine(
                            new PointD(halfPlane.X, -500),
                            new PointD(halfPlane.X, 500),
                            lineThickness,
                            brush);
                        break;
                    case UpFacingHalfPlane halfPlane:
                        _geometryEditorViewModel.AddLine(
                            new PointD(-500, halfPlane.Y),
                            new PointD(500, halfPlane.Y),
                            lineThickness,
                            brush);
                        break;
                    case DownFacingHalfPlane halfPlane:
                        _geometryEditorViewModel.AddLine(
                            new PointD(-500, halfPlane.Y),
                            new PointD(500, halfPlane.Y),
                            lineThickness,
                            brush);
                        break;
                    case LineSegment lineSegment:
                        _geometryEditorViewModel.AddLine(
                            lineSegment.Point1.AsPointD(),
                            lineSegment.Point2.AsPointD(),
                            lineThickness,
                            brush);
                        break;
                    case VerticalLineSegment lineSegment:
                        _geometryEditorViewModel.AddLine(
                            lineSegment.Point1.AsPointD(),
                            lineSegment.Point2.AsPointD(),
                            lineThickness,
                            brush);
                        break;
                    case HorizontalLineSegment lineSegment:
                        _geometryEditorViewModel.AddLine(
                            lineSegment.Point1.AsPointD(),
                            lineSegment.Point2.AsPointD(),
                            lineThickness,
                            brush);
                        break;
                    case BoundaryPoint boundaryPoint:
                        _geometryEditorViewModel.AddPoint(boundaryPoint.Point.AsPointD(), 3, new SolidColorBrush(Colors.Black));
                        break;
                    default:
                        throw new ArgumentException();
                }
            });

            _application.Engine.Scene.Props.ForEach(p =>
            {
                switch (p)
                {
                    case PropRectangle propRectangle:
                    {
                        _geometryEditorViewModel.AddShape(p.Id, new RectangleViewModel
                        {
                            Width = propRectangle.Width,
                            Height = propRectangle.Height,
                            Point = propRectangle.Position.AsPointD()
                        });

                        break;
                    }
                    case PropRotatableRectangle propRotatableRectangle:
                        {
                            _geometryEditorViewModel.AddShape(p.Id, new RotatableRectangleViewModel
                            {
                                Width = propRotatableRectangle.Width,
                                Height = propRotatableRectangle.Height,
                                Point = propRotatableRectangle.Position.AsPointD(),
                                Orientation = propRotatableRectangle.Orientation
                            });

                            break;
                        }
                    case PropCircle propCircle:
                    {
                        _geometryEditorViewModel.AddShape(p.Id, new EllipseViewModel
                        {
                            Width = propCircle.Diameter,
                            Height = propCircle.Diameter,
                            Point = propCircle.Position.AsPointD()
                        });

                        break;
                    }
                    default:
                    {
                        throw new ArgumentException();
                    }
                }
            });

            _propIds = _application.Engine.Scene.Props.Select(p => p.Id).ToArray();

            var initialState = _application.Engine.SpawnNewThread();
            //RepositionWorldWindowIfRequired(initialState); // Vær opmærksom på den her i arbejdet med at slide World Window
            UpdateCurrentState(initialState);
        }

        private void Reset()
        {
            _application.ResetEngine();
            ClearCurrentScene();
        }

        private void ClearCurrentScene()
        {
            _geometryEditorViewModel.ClearShapes();
            _geometryEditorViewModel.ClearPoints();
            _geometryEditorViewModel.ClearLines();
        }

        private void UpdateScene(
            State state)
        {
            RepositionWorldWindowIfRequired(state);
            UpdateCurrentState(state);
        }

        private void RepositionWorldWindowIfRequired(
            State state)
        {
            switch (_application.Engine.Scene.ViewMode)
            {
                case SceneViewMode.FocusOnCenterOfMass:
                    var centerOfMass = state.CenterOfMass();
                    if (centerOfMass != null)
                    {
                        _geometryEditorViewModel.SetFocusForWorldWindow(centerOfMass.AsPointD());
                    }
                    break;
                case SceneViewMode.FocusOnFirstBody:
                    var centerOfInitialBody = state.CenterOfInitialBody();
                    if (centerOfInitialBody != null)
                    {
                        _geometryEditorViewModel.SetFocusForWorldWindow(centerOfInitialBody.AsPointD());
                    }
                    break;
                case SceneViewMode.MaintainFocusInVicinityOfPoint:
                    var centerOfInitialBody2 = state.CenterOfInitialBody();
                    if (centerOfInitialBody2 != null)
                    {
                        _geometryEditorViewModel.AdjustWorldWindowSoPointLiesInCentralSquare(
                            centerOfInitialBody2.AsPointD(),
                            _application.Engine.Scene.MaxOffsetXFraction,
                            _application.Engine.Scene.MaxOffsetYFraction,
                            _application.Engine.Scene.CorrectionXFraction,
                            _application.Engine.Scene.CorrectionYFraction);
                    }
                    break;
                case SceneViewMode.Stationary:
                default:
                    break;
            }
        }

        private void UpdateCurrentState(
            State state)
        {
            // Identificer eventuelle bodies, der ikke længere er med
            _geometryEditorViewModel.AllShapeIds
                .Except(_propIds)
                .Except(state.BodyStates.Select(bs => bs.Body.Id))
                .ToList()
                .ForEach(id =>
                {
                    _geometryEditorViewModel.RemoveShape(id);
                });

            state.BodyStates.ForEach(bs =>
            {
                var shape = _geometryEditorViewModel.TryGetShape(bs.Body.Id);

                if (shape != null)
                {
                    ShapeUpdateCallback.Invoke(shape, bs);
                }
                else
                {
                    var shapeViewModel = ShapeSelectorCallback.Invoke(bs);
                    shapeViewModel.Point = bs.Position.AsPointD();

                    if (shapeViewModel is RotatableShapeViewModel)
                    {
                        var bsc = bs as BodyStateClassic;

                        if (bsc != null)
                        {
                            (shapeViewModel as RotatableShapeViewModel).Orientation = bsc.Orientation;
                        }
                    }

                    _geometryEditorViewModel.AddShape(bs.Body.Id, shapeViewModel);
                }
            });
        }
    }
}
