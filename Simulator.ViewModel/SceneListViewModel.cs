using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using Craft.Utils;
using Simulator.Domain;

namespace Simulator.ViewModel
{
    public class SceneListViewModel : ViewModelBase
    {
        private Dictionary<string, Scene> _sceneDictionary;
        private Scene _activeScene;

        public ObservableObject<Scene> SelectedScene { get; }

        public ObservableCollection<Scene> Scenes { get; }

        public Scene ActiveScene
        {
            get { return _activeScene; }
            set
            {
                _activeScene = value;
                SelectedScene.Object = _activeScene;
                RaisePropertyChanged();
            }
        }

        public SceneListViewModel()
        {
            _sceneDictionary = new Dictionary<string, Scene>();
            Scenes = new ObservableCollection<Scene>();
            SelectedScene = new ObservableObject<Scene>();
        }

        public void AddScene(
            Scene scene)
        {
            if (string.IsNullOrEmpty(scene.Name) ||
                _sceneDictionary.ContainsKey(scene.Name))
            {
                throw new InvalidOperationException(
                    "A scene must have a unique name in order to be included in the collection");
            }

            _sceneDictionary[scene.Name] = scene;
            Scenes.Add(scene);
        }

        public bool ContainsScene(
            string name)
        {
            return _sceneDictionary.ContainsKey(name);
        }

        public Scene GetScene(
            string name)
        {
            if (!_sceneDictionary.ContainsKey(name))
            {
                throw new InvalidOperationException("No scene with given name found");
            }

            return _sceneDictionary[name];
        }
    }
}
