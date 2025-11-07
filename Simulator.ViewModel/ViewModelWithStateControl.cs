using Craft.DataStructures.Graph;
using Craft.Utils;
using GalaSoft.MvvmLight;
using Simulator.Application;

namespace Simulator.ViewModel
{
    public class ViewModelWithStateControl : ViewModelBase
    {
        private StateMachine _stateMachine;

        public ObservableObject<Craft.DataStructures.Graph.State> ApplicationState { get; private set; }
        public Craft.DataStructures.Graph.State PreviousState { get; private set; }

        public void AddApplicationState(
            Craft.DataStructures.Graph.State applicationState)
        {
            if (_stateMachine == null)
            {
                _stateMachine = new StateMachine(applicationState);
                ApplicationState = new ObservableObject<Craft.DataStructures.Graph.State> { Object = applicationState };
            }
            else
            {
                if (_stateMachine.Vertices.Any(_ => _.Name == applicationState.Name))
                {
                    throw new InvalidOperationException("The name of the application state has to be unique");
                }

                _stateMachine.AddVertex(applicationState);
            }
        }

        public void AddApplicationStateTransition(
            Craft.DataStructures.Graph.State from,
            Craft.DataStructures.Graph.State to)
        {
            _stateMachine.AddTransition(from, to);
        }

        public IEnumerable<string> ExitsFromCurrentApplicationState()
        {
            return _stateMachine.ExitsFromCurrentState();
        }

        public void SwitchState(
            Engine engine,
            string name = null)
        {
            if (engine.Scene != null)
            {
                engine.PreviousScene = engine.Scene.Name;
            }

            PreviousState = _stateMachine.CurrentState;

            _stateMachine.SwitchState(name);

            ApplicationState.Object = _stateMachine.CurrentState;
        }
    }
}
