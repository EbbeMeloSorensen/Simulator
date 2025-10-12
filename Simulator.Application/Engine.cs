using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Craft.Logging;
using Simulator.Domain;

namespace Simulator.Application
{
    public class Engine
    {
        private abstract class PotentialEvent
        {
            public int StateIndex { get; protected set; } // Index Of state whose propagation lead to the event
        }

        private class AssignExpirationToAnimation : PotentialEvent
        {
            public int IndexOfFinalState { get; }

            public AssignExpirationToAnimation(
                int stateIndex,
                int indexOfFinalState)
            {
                StateIndex = stateIndex;
                IndexOfFinalState = indexOfFinalState;
            }
        }

        private class AssignOutcomeToAnimation : PotentialEvent
        {
            public string Outcome { get; }

            public AssignOutcomeToAnimation(
                int stateIndex,
                string outcome)
            {
                StateIndex = stateIndex;
                Outcome = outcome;
            }
        }

        private abstract class BodyEvent : PotentialEvent
        {
            public int BodyId { get; protected set; }
        }

        private class BoundaryCollision : BodyEvent
        {
            public List<BoundaryCollisionReport> Reports { get; }

            public BoundaryCollision(
                int stateIndex,
                List<BoundaryCollisionReport> reports)
            {
                StateIndex = stateIndex;
                Reports = reports;
            }
        }

        private const int QueueMaxSize = 100; // Denne skal losses ned i en dedikeret container
        private int _finalStateIndex;
        private Thread _pro;
        private volatile bool _exit;
        private List<State> _stateSequence; // Denne skal losses ned i en dedikeret container
        private int _stateSequenceCount; // Denne skal losses ned i en dedikeret container
        private int _disposedStates; // Denne skal losses ned i en dedikeret container
        private int _lastIndexGenerated; // Denne skal losses ned i en dedikeret container
        private readonly Mutex _stateSequenceMutex; // Denne skal losses ned i en dedikeret container
        private readonly AutoResetEvent _proWaitEvent;
        private Queue<PotentialEvent> _potentialEvents;  // Denne skal losses ned i en dedikeret container

        private ILogger _logger;

        public Scene Scene { get; set; }
        public string Outcome { get; set; }
        public string PreviousScene { get; set; }

        public Engine(
            ILogger logger)
        {
            _logger = logger;

            _stateSequence = new List<State>();
            _stateSequenceMutex = new Mutex();
            _proWaitEvent = new AutoResetEvent(false);
            _potentialEvents = new Queue<PotentialEvent>();
        }

        // This method is called by the View model when it is time to refresh the view. If the scene has a callback function for controlling the primary body of the scene,
        // then that callback function is invoked. If the callback returns a new velocity then the calculated states after the current one are invalidated and disposed of.
        public State TryGetState(
            int stateIndex,
            KeyboardState keyBoardState,
            KeyboardState keyBoardEvents,
            MouseClickPosition? mouseClickPosition,
            out List<int> idsOfDisposedBodies)
        {
            State result = null;
            idsOfDisposedBodies = null;

            _stateSequenceMutex.WaitOne();

            _logger?.WriteLine(LogMessageCategory.Debug, $"  Main thread: Requesting state with index {stateIndex}", "state_sequence");

            stateIndex = Math.Min(stateIndex, _finalStateIndex);
            var effectiveCurrentIndex = stateIndex - _disposedStates;
            var lead = _stateSequenceCount - 1 - effectiveCurrentIndex;

            if (effectiveCurrentIndex > 0)
            {
                DisposeOfHistoricStates(effectiveCurrentIndex);
            }

            if (lead >= 0)
            {
                // Fjern disposable bodies og noter deres id, så det kan kommunikeres tilbage til viewmodellen
                // (så den kan fjerne de tilsvarende grafiske repræsentationer)
                idsOfDisposedBodies = new List<int>();
                var collisions = new Dictionary<int, List<BoundaryCollisionReport>>();
                do
                {
                    // Her kigger vi på køen af special event reports, specielt på dem, hvis stateIndex er mindre end eller lig med index for current state.
                    // De hændelser, som disse rapporter beskriver, REALISERES her, hvilket jo kun sker, når de pågældende rapporter konsumeres. Indtil da er
                    // de rent POTENTIELLE, dvs de kan invalideres, hvis en brugeraktion nødvendiggør en genberegning
                    if (_potentialEvents.Count == 0) break;
                    var potentialEvent = _potentialEvents.Peek();
                    if (stateIndex <= potentialEvent.StateIndex) break;
                    _potentialEvents.Dequeue();

                    switch (potentialEvent)
                    {
                        //case BodyDisposal bodyDisposal:
                        //{
                        //    idsOfDisposedBodies.Add(bodyDisposal.BodyId);
                        //    break;
                        //}
                        case BoundaryCollision boundaryCollision:
                        {
                            if (!collisions.ContainsKey(boundaryCollision.BodyId))
                            {
                                collisions[boundaryCollision.BodyId] = new List<BoundaryCollisionReport>();
                            }

                            collisions[boundaryCollision.BodyId].AddRange(boundaryCollision.Reports);
                            break;
                        }
                        case AssignExpirationToAnimation assignExpirationToAnimation:
                        {
                            _finalStateIndex = Math.Min(_finalStateIndex, assignExpirationToAnimation.IndexOfFinalState);
                            break;
                        }
                        case AssignOutcomeToAnimation assignOutcomeToAnimation:
                        {
                            if (string.IsNullOrEmpty(Outcome))
                            {
                                Outcome = assignOutcomeToAnimation.Outcome;
                            }
                            break;
                        }
                        default:
                        {
                            throw new ArgumentException();
                        }
                    }
                } while (true);

                result = _stateSequence.First();

                if (Scene.InteractionCallBack != null)
                {
                    var somethingWasChanged = Scene.InteractionCallBack.Invoke(keyBoardState, keyBoardEvents, mouseClickPosition, collisions, result);

                    keyBoardEvents.Clear();

                    if (somethingWasChanged)
                    {
                        InvalidateFutureStates(stateIndex);
                    }
                }
            }

            _stateSequenceMutex.ReleaseMutex();

            //_logger?.WriteLine(LogMessageCategory.Debug, $"  Main thread: Signaling State producer thread to proceed (after having consumed or tried to consume state)", "state_sequence");
            _proWaitEvent.Set();
            return result;
        }

