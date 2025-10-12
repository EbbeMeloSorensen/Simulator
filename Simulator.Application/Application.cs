using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Craft.Logging;
using Craft.Math;
using Craft.Utils;
using Craft.DataStructures.Graph;
using Simulator.Domain;
using ApplicationState = Craft.DataStructures.Graph.State;
using State = Simulator.Domain.State;

namespace Simulator.Application
{
    // Todo:
    // * Gør så meget som muligt private
    // * Lav så mange properties som muligt om til member variable

    public enum KeyboardKey
    {
        LeftArrow,
        RightArrow,
        UpArrow,
        DownArrow,
        Space
    }

    public enum KeyEventType
    {
        KeyPressed,
        KeyReleased
    }

    public delegate void CurrentStateChangedCallback(
        State currentState);

    public class Application
    {
        public const double MinTimeBetweenRefresh = 0.005; // 5 milliseconds
        private StateMachine _stateMachine;

        private ILogger _logger;

        public Engine Engine { get; }
        public Stopwatch Stopwatch { get; }
        public KeyboardState KeyboardState { get; }
        public KeyboardState KeyboardEvents { get; }
        public MouseClickPosition? MouseClickPosition { get; private set; }
        public int LastIndexRequested { get; private set; }
        public int LastIndexConsumed { get; private set; }
        public int IndexDifference { get; private set; }
        public int FrameSkipCount { get; private set; }
        public double TimeElapsedAtLastRefresh { get; private set; }
        public bool AnimationLaunched { get; private set; }
        public bool AnimationRunning { get; private set; }
        public bool AnimationComplete { get; private set; }

        public ObservableObject<ApplicationState> State { get; }
        public ApplicationState PreviousState { get; private set; }

        public CurrentStateChangedCallback CurrentStateChangedCallback { get; set; }

        public bool CanStartOrResumeAnimation => Engine.Scene != null && !AnimationRunning && !AnimationComplete;

        public bool CanPauseAnimation => Engine.Scene != null && AnimationRunning;

        public bool CanResetAnimation => AnimationLaunched;

        public event EventHandler<KeyEventArgs> KeyEventOccured;
        public event EventHandler AnimationCompleted;

        public Application(
            ILogger logger,
            ApplicationState initialState = null)
        {
            _logger = logger;

            KeyboardState = new KeyboardState();
            KeyboardEvents = new KeyboardState();

            Engine = new Engine(logger);
            Stopwatch = new Stopwatch();

            if (initialState != null)
            {
                _stateMachine = new StateMachine(initialState);
                State = new ObservableObject<ApplicationState>{Object = initialState};
            }
        }

        public void AddApplicationState(
            ApplicationState applicationState)
        {
            if (_stateMachine.Vertices.Any(_ => _.Name == applicationState.Name))
            {
                throw new InvalidOperationException("The name of the application state has to be unique");
            }

            _stateMachine.AddVertex(applicationState);
        }

        public void AddApplicationStateTransition(
            ApplicationState from,
            ApplicationState to)
        {
            _stateMachine.AddTransition(from, to);
        }

        public IEnumerable<string> ExitsFromCurrentApplicationState()
        {
            return _stateMachine.ExitsFromCurrentState();
        }

        public void HandleKeyEvent(
            KeyboardKey keyboardKey,
            KeyEventType keyEventType)
        {
            switch (keyboardKey)
            {
                case KeyboardKey.LeftArrow:
                    KeyboardState.LeftArrowDown = keyEventType == KeyEventType.KeyPressed;
                    KeyboardEvents.LeftArrowDown = true;
                    break;
                case KeyboardKey.RightArrow:
                    KeyboardState.RightArrowDown = keyEventType == KeyEventType.KeyPressed;
                    KeyboardEvents.RightArrowDown = true;
                    break;
                case KeyboardKey.UpArrow:
                    KeyboardState.UpArrowDown = keyEventType == KeyEventType.KeyPressed;
                    KeyboardEvents.UpArrowDown = true;
                    break;
                case KeyboardKey.DownArrow:
                    KeyboardState.DownArrowDown = keyEventType == KeyEventType.KeyPressed;
                    KeyboardEvents.DownArrowDown = true;
                    break;
                case KeyboardKey.Space:
                    KeyboardState.SpaceDown = keyEventType == KeyEventType.KeyPressed;
                    KeyboardEvents.SpaceDown = true;
                    break;
            }

            OnKeyEventOccured(keyboardKey, keyEventType);
        }

        public void HandleMouseClickEvent(
            Point2D position)
        {
            MouseClickPosition = new MouseClickPosition(position);
        }

        public void StartOrResumeAnimation()
        {
            MouseClickPosition = null;
            AnimationLaunched = true;
            AnimationRunning = true;
            Stopwatch.Start();
        }

        public void PauseAnimation()
        {
            AnimationRunning = false;
            Stopwatch.Stop();
        }

        public void HandleClosing()
        {
            Engine.Reset();
        }

        private void StopAnimation()
        {
            KeyboardState.Clear();
            KeyboardEvents.Clear();
            MouseClickPosition = null;

            AnimationRunning = false;
            AnimationComplete = true;

            Stopwatch.Stop();

            OnAnimationCompleted();
        }

        public void UpdateModel()
        {
            if (!AnimationRunning)
            {
                return;
            }

            if (Engine.Scene == null)
            {
                // Dette er en påmindelse om at man skal huske at give enginen en scene før man starter enginen
                throw new InvalidOperationException("Scene Required when refreshing view for a running animation");
            }

            if (Stopwatch.IsRunning)
            {
                // Try consuming a state from the queue of the engine

                LastIndexRequested = DetermineCurrentIndex(out var secondsElapsed);
                IndexDifference = LastIndexRequested - LastIndexConsumed;

                if (IndexDifference <= 0)
                {
                    return;
                }

                var timeElapsedSinceLastRefresh = secondsElapsed - TimeElapsedAtLastRefresh;

                if (timeElapsedSinceLastRefresh < MinTimeBetweenRefresh)
                {
                    return;
                }

                var currentState = Engine.TryGetState(
                    LastIndexRequested,
                    KeyboardState,
                    KeyboardEvents,
                    MouseClickPosition,
                    out var idsOfDisposedBodies);

                if (currentState != null)
                {
                    // Her skal vi gerne kunne modtage en eller anden besked fra den høstede tilstand,
                    // såsom at spillet er tabt eller vundet, at der skal skiftes til en anden scene,
                    // eller at der skal ske et eller andet specielt.
                    // Husk også på, at det skal være GENERELT, da Application jo deles mellem forskellige spil

                    CurrentStateChangedCallback?.Invoke(currentState);

                    LastIndexConsumed = currentState.Index;
                    TimeElapsedAtLastRefresh = secondsElapsed;

                    var message = $"  Main thread: Refreshed view using state {LastIndexConsumed}";

                    if (LastIndexConsumed < LastIndexRequested)
                    {
                        message += " (final state)";
                        Engine.Reset();
                        StopAnimation();
                    }

                    MouseClickPosition = null; // Lad nu ff være med at hacke sådan.. (det virker tilsyneladende, men det er ikke videre elegant)

                    _logger?.WriteLine(LogMessageCategory.Debug, message, "state_sequence");
                }
                else
                {
                    // Hvis vi lander her, så er det fordi beregneren ikke har kunnet følge med
                    Stopwatch.Stop();

                    FrameSkipCount++;
                    _logger?.WriteLine(LogMessageCategory.Debug, "  Main thread: State producer fell behind by ? indexes - pausing state consumption", "state_sequence");
                }
            }
            else
            {
                FrameSkipCount++;

                if (!Engine.IsLeadSufficientlyLargeForResumingAnimation(LastIndexRequested)) return;

                _logger?.WriteLine(LogMessageCategory.Debug, "  Main thread: Resuming state consumption", "state_sequence");
                Stopwatch.Start();
            }
        }

        public void ResetEngine()
        {
            Engine.Reset();
            Stopwatch.Reset();
            TimeElapsedAtLastRefresh = 0.0;
            AnimationLaunched = false;
            AnimationRunning = false;
            AnimationComplete = false;
            LastIndexRequested = 0;
            LastIndexConsumed = 0;
            IndexDifference = 0;
            FrameSkipCount = 0;
        }

        public void SwitchState(
            string name = null)
        {
            if (Engine.Scene != null)
            {
                Engine.PreviousScene = Engine.Scene.Name;
            }

            PreviousState = _stateMachine.CurrentState;

            _stateMachine.SwitchState(name);

            State.Object = _stateMachine.CurrentState;
        }

        private int DetermineCurrentIndex(
            out double secondsElapsed)
        {
            // Hvor mange sekunder er der gået siden vi satte gang i animationen
            secondsElapsed = 0.001 * Stopwatch.Elapsed.TotalMilliseconds;

            // Hvor lang tid svarer det til i den verden, der gælder for animationen?
            var secondsElapsedInScene = secondsElapsed * Engine.Scene.TimeFactor;

            // Det skal vi så omregne til et index, og det skal vi bruge deltaT for scenen til
            return (int)Math.Round(secondsElapsedInScene / Engine.Scene.DeltaT);
        }

        private void OnKeyEventOccured(
            KeyboardKey keyboardKey,
            KeyEventType keyEventType)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = KeyEventOccured;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                handler(this, new KeyEventArgs(keyboardKey, keyEventType));
            }
        }

        private void OnAnimationCompleted()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = AnimationCompleted;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