        public bool IsLeadSufficientlyLargeForResumingAnimation(
            int lastIndexRequested)
        {
            _stateSequenceMutex.WaitOne();
            DisposeOfHistoricStates(lastIndexRequested - _disposedStates);
            var lead = _stateSequenceCount + _disposedStates - lastIndexRequested;
            _stateSequenceMutex.ReleaseMutex();
            _logger?.WriteLine(LogMessageCategory.Debug, $"  Checking if we should continue animation, lead is currently {lead} ({_stateSequenceCount} + {_disposedStates} - {lastIndexRequested})", "state_sequence");

            //_logger?.WriteLine(LogMessageCategory.Debug, $"  Main thread: Signaling State producer thread to proceed (after having checked for resume of animation)", "state_sequence");
            _proWaitEvent.Set();

            return lead >= QueueMaxSize;
        }

        public State SpawnNewThread()
        {
            if (Scene == null)
            {
                throw new InvalidOperationException("Please set the Scene property before spawning a new thread");
            }

            var initialState = Scene.InitialState.Clone();

            if (Scene.InitializationCallback != null)
            {
                Scene.InitializationCallback.Invoke(initialState, PreviousScene);
            }

            _finalStateIndex = Scene.FinalStateIndex;
            PreviousScene = null;
            Outcome = null;
            _stateSequenceMutex.WaitOne();
            _stateSequence.Add(initialState);
            _stateSequenceCount = 1;
            _stateSequenceMutex.ReleaseMutex();

            _logger?.WriteLine(LogMessageCategory.Debug, "Spawning new thread", "state_sequence");

            _logger?.WriteLine(LogMessageCategory.Debug, "State 0:", "propagation");
            Calculator.LogState(Scene.InitialState, _logger);

            _pro = new Thread(Producer);
            _pro.Start();

            return initialState;
        }

        public void Reset()
        {
            StopThreadIfRunning();
            ClearStateSequence();
            _potentialEvents.Clear();
        }

        private void ClearStateSequence()
        {
            _stateSequenceMutex.WaitOne();
            _stateSequence.Clear();
            _stateSequenceCount = 0;
            _lastIndexGenerated = 0;
            _disposedStates = 0;
            _stateSequenceMutex.ReleaseMutex();
        }

        private void StopThreadIfRunning()
        {
            if (_pro == null) return;

            _exit = true;
            _proWaitEvent.Set();
            _pro.Join();
            _pro = null;
            _exit = false;
            _logger?.WriteLine(LogMessageCategory.Debug, "Thread should have exited by now", "state_sequence");
        }

        private void DisposeOfHistoricStates(
            int indexOfFirstNonDisposedState)
        {
            var nDisposableStates = indexOfFirstNonDisposedState < _stateSequenceCount
                ? indexOfFirstNonDisposedState
                : _stateSequenceCount - 1;

            _stateSequence = _stateSequence.Skip(nDisposableStates).ToList();
            _stateSequenceCount -= nDisposableStates;
            _disposedStates += nDisposableStates;

            _logger?.WriteLine(LogMessageCategory.Debug, $"  Main thread: Disposed of {nDisposableStates} historical states. Buffer size: {_stateSequenceCount}", "state_sequence");
        }

        private void InvalidateFutureStates(
            int stateIndex)
        {
            if (_potentialEvents.Count > 0)
            {
                _potentialEvents = new Queue<PotentialEvent>(_potentialEvents.TakeWhile(x => x.StateIndex < stateIndex));
            }

            var effectiveCurrentIndex = stateIndex - _disposedStates;

            var disposableStates = _stateSequenceCount - 1 - effectiveCurrentIndex;

            if (disposableStates <= 0) return;

            _logger?.WriteLine(LogMessageCategory.Debug, $"Gonna invalidate states later than index {stateIndex} ({disposableStates} states in total)", "state_sequence");
            var remainingStates = effectiveCurrentIndex + 1;
            _stateSequence = _stateSequence.Take(remainingStates).ToList();
            _stateSequenceCount = remainingStates;
            _lastIndexGenerated -= disposableStates;
            _logger?.WriteLine(LogMessageCategory.Debug, $"Done - length of state sequence is now {_stateSequenceCount}, and Last index generated is {_lastIndexGenerated})", "state_sequence");
        }

        // Denne skal hele tiden tage den seneste og propagere den
        // Det er ikke her at states invalideres
        private void Producer()
        {
            while (true)
            {
                // Vent indtil der er adgang til state sekvensen
                _stateSequenceMutex.WaitOne();

                // Der er adgang til state sekvensen nu

                // Er state sekvensen fuld?
                while (_stateSequenceCount >= QueueMaxSize ||
                    _lastIndexGenerated == _finalStateIndex)
                {
                    // Ja - state sekvensen er fuld

                    // Slip state sekvensen, så andre kan tage noget fra den
                    _stateSequenceMutex.ReleaseMutex();

                    // Vent på et signal om at der skal fortsættes, som f.eks. hvis der igen er skabt plads i state sekvensen
                    _proWaitEvent.WaitOne();

                    if (_exit)
                    {
                        _logger?.WriteLine(LogMessageCategory.Debug, "State producer thread: Got signal to exit", "state_sequence");
                        return;
                    }

                    // Vent indtil der er adgang til state sekvensen
                    _stateSequenceMutex.WaitOne();
                }

                // Køen er ikke fuld nu

                // Tag den seneste state fra state sekvensen
                var currentState = _stateSequence.Last();
                var lastIndexGeneratedBeforePropagation = _lastIndexGenerated;

                // Slip state sekvensen, så andre kan tage noget fra den
                _stateSequenceMutex.ReleaseMutex();

                // Propager staten
                var propagatedState = Calculator.PropagateState(
                    Scene, 
                    currentState, 
                    Scene.DeltaT, 
                    _logger, 
                    out var boundaryCollisionReports,
                    out var bodyCollisionReports);

                // Possibly add extra bodies to the state (or manipulate the state in some other way..)
                var response = Scene.PostPropagationCallBack?.Invoke(propagatedState, boundaryCollisionReports, bodyCollisionReports);

                // Vent indtil der er adgang til state sekvensen
                _stateSequenceMutex.WaitOne();

                // Dette check er for at undgå, at man tilføjer en state efter at have cuttet de forreste af i forbindelse med invalidation
                if (_lastIndexGenerated == lastIndexGeneratedBeforePropagation)
                {
                    if (boundaryCollisionReports.Any())
                    {
                        _potentialEvents.Enqueue(new BoundaryCollision(_lastIndexGenerated, boundaryCollisionReports));
                    }

                    if (response?.IndexOfLastState != null)
                    {
                        _potentialEvents.Enqueue(new AssignExpirationToAnimation(
                            _lastIndexGenerated, response.IndexOfLastState.Value));
                    }

                    if (response != null && !string.IsNullOrEmpty(response.Outcome))
                    {
                        _potentialEvents.Enqueue(new AssignOutcomeToAnimation(
                            _lastIndexGenerated, response.Outcome));
                    }

                    // Tilføj den beregnede state til state sekvensen
                    _stateSequence.Add(propagatedState);
                    _stateSequenceCount++;
                    _lastIndexGenerated++;

                    var message = $"State producer thread: Produced state {_lastIndexGenerated}. Buffer size: {_stateSequenceCount}. Bodies: {propagatedState.BodyStates.Count}";

                    if (_stateSequenceCount == QueueMaxSize)
                    {
                        message += " (full)";
                    }

                    if (_lastIndexGenerated == _finalStateIndex)
                    {
                        message += " (done)";
                    }

                    _logger?.WriteLine(LogMessageCategory.Debug, message, "state_sequence");
                }
                else
                {
                    // If we are here, then some of the latest states were invalidated
                    _logger?.WriteLine(LogMessageCategory.Debug, $"State producer thread: Newly produced state with index {lastIndexGeneratedBeforePropagation + 1} has to be discarded", "state_sequence");
                }

                // Slip køen, så andre kan tage noget fra den
                _stateSequenceMutex.ReleaseMutex();
            }
        }
    }
}
